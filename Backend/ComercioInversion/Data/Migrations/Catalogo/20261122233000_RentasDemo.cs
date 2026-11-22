using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ComercioInversion.Data.Migrations.Catalogo;

[DbContext(typeof(CatalogoContext))]
[Migration("20261122233000_RentasDemo")]
public sealed class RentasDemo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE catalogo.cierre_renta (
            "Id" uuid PRIMARY KEY,
            "OfertaId" uuid NOT NULL REFERENCES catalogo.oferta_publicada ("Id") ON DELETE RESTRICT,
            "TenantId" uuid NOT NULL,
            "Periodo" text NOT NULL,
            "Corte" timestamptz NOT NULL,
            "AutorId" uuid NOT NULL,
            "RevisorId" uuid,
            "AprobadoEn" timestamptz,
            "Datos" jsonb NOT NULL
        );
        CREATE UNIQUE INDEX "IX_cierre_renta_OfertaId_Periodo" ON catalogo.cierre_renta ("OfertaId", "Periodo");
        CREATE TABLE catalogo.pago_renta (
            "Id" uuid PRIMARY KEY,
            "CierreId" uuid NOT NULL REFERENCES catalogo.cierre_renta ("Id") ON DELETE RESTRICT,
            "CompradorId" uuid NOT NULL,
            "Unidades" bigint NOT NULL,
            "Centavos" bigint NOT NULL,
            "Stroops" bigint NOT NULL,
            "Billetera" text NOT NULL,
            "Sobre" text,
            "Hash" text,
            "Ledger" bigint,
            "Fallido" boolean NOT NULL
        );
        CREATE INDEX "IX_pago_renta_CompradorId" ON catalogo.pago_renta ("CompradorId");
        CREATE UNIQUE INDEX "IX_pago_renta_CierreId_CompradorId" ON catalogo.pago_renta ("CierreId", "CompradorId");
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TABLE catalogo.pago_renta;
        DROP TABLE catalogo.cierre_renta;
        """);
}
