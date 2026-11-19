using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adminstracion.Migrations
{
    /// <inheritdoc />
    public partial class VincularTenantASuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "admin",
                table: "suscripcion",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE admin.suscripcion AS s
                SET tenant_id = t.id
                FROM admin.tenant AS t
                WHERE t.propietario_id = s.usuario_id;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM admin.suscripcion WHERE tenant_id IS NULL) THEN
                        RAISE EXCEPTION 'Hay suscripciones sin tenant para su propietario. Completa los vínculos antes de aplicar esta migración.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "admin",
                table: "suscripcion",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_suscripcion_tenant_id",
                schema: "admin",
                table: "suscripcion",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_suscripcion_tenant_tenant_id",
                schema: "admin",
                table: "suscripcion",
                column: "tenant_id",
                principalSchema: "admin",
                principalTable: "tenant",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_suscripcion_tenant_tenant_id",
                schema: "admin",
                table: "suscripcion");

            migrationBuilder.DropIndex(
                name: "IX_suscripcion_tenant_id",
                schema: "admin",
                table: "suscripcion");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "admin",
                table: "suscripcion");
        }
    }
}
