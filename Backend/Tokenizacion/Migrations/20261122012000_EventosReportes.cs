using Tokenizacion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tokenizacion.Migrations;

[DbContext(typeof(TokenizacionContext))]
[Migration("20261122012000_EventosReportes")]
public sealed class EventosReportes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE tokenizacion.evento_reportes (
            id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            tipo text NOT NULL,
            referencia uuid NOT NULL,
            datos jsonb NOT NULL,
            entregado boolean NOT NULL DEFAULT false
        );
        CREATE INDEX ix_evento_reportes_pendiente ON tokenizacion.evento_reportes (id) WHERE NOT entregado;

        LOCK TABLE tokenizacion.emision_inmueble, tokenizacion.instruccion_cadena IN SHARE ROW EXCLUSIVE MODE;
        CREATE VIEW tokenizacion.emision_para_reportes AS
        SELECT e.inmueble_ref, e.instruccion_id, jsonb_build_object(
            'estado', i.estado, 'unidades', i.cantidad_base / 10000000, 'codigo', a.codigo,
            'emisor', a.emisor, 'red', a.red) AS datos
        FROM tokenizacion.emision_inmueble e
        JOIN tokenizacion.instruccion_cadena i ON i.id = e.instruccion_id
        JOIN tokenizacion.activo_cadena a ON a.id = i.activo_id;

        CREATE FUNCTION tokenizacion.publicar_emision_reportes() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                INSERT INTO tokenizacion.evento_reportes(tipo, referencia, datos)
                VALUES ('emision', OLD.inmueble_ref, jsonb_build_object('eliminado', true));
                RETURN OLD;
            END IF;
            IF TG_TABLE_NAME = 'emision_inmueble' THEN
                INSERT INTO tokenizacion.evento_reportes(tipo, referencia, datos)
                SELECT 'emision', inmueble_ref, datos FROM tokenizacion.emision_para_reportes WHERE inmueble_ref = NEW.inmueble_ref;
            ELSE
                IF NEW.estado IS NOT DISTINCT FROM OLD.estado OR NEW.clase <> 'EMISION_INICIAL' THEN RETURN NEW; END IF;
                INSERT INTO tokenizacion.evento_reportes(tipo, referencia, datos)
                SELECT 'emision', inmueble_ref, datos FROM tokenizacion.emision_para_reportes WHERE instruccion_id = NEW.id;
            END IF;
            RETURN NEW;
        END $$;
        CREATE TRIGGER tr_emision_reportes AFTER INSERT OR DELETE ON tokenizacion.emision_inmueble
        FOR EACH ROW EXECUTE FUNCTION tokenizacion.publicar_emision_reportes();
        CREATE TRIGGER tr_emision_estado_reportes AFTER UPDATE OF estado ON tokenizacion.instruccion_cadena
        FOR EACH ROW EXECUTE FUNCTION tokenizacion.publicar_emision_reportes();

        INSERT INTO tokenizacion.evento_reportes(tipo, referencia, datos)
        SELECT 'emision', inmueble_ref, datos FROM tokenizacion.emision_para_reportes;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER tr_emision_estado_reportes ON tokenizacion.instruccion_cadena;
        DROP TRIGGER tr_emision_reportes ON tokenizacion.emision_inmueble;
        DROP FUNCTION tokenizacion.publicar_emision_reportes();
        DROP VIEW tokenizacion.emision_para_reportes;
        DROP TABLE tokenizacion.evento_reportes;
        """);
}
