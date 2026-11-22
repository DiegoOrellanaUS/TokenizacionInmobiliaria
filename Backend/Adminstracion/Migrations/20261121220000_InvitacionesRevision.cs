using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adminstracion.Migrations;

public partial class InvitacionesRevision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "invitacion_revision_canjeada",
            schema: "admin",
            columns: table => new
            {
                CodigoHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CanjeadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_invitacion_revision_canjeada", x => x.CodigoHash);
                table.ForeignKey("FK_invitacion_revision_canjeada_usuario_UsuarioId", x => x.UsuarioId,
                    principalSchema: "admin", principalTable: "usuario", principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_invitacion_revision_canjeada_UsuarioId",
            schema: "admin", table: "invitacion_revision_canjeada", column: "UsuarioId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "invitacion_revision_canjeada", schema: "admin");
}
