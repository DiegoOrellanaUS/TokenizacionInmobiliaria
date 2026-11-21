using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComercioInversion.Data.Migrations.Catalogo
{
    /// <inheritdoc />
    public partial class ComprasDemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "disponibles",
                schema: "catalogo",
                table: "oferta_publicada",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE catalogo.oferta_publicada SET disponibles = unidades");

            migrationBuilder.CreateTable(
                name: "compra_demo",
                schema: "catalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OfertaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompradorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Unidades = table.Column<long>(type: "bigint", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    CreadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReintentarEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: true),
                    Billetera = table.Column<string>(type: "text", nullable: true),
                    Ledger = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_demo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compra_demo_oferta_publicada_OfertaId",
                        column: x => x.OfertaId,
                        principalSchema: "catalogo",
                        principalTable: "oferta_publicada",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_disponibles_oferta",
                schema: "catalogo",
                table: "oferta_publicada",
                sql: "disponibles >= 0 AND disponibles <= unidades");

            migrationBuilder.CreateIndex(
                name: "IX_compra_demo_CompradorId_CreadaEn",
                schema: "catalogo",
                table: "compra_demo",
                columns: new[] { "CompradorId", "CreadaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_compra_demo_Estado_ReintentarEn",
                schema: "catalogo",
                table: "compra_demo",
                columns: new[] { "Estado", "ReintentarEn" });

            migrationBuilder.CreateIndex(
                name: "IX_compra_demo_OfertaId",
                schema: "catalogo",
                table: "compra_demo",
                column: "OfertaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compra_demo",
                schema: "catalogo");

            migrationBuilder.DropCheckConstraint(
                name: "ck_disponibles_oferta",
                schema: "catalogo",
                table: "oferta_publicada");

            migrationBuilder.DropColumn(
                name: "disponibles",
                schema: "catalogo",
                table: "oferta_publicada");
        }
    }
}
