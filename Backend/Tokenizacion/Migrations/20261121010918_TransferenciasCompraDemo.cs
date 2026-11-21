using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tokenizacion.Migrations
{
    /// <inheritdoc />
    public partial class TransferenciasCompraDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transferencia_compra_demo",
                schema: "tokenizacion",
                columns: table => new
                {
                    CompraId = table.Column<Guid>(type: "uuid", nullable: false),
                    InmuebleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompradorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unidades = table.Column<long>(type: "bigint", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: true),
                    Sobre = table.Column<string>(type: "text", nullable: true),
                    Billetera = table.Column<string>(type: "text", nullable: true),
                    Ledger = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencia_compra_demo", x => x.CompraId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_compra_demo_InmuebleId_Estado",
                schema: "tokenizacion",
                table: "transferencia_compra_demo",
                columns: new[] { "InmuebleId", "Estado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transferencia_compra_demo",
                schema: "tokenizacion");
        }
    }
}
