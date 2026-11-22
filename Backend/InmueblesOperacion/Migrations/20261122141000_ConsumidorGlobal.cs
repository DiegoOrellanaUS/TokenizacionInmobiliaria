using InmueblesOperacion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InmueblesOperacion.Migrations;

[DbContext(typeof(InmueblesOperacionContext))]
[Migration("20261122141000_ConsumidorGlobal")]
public sealed class ConsumidorGlobal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE operacion.evento_reportes ADD COLUMN entregado_global boolean NOT NULL DEFAULT false;
        CREATE INDEX ix_evento_reportes_global ON operacion.evento_reportes (id) WHERE NOT entregado_global;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX operacion.ix_evento_reportes_global;
        ALTER TABLE operacion.evento_reportes DROP COLUMN entregado_global;
        """);
}
