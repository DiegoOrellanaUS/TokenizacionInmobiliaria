using ComercioInversion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ComercioInversion.Migrations;

[DbContext(typeof(CatalogoContext))]
[Migration("20261122012000_EventosReportes")]
public sealed class EventosReportes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE catalogo.evento_reportes (
            id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            tenant_id uuid NOT NULL,
            tipo text NOT NULL,
            referencia uuid NOT NULL,
            datos jsonb NOT NULL,
            entregado boolean NOT NULL DEFAULT false
        );
        CREATE INDEX ix_evento_reportes_pendiente ON catalogo.evento_reportes (tenant_id, id) WHERE NOT entregado;

        ALTER TABLE catalogo.compra_demo ADD COLUMN "TransferenciaIniciada" boolean NOT NULL DEFAULT false;
        -- Las compras anteriores pueden haber llegado a Stellar aunque todavía estén pendientes.
        UPDATE catalogo.compra_demo SET "TransferenciaIniciada" = true WHERE "Estado" = 'PENDIENTE';

        CREATE FUNCTION catalogo.datos_oferta_reportes(o catalogo.oferta_publicada) RETURNS jsonb LANGUAGE sql STABLE AS $$
            SELECT jsonb_build_object('inmuebleId', o."InmuebleId", 'nombre', o."Nombre",
                'unidades', o.unidades, 'precio', o.precio, 'moneda', o."Moneda", 'codigo', o."Codigo");
        $$;
        CREATE FUNCTION catalogo.datos_compra_reportes(c catalogo.compra_demo) RETURNS jsonb LANGUAGE sql STABLE AS $$
            SELECT jsonb_build_object('ofertaId', c."OfertaId", 'estado', c."Estado", 'unidades', c."Unidades",
                'precio', c."PrecioUnitario", 'moneda', c."Moneda", 'fecha', c."CreadaEn");
        $$;
        CREATE FUNCTION catalogo.publicar_oferta_reportes() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
                VALUES (OLD."TenantId", 'oferta', OLD."Id", jsonb_build_object('eliminado', true));
                RETURN OLD;
            END IF;
            INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
            VALUES (NEW."TenantId", 'oferta', NEW."Id", catalogo.datos_oferta_reportes(NEW));
            RETURN NEW;
        END $$;
        CREATE FUNCTION catalogo.publicar_compra_reportes() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
                SELECT o."TenantId", 'compra', OLD."Id", jsonb_build_object('eliminado', true)
                FROM catalogo.oferta_publicada o WHERE o."Id" = OLD."OfertaId";
                RETURN OLD;
            END IF;
            INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
            SELECT o."TenantId", 'compra', NEW."Id", catalogo.datos_compra_reportes(NEW)
            FROM catalogo.oferta_publicada o WHERE o."Id" = NEW."OfertaId";
            RETURN NEW;
        END $$;

        LOCK TABLE catalogo.oferta_publicada IN SHARE ROW EXCLUSIVE MODE;
        CREATE TRIGGER tr_oferta_reportes AFTER INSERT OR UPDATE OF "Nombre", unidades, precio, "Moneda", "Codigo" OR DELETE
        ON catalogo.oferta_publicada FOR EACH ROW EXECUTE FUNCTION catalogo.publicar_oferta_reportes();
        CREATE TRIGGER tr_compra_reportes AFTER INSERT OR UPDATE OF "Estado", "Unidades", "PrecioUnitario", "Moneda" OR DELETE
        ON catalogo.compra_demo FOR EACH ROW EXECUTE FUNCTION catalogo.publicar_compra_reportes();

        INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
        SELECT o."TenantId", 'oferta', o."Id", catalogo.datos_oferta_reportes(o) FROM catalogo.oferta_publicada o;
        INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
        SELECT o."TenantId", 'compra', c."Id", catalogo.datos_compra_reportes(c)
        FROM catalogo.compra_demo c JOIN catalogo.oferta_publicada o ON o."Id" = c."OfertaId";
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER tr_compra_reportes ON catalogo.compra_demo;
        DROP TRIGGER tr_oferta_reportes ON catalogo.oferta_publicada;
        DROP FUNCTION catalogo.publicar_compra_reportes();
        DROP FUNCTION catalogo.publicar_oferta_reportes();
        DROP FUNCTION catalogo.datos_compra_reportes(catalogo.compra_demo);
        DROP FUNCTION catalogo.datos_oferta_reportes(catalogo.oferta_publicada);
        DROP TABLE catalogo.evento_reportes;
        ALTER TABLE catalogo.compra_demo DROP COLUMN "TransferenciaIniciada";
        """);
}
