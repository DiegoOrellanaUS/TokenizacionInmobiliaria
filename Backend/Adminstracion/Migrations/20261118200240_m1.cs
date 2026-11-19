using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adminstracion.Migrations
{
    /// <inheritdoc />
    public partial class m1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "plan",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    precio = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    moneda = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    periodicidad = table.Column<string>(type: "text", nullable: false),
                    max_tokens_propios = table.Column<int>(type: "integer", nullable: false),
                    max_tokens_como_cliente = table.Column<int>(type: "integer", nullable: false),
                    plan_origen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    plan_destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    oferta_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    oferta_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan", x => x.id);
                    table.CheckConstraint("ck_plan_condiciones", "revision > 0 AND precio >= 0 AND max_tokens_propios >= 0 AND max_tokens_como_cliente >= 0");
                    table.CheckConstraint("ck_plan_oferta_fechas", "oferta_hasta > oferta_desde");
                    table.CheckConstraint("ck_plan_transicion", "(plan_origen_id IS NULL OR plan_destino_id IS NOT NULL)\nAND (plan_origen_id IS NULL OR plan_origen_id <> plan_destino_id)\nAND (plan_origen_id IS NULL OR plan_origen_id <> id)\nAND (plan_destino_id IS NULL OR plan_destino_id <> id)");
                    table.ForeignKey(
                        name: "FK_plan_plan_plan_destino_id",
                        column: x => x.plan_destino_id,
                        principalSchema: "admin",
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_plan_plan_plan_origen_id",
                        column: x => x.plan_origen_id,
                        principalSchema: "admin",
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre_visible = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    orden_visual = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id);
                    table.CheckConstraint("ck_rol_orden", "orden_visual >= 0");
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombres = table.Column<string>(type: "text", nullable: false),
                    apellidos = table.Column<string>(type: "text", nullable: false),
                    ci = table.Column<string>(type: "text", nullable: true),
                    correo_electronico = table.Column<string>(type: "text", nullable: true),
                    emisor_oidc = table.Column<string>(type: "text", nullable: true),
                    sujeto_oidc = table.Column<string>(type: "text", nullable: true),
                    contrasenia_hash = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                    table.CheckConstraint("ck_usuario_identidad_oidc", "(emisor_oidc IS NULL) = (sujeto_oidc IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "suscripcion",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condiciones_contratadas = table.Column<string>(type: "jsonb", nullable: false),
                    proveedor_pago = table.Column<string>(type: "text", nullable: true),
                    referencia_pago = table.Column<string>(type: "text", nullable: true),
                    importe_pagado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    pago_confirmado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suscripcion", x => x.id);
                    table.CheckConstraint("ck_suscripcion_pago", "(proveedor_pago IS NULL) = (referencia_pago IS NULL)\nAND (pago_confirmado_en IS NULL OR (referencia_pago IS NOT NULL AND importe_pagado IS NOT NULL))\nAND (importe_pagado IS NULL OR importe_pagado >= 0)");
                    table.CheckConstraint("ck_suscripcion_vigencia", "hasta > desde");
                    table.ForeignKey(
                        name: "FK_suscripcion_plan_plan_id",
                        column: x => x.plan_id,
                        principalSchema: "admin",
                        principalTable: "plan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_suscripcion_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "admin",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    propietario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_comercial = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_usuario_propietario_id",
                        column: x => x.propietario_id,
                        principalSchema: "admin",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alcance = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    silos_por_servicio = table.Column<string>(type: "jsonb", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_rol", x => x.id);
                    table.CheckConstraint("ck_usuario_rol_alcance", "(alcance = 'GLOBAL' AND tenant_id IS NULL AND token_ref IS NULL)\nOR (alcance = 'TENANT' AND tenant_id IS NOT NULL AND token_ref IS NULL)\nOR (alcance = 'TOKEN' AND tenant_id IS NOT NULL AND token_ref IS NOT NULL)");
                    table.CheckConstraint("ck_usuario_rol_fechas", "fecha_eliminacion >= fecha_creacion");
                    table.CheckConstraint("ck_usuario_rol_silos_alcance", "silos_por_servicio IS NULL OR alcance = 'TENANT'");
                    table.ForeignKey(
                        name: "FK_usuario_rol_rol_rol_id",
                        column: x => x.rol_id,
                        principalSchema: "admin",
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuario_rol_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "admin",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuario_rol_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalSchema: "admin",
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_codigo",
                schema: "admin",
                table: "plan",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plan_plan_destino_id",
                schema: "admin",
                table: "plan",
                column: "plan_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_plan_origen_id",
                schema: "admin",
                table: "plan",
                column: "plan_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_rol_codigo",
                schema: "admin",
                table: "rol",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suscripcion_plan_id",
                schema: "admin",
                table: "suscripcion",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_suscripcion_proveedor_pago_referencia_pago",
                schema: "admin",
                table: "suscripcion",
                columns: new[] { "proveedor_pago", "referencia_pago" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_suscripcion_usuario_id",
                schema: "admin",
                table: "suscripcion",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_propietario_id",
                schema: "admin",
                table: "tenant",
                column: "propietario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_ci",
                schema: "admin",
                table: "usuario",
                column: "ci",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo_electronico",
                schema: "admin",
                table: "usuario",
                column: "correo_electronico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_emisor_oidc_sujeto_oidc",
                schema: "admin",
                table: "usuario",
                columns: new[] { "emisor_oidc", "sujeto_oidc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_rol_id",
                schema: "admin",
                table: "usuario_rol",
                column: "rol_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_usuario_id_rol_id_tenant_id_token_ref",
                schema: "admin",
                table: "usuario_rol",
                columns: new[] { "usuario_id", "rol_id", "tenant_id", "token_ref" },
                unique: true,
                filter: "fecha_eliminacion IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_usuario_rol_silos_tenant",
                schema: "admin",
                table: "usuario_rol",
                column: "tenant_id",
                unique: true,
                filter: "fecha_eliminacion IS NULL AND silos_por_servicio IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suscripcion",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "usuario_rol",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "plan",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "rol",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "tenant",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "usuario",
                schema: "admin");
        }
    }
}
