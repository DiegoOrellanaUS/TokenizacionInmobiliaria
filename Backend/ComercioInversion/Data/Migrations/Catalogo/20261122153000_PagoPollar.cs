using ComercioInversion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ComercioInversion.Data.Migrations.Catalogo;

[DbContext(typeof(CatalogoContext))]
[Migration("20261122153000_PagoPollar")]
public sealed class PagoPollar : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        ALTER TABLE catalogo.compra_demo
            ADD COLUMN "PagadorUsdc" text,
            ADD COLUMN "DestinoUsdc" text,
            ADD COLUMN "HashPago" text,
            ADD COLUMN "LedgerPago" bigint;
        CREATE UNIQUE INDEX "IX_compra_demo_HashPago" ON catalogo.compra_demo ("HashPago");
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP INDEX catalogo."IX_compra_demo_HashPago";
        ALTER TABLE catalogo.compra_demo
            DROP COLUMN "PagadorUsdc", DROP COLUMN "DestinoUsdc", DROP COLUMN "HashPago", DROP COLUMN "LedgerPago";
        """);
}
