using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComercioInversion.Data.Migrations.Catalogo
{
    /// <inheritdoc />
    public partial class CatalogoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalogo");

            migrationBuilder.CreateTable(
                name: "oferta_publicada",
                schema: "catalogo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InmuebleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Ciudad = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    unidades = table.Column<long>(type: "bigint", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PublicadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oferta_publicada", x => x.Id);
                    table.CheckConstraint("ck_precio_oferta", "precio > 0");
                    table.CheckConstraint("ck_unidades_oferta", "unidades > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_oferta_publicada_TenantId_InmuebleId",
                schema: "catalogo",
                table: "oferta_publicada",
                columns: new[] { "TenantId", "InmuebleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oferta_publicada",
                schema: "catalogo");
        }
    }
}
