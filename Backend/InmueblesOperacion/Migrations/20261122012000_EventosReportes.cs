using InmueblesOperacion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InmueblesOperacion.Migrations;

[DbContext(typeof(InmueblesOperacionContext))]
[Migration("20261122012000_EventosReportes")]
public sealed class EventosReportes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE operacion.evento_reportes (
            id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
            tipo text NOT NULL,
            referencia uuid NOT NULL,
            datos jsonb NOT NULL,
            entregado boolean NOT NULL DEFAULT false
        );
        CREATE INDEX ix_evento_reportes_pendiente ON operacion.evento_reportes (id) WHERE NOT entregado;

        LOCK TABLE operacion.inmueble IN SHARE ROW EXCLUSIVE MODE;
        CREATE FUNCTION operacion.publicar_inmueble_reportes() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            IF TG_OP = 'DELETE' THEN
                INSERT INTO operacion.evento_reportes(tipo, referencia, datos)
                VALUES ('inmueble', OLD.id, jsonb_build_object('eliminado', true));
                RETURN OLD;
            END IF;

            INSERT INTO operacion.evento_reportes(tipo, referencia, datos)
            VALUES ('inmueble', NEW.id, jsonb_build_object('nombre', NEW.nombre,
                'estado', NEW.estado_revision, 'eliminado', NEW.estado <> 'ACTIVO'));
            RETURN NEW;
        END $$;
        CREATE TRIGGER tr_inmueble_reportes AFTER INSERT OR UPDATE OF nombre, estado_revision, estado OR DELETE
        ON operacion.inmueble FOR EACH ROW EXECUTE FUNCTION operacion.publicar_inmueble_reportes();

        INSERT INTO operacion.evento_reportes(tipo, referencia, datos)
        SELECT 'inmueble', id, jsonb_build_object('nombre', nombre,
            'estado', estado_revision, 'eliminado', estado <> 'ACTIVO')
        FROM operacion.inmueble;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TRIGGER tr_inmueble_reportes ON operacion.inmueble;
        DROP FUNCTION operacion.publicar_inmueble_reportes();
        DROP TABLE operacion.evento_reportes;
        """);
}
