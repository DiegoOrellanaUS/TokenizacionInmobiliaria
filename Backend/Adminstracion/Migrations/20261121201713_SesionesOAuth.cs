using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adminstracion.Migrations
{
    /// <inheritdoc />
    public partial class SesionesOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sesion_oauth",
                schema: "admin",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AccessHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VenceAccess = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    VenceEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevocadoEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesion_oauth", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_sesion_oauth_usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "admin",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sesion_oauth_AccessHash",
                schema: "admin",
                table: "sesion_oauth",
                column: "AccessHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesion_oauth_FamiliaId",
                schema: "admin",
                table: "sesion_oauth",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_sesion_oauth_UsuarioId",
                schema: "admin",
                table: "sesion_oauth",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sesion_oauth",
                schema: "admin");
        }
    }
}
