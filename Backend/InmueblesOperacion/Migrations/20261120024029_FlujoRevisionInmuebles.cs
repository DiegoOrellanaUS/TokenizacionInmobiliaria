using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InmueblesOperacion.Migrations
{
    /// <inheritdoc />
    public partial class FlujoRevisionInmuebles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "verificada_en",
                schema: "operacion",
                table: "titularidad",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "contenido",
                schema: "operacion",
                table: "documento_version",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "evento_inmueble",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado_revision = table.Column<string>(type: "text", nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_inmueble", x => x.id);
                    table.ForeignKey(
                        name: "fk_evento_inmueble",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evento_inmueble_inmueble_id",
                schema: "operacion",
                table: "evento_inmueble",
                column: "inmueble_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evento_inmueble",
                schema: "operacion");

            migrationBuilder.DropColumn(
                name: "verificada_en",
                schema: "operacion",
                table: "titularidad");

            migrationBuilder.DropColumn(
                name: "contenido",
                schema: "operacion",
                table: "documento_version");
        }
    }
}
