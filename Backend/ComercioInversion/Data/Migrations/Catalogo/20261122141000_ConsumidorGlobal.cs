using ComercioInversion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ComercioInversion.Migrations;

[DbContext(typeof(CatalogoContext))]
[Migration("20261122141000_ConsumidorGlobal")]
public sealed class ConsumidorGlobal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE catalogo.evento_reportes ADD COLUMN entregado_global boolean NOT NULL DEFAULT false;
        CREATE INDEX ix_evento_reportes_global ON catalogo.evento_reportes (id) WHERE NOT entregado_global;

        LOCK TABLE catalogo.oferta_publicada IN SHARE ROW EXCLUSIVE MODE;
        CREATE OR REPLACE FUNCTION catalogo.datos_oferta_reportes(o catalogo.oferta_publicada) RETURNS jsonb LANGUAGE sql STABLE AS $$
            SELECT jsonb_build_object('inmuebleId', o."InmuebleId", 'nombre', o."Nombre",
                'unidades', o.unidades, 'precio', o.precio, 'moneda', o."Moneda", 'codigo', o."Codigo",
                'disponibles', o.disponibles, 'ciudad', o."Ciudad", 'tipo', o."Tipo", 'publicadaEn', o."PublicadaEn");
        $$;
        DROP TRIGGER tr_oferta_reportes ON catalogo.oferta_publicada;
        CREATE TRIGGER tr_oferta_reportes AFTER INSERT OR UPDATE OF "Nombre", unidades, precio, "Moneda", "Codigo",
            disponibles, "Ciudad", "Tipo", "PublicadaEn" OR DELETE ON catalogo.oferta_publicada
        FOR EACH ROW EXECUTE FUNCTION catalogo.publicar_oferta_reportes();

        INSERT INTO catalogo.evento_reportes(tenant_id, tipo, referencia, datos)
        SELECT o."TenantId", 'oferta', o."Id", catalogo.datos_oferta_reportes(o) FROM catalogo.oferta_publicada o;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX catalogo.ix_evento_reportes_global;
        ALTER TABLE catalogo.evento_reportes DROP COLUMN entregado_global;

        DROP TRIGGER tr_oferta_reportes ON catalogo.oferta_publicada;
        CREATE TRIGGER tr_oferta_reportes AFTER INSERT OR UPDATE OF "Nombre", unidades, precio, "Moneda", "Codigo" OR DELETE
        ON catalogo.oferta_publicada FOR EACH ROW EXECUTE FUNCTION catalogo.publicar_oferta_reportes();
        CREATE OR REPLACE FUNCTION catalogo.datos_oferta_reportes(o catalogo.oferta_publicada) RETURNS jsonb LANGUAGE sql STABLE AS $$
            SELECT jsonb_build_object('inmuebleId', o."InmuebleId", 'nombre', o."Nombre",
                'unidades', o.unidades, 'precio', o.precio, 'moneda', o."Moneda", 'codigo', o."Codigo");
        $$;
        """);
}
