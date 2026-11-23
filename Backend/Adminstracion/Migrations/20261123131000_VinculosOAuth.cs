using Adminstracion.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Adminstracion.Migrations;

[DbContext(typeof(AdminContext))]
[Migration("20261123131000_VinculosOAuth")]
public sealed class VinculosOAuth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "vinculo_oauth",
            schema: "admin",
            columns: table => new
            {
                Emisor = table.Column<string>(type: "text", nullable: false),
                Sujeto = table.Column<string>(type: "text", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_vinculo_oauth", v => new { v.Emisor, v.Sujeto });
                table.ForeignKey("FK_vinculo_oauth_usuario_UsuarioId", v => v.UsuarioId,
                    principalSchema: "admin", principalTable: "usuario", principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_vinculo_oauth_UsuarioId_Emisor", "vinculo_oauth",
            new[] { "UsuarioId", "Emisor" }, schema: "admin", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("vinculo_oauth", "admin");
}
