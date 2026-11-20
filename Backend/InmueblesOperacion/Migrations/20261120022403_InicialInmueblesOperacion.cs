using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InmueblesOperacion.Migrations
{
    /// <inheritdoc />
    public partial class InicialInmueblesOperacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operacion");

            migrationBuilder.CreateTable(
                name: "auditoria",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "text", nullable: true),
                    recurso = table.Column<string>(type: "text", nullable: true),
                    objeto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlacion = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categoria",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    naturaleza = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categoria", x => x.id);
                    table.CheckConstraint("ck_categoria_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "documento",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clase = table.Column<string>(type: "text", nullable: true),
                    creado_por_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documento", x => x.id);
                    table.CheckConstraint("ck_documento_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "inmueble",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    direccion = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    tipo = table.Column<string>(type: "text", nullable: true),
                    ciudad = table.Column<string>(type: "text", nullable: true),
                    superficie_m2 = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    estado_revision = table.Column<string>(type: "text", nullable: false, defaultValue: "BORRADOR"),
                    creado_por_ref = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inmueble", x => x.id);
                    table.CheckConstraint("ck_inmueble_1", "btrim(estado) <> ''");
                    table.CheckConstraint("ck_inmueble_creado_por_ref", "creado_por_ref <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_inmueble_estado_revision", "estado_revision IN ('BORRADOR', 'EN_REVISION', 'CON_OBSERVACIONES', 'APROBADO')");
                    table.CheckConstraint("ck_inmueble_superficie_m2", "superficie_m2 > 0 AND superficie_m2 <> 'NaN'::numeric");
                });

            migrationBuilder.CreateTable(
                name: "parte",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    clase = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parte", x => x.id);
                    table.CheckConstraint("ck_parte_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "tipo_entidad",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_entidad", x => x.id);
                    table.CheckConstraint("ck_tipo_entidad_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "documento_version",
                schema: "operacion",
                columns: table => new
                {
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    objeto = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documento_version", x => new { x.documento_id, x.version });
                    table.CheckConstraint("ck_documento_version_1", "version > 0");
                    table.ForeignKey(
                        name: "fk_documento_version_9",
                        column: x => x.documento_id,
                        principalSchema: "operacion",
                        principalTable: "documento",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inmueble_documento",
                schema: "operacion",
                columns: table => new
                {
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inmueble_documento", x => new { x.inmueble_id, x.documento_id, x.fecha_creacion });
                    table.CheckConstraint("ck_inmueble_documento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_inmueble_documento_10",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_inmueble_documento_11",
                        column: x => x.documento_id,
                        principalSchema: "operacion",
                        principalTable: "documento",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inmueble_revision",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "text", nullable: false),
                    observacion = table.Column<string>(type: "text", nullable: true),
                    revisor_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    revisado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inmueble_revision", x => x.id);
                    table.CheckConstraint("ck_inmueble_revision_decision", "decision IN ('APROBADO', 'CON_OBSERVACIONES')");
                    table.CheckConstraint("ck_inmueble_revision_observacion", "decision <> 'CON_OBSERVACIONES' OR (observacion IS NOT NULL AND observacion ~ '[^[:space:]]')");
                    table.CheckConstraint("ck_inmueble_revision_revisor_ref", "revisor_ref <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.ForeignKey(
                        name: "fk_inmueble_revision_inmueble",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimiento",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento", x => x.id);
                    table.CheckConstraint("ck_movimiento_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_movimiento_14",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "permiso_inmueble",
                schema: "operacion",
                columns: table => new
                {
                    usuario_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permiso = table.Column<string>(type: "text", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permiso_inmueble", x => new { x.usuario_ref, x.inmueble_id, x.permiso, x.fecha_creacion });
                    table.CheckConstraint("ck_permiso_inmueble_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_permiso_inmueble_3",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "unidad",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidad", x => x.id);
                    table.CheckConstraint("ck_unidad_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_unidad_2",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "titularidad",
                schema: "operacion",
                columns: table => new
                {
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desde = table.Column<DateOnly>(type: "date", nullable: false),
                    hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    cuota = table.Column<decimal>(type: "numeric", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_titularidad", x => new { x.inmueble_id, x.parte_id, x.desde });
                    table.CheckConstraint("ck_titularidad_1", "cuota > 0 AND cuota <= 1");
                    table.CheckConstraint("ck_titularidad_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.CheckConstraint("ck_titularidad_3", "hasta IS NULL OR hasta > desde");
                    table.ForeignKey(
                        name: "fk_titularidad_4",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_titularidad_5",
                        column: x => x.parte_id,
                        principalSchema: "operacion",
                        principalTable: "parte",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "usuario_parte",
                schema: "operacion",
                columns: table => new
                {
                    usuario_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    parte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_parte", x => new { x.usuario_ref, x.parte_id, x.fecha_creacion });
                    table.CheckConstraint("ck_usuario_parte_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_usuario_parte_1",
                        column: x => x.parte_id,
                        principalSchema: "operacion",
                        principalTable: "parte",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "atributo",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    clase_valor = table.Column<string>(type: "text", nullable: true),
                    nombre_visible = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atributo", x => x.id);
                    table.CheckConstraint("ck_atributo_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_atributo_34",
                        column: x => x.tipo_entidad_id,
                        principalSchema: "operacion",
                        principalTable: "tipo_entidad",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "entidad",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entidad", x => x.id);
                    table.CheckConstraint("ck_entidad_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_entidad_28",
                        column: x => x.tipo_id,
                        principalSchema: "operacion",
                        principalTable: "tipo_entidad",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tipo_relacion",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_destino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: true),
                    nombre_visible = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_relacion", x => x.id);
                    table.CheckConstraint("ck_tipo_relacion_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_tipo_relacion_29",
                        column: x => x.tipo_origen_id,
                        principalSchema: "operacion",
                        principalTable: "tipo_entidad",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tipo_relacion_30",
                        column: x => x.tipo_destino_id,
                        principalSchema: "operacion",
                        principalTable: "tipo_entidad",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "avaluo",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tasador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metodo = table.Column<string>(type: "text", nullable: true),
                    fecha_valuacion = table.Column<DateOnly>(type: "date", nullable: true),
                    importe = table.Column<decimal>(type: "numeric", nullable: true),
                    moneda = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_version = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_avaluo", x => x.id);
                    table.CheckConstraint("ck_avaluo_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_avaluo_41",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_avaluo_42",
                        column: x => x.tasador_id,
                        principalSchema: "operacion",
                        principalTable: "parte",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_avaluo_43",
                        columns: x => new { x.documento_id, x.documento_version },
                        principalSchema: "operacion",
                        principalTable: "documento_version",
                        principalColumns: new[] { "documento_id", "version" });
                });

            migrationBuilder.CreateTable(
                name: "movimiento_documento",
                schema: "operacion",
                columns: table => new
                {
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_documento", x => new { x.movimiento_id, x.documento_id, x.fecha_creacion });
                    table.CheckConstraint("ck_movimiento_documento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_movimiento_documento_17",
                        column: x => x.movimiento_id,
                        principalSchema: "operacion",
                        principalTable: "movimiento",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_movimiento_documento_18",
                        column: x => x.documento_id,
                        principalSchema: "operacion",
                        principalTable: "documento",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "movimiento_version",
                schema: "operacion",
                columns: table => new
                {
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    importe = table.Column<decimal>(type: "numeric", nullable: false),
                    moneda = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    referencia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_version", x => new { x.movimiento_id, x.version });
                    table.CheckConstraint("ck_movimiento_version_1", "version > 0");
                    table.ForeignKey(
                        name: "fk_movimiento_version_15",
                        column: x => x.movimiento_id,
                        principalSchema: "operacion",
                        principalTable: "movimiento",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_movimiento_version_16",
                        column: x => x.categoria_id,
                        principalSchema: "operacion",
                        principalTable: "categoria",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contrato",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    fin = table.Column<DateOnly>(type: "date", nullable: true),
                    moneda = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    renta = table.Column<decimal>(type: "numeric", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contrato", x => x.id);
                    table.CheckConstraint("ck_contrato_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_contrato_6",
                        column: x => x.unidad_id,
                        principalSchema: "operacion",
                        principalTable: "unidad",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "entidad_inmueble",
                schema: "operacion",
                columns: table => new
                {
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entidad_inmueble", x => new { x.entidad_id, x.inmueble_id, x.fecha_creacion });
                    table.CheckConstraint("ck_entidad_inmueble_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_entidad_inmueble_37",
                        column: x => x.entidad_id,
                        principalSchema: "operacion",
                        principalTable: "entidad",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_entidad_inmueble_38",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "entidad_parte",
                schema: "operacion",
                columns: table => new
                {
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entidad_parte", x => new { x.entidad_id, x.parte_id, x.fecha_creacion });
                    table.CheckConstraint("ck_entidad_parte_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_entidad_parte_39",
                        column: x => x.entidad_id,
                        principalSchema: "operacion",
                        principalTable: "entidad",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_entidad_parte_40",
                        column: x => x.parte_id,
                        principalSchema: "operacion",
                        principalTable: "parte",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "valor_atributo",
                schema: "operacion",
                columns: table => new
                {
                    entidad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    atributo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_texto = table.Column<string>(type: "text", nullable: true),
                    valor_numero = table.Column<decimal>(type: "numeric", nullable: true),
                    valor_fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    valor_booleano = table.Column<bool>(type: "boolean", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_valor_atributo", x => new { x.entidad_id, x.atributo_id });
                    table.CheckConstraint("ck_valor_atributo_1", "num_nonnulls(valor_texto, valor_numero, valor_fecha, valor_booleano) = 1");
                    table.CheckConstraint("ck_valor_atributo_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_valor_atributo_35",
                        column: x => x.entidad_id,
                        principalSchema: "operacion",
                        principalTable: "entidad",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_valor_atributo_36",
                        column: x => x.atributo_id,
                        principalSchema: "operacion",
                        principalTable: "atributo",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "relacion",
                schema: "operacion",
                columns: table => new
                {
                    tipo_relacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destino_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relacion", x => new { x.tipo_relacion_id, x.origen_id, x.destino_id, x.desde });
                    table.CheckConstraint("ck_relacion_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.CheckConstraint("ck_relacion_2", "hasta IS NULL OR hasta > desde");
                    table.ForeignKey(
                        name: "fk_relacion_31",
                        column: x => x.tipo_relacion_id,
                        principalSchema: "operacion",
                        principalTable: "tipo_relacion",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_relacion_32",
                        column: x => x.origen_id,
                        principalSchema: "operacion",
                        principalTable: "entidad",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_relacion_33",
                        column: x => x.destino_id,
                        principalSchema: "operacion",
                        principalTable: "entidad",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contrato_documento",
                schema: "operacion",
                columns: table => new
                {
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contrato_documento", x => new { x.contrato_id, x.documento_id, x.fecha_creacion });
                    table.CheckConstraint("ck_contrato_documento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_contrato_documento_12",
                        column: x => x.contrato_id,
                        principalSchema: "operacion",
                        principalTable: "contrato",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_contrato_documento_13",
                        column: x => x.documento_id,
                        principalSchema: "operacion",
                        principalTable: "documento",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contrato_parte",
                schema: "operacion",
                columns: table => new
                {
                    contrato_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parte_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "text", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contrato_parte", x => new { x.contrato_id, x.parte_id, x.rol, x.fecha_creacion });
                    table.CheckConstraint("ck_contrato_parte_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_contrato_parte_7",
                        column: x => x.contrato_id,
                        principalSchema: "operacion",
                        principalTable: "contrato",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_contrato_parte_8",
                        column: x => x.parte_id,
                        principalSchema: "operacion",
                        principalTable: "parte",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cierre",
                schema: "operacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inmueble_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mes = table.Column<DateOnly>(type: "date", nullable: false),
                    moneda = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    version_publicada = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierre", x => x.id);
                    table.CheckConstraint("ck_cierre_1", "EXTRACT(DAY FROM mes) = 1");
                    table.CheckConstraint("ck_cierre_2", "version_publicada > 0");
                    table.CheckConstraint("ck_cierre_3", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_cierre_19",
                        column: x => x.inmueble_id,
                        principalSchema: "operacion",
                        principalTable: "inmueble",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cierre_version",
                schema: "operacion",
                columns: table => new
                {
                    cierre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    creado_por_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierre_version", x => new { x.cierre_id, x.version });
                    table.CheckConstraint("ck_cierre_version_1", "version > 0");
                    table.CheckConstraint("ck_cierre_version_2", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_cierre_version_21",
                        column: x => x.cierre_id,
                        principalSchema: "operacion",
                        principalTable: "cierre",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cierre_linea",
                schema: "operacion",
                columns: table => new
                {
                    cierre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movimiento_version = table.Column<int>(type: "integer", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierre_linea", x => new { x.cierre_id, x.version, x.movimiento_id });
                    table.CheckConstraint("ck_cierre_linea_1", "version > 0");
                    table.CheckConstraint("ck_cierre_linea_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_cierre_linea_22",
                        columns: x => new { x.cierre_id, x.version },
                        principalSchema: "operacion",
                        principalTable: "cierre_version",
                        principalColumns: new[] { "cierre_id", "version" });
                    table.ForeignKey(
                        name: "fk_cierre_linea_23",
                        columns: x => new { x.movimiento_id, x.movimiento_version },
                        principalSchema: "operacion",
                        principalTable: "movimiento_version",
                        principalColumns: new[] { "movimiento_id", "version" });
                });

            migrationBuilder.CreateTable(
                name: "reporte_cierre",
                schema: "operacion",
                columns: table => new
                {
                    cierre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    formato = table.Column<string>(type: "text", nullable: false),
                    objeto = table.Column<string>(type: "text", nullable: true),
                    generado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hash = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reporte_cierre", x => new { x.cierre_id, x.version, x.formato });
                    table.CheckConstraint("ck_reporte_cierre_1", "version > 0");
                    table.CheckConstraint("ck_reporte_cierre_2", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_reporte_cierre_27",
                        columns: x => new { x.cierre_id, x.version },
                        principalSchema: "operacion",
                        principalTable: "cierre_version",
                        principalColumns: new[] { "cierre_id", "version" });
                });

            migrationBuilder.CreateTable(
                name: "cierre_evidencia",
                schema: "operacion",
                columns: table => new
                {
                    cierre_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento_version = table.Column<int>(type: "integer", nullable: false),
                    vinculo_creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierre_evidencia", x => new { x.cierre_id, x.version, x.movimiento_id, x.documento_id, x.documento_version });
                    table.CheckConstraint("ck_cierre_evidencia_1", "version > 0");
                    table.CheckConstraint("ck_cierre_evidencia_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_cierre_evidencia_24",
                        columns: x => new { x.cierre_id, x.version, x.movimiento_id },
                        principalSchema: "operacion",
                        principalTable: "cierre_linea",
                        principalColumns: new[] { "cierre_id", "version", "movimiento_id" });
                    table.ForeignKey(
                        name: "fk_cierre_evidencia_25",
                        columns: x => new { x.movimiento_id, x.documento_id, x.vinculo_creado_en },
                        principalSchema: "operacion",
                        principalTable: "movimiento_documento",
                        principalColumns: new[] { "movimiento_id", "documento_id", "fecha_creacion" });
                    table.ForeignKey(
                        name: "fk_cierre_evidencia_26",
                        columns: x => new { x.documento_id, x.documento_version },
                        principalSchema: "operacion",
                        principalTable: "documento_version",
                        principalColumns: new[] { "documento_id", "version" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_atributo_tipo_entidad_id",
                schema: "operacion",
                table: "atributo",
                column: "tipo_entidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaluo_documento_id_documento_version",
                schema: "operacion",
                table: "avaluo",
                columns: new[] { "documento_id", "documento_version" });

            migrationBuilder.CreateIndex(
                name: "IX_avaluo_inmueble_id",
                schema: "operacion",
                table: "avaluo",
                column: "inmueble_id");

            migrationBuilder.CreateIndex(
                name: "IX_avaluo_tasador_id",
                schema: "operacion",
                table: "avaluo",
                column: "tasador_id");

            migrationBuilder.CreateIndex(
                name: "categoria_codigo_key",
                schema: "operacion",
                table: "categoria",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "cierre_inmueble_id_mes_moneda_key",
                schema: "operacion",
                table: "cierre",
                columns: new[] { "inmueble_id", "mes", "moneda" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cierre_id_version_publicada",
                schema: "operacion",
                table: "cierre",
                columns: new[] { "id", "version_publicada" });

            migrationBuilder.CreateIndex(
                name: "IX_cierre_evidencia_documento_id_documento_version",
                schema: "operacion",
                table: "cierre_evidencia",
                columns: new[] { "documento_id", "documento_version" });

            migrationBuilder.CreateIndex(
                name: "IX_cierre_evidencia_movimiento_id_documento_id_vinculo_creado_~",
                schema: "operacion",
                table: "cierre_evidencia",
                columns: new[] { "movimiento_id", "documento_id", "vinculo_creado_en" });

            migrationBuilder.CreateIndex(
                name: "IX_cierre_linea_movimiento_id_movimiento_version",
                schema: "operacion",
                table: "cierre_linea",
                columns: new[] { "movimiento_id", "movimiento_version" });

            migrationBuilder.CreateIndex(
                name: "IX_contrato_unidad_id",
                schema: "operacion",
                table: "contrato",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_contrato_documento_documento_id",
                schema: "operacion",
                table: "contrato_documento",
                column: "documento_id");

            migrationBuilder.CreateIndex(
                name: "uq_contrato_documento_vigente",
                schema: "operacion",
                table: "contrato_documento",
                columns: new[] { "contrato_id", "documento_id" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_contrato_parte_parte_id",
                schema: "operacion",
                table: "contrato_parte",
                column: "parte_id");

            migrationBuilder.CreateIndex(
                name: "uq_contrato_parte_vigente",
                schema: "operacion",
                table: "contrato_parte",
                columns: new[] { "contrato_id", "parte_id", "rol" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_entidad_tipo_id",
                schema: "operacion",
                table: "entidad",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "IX_entidad_inmueble_inmueble_id",
                schema: "operacion",
                table: "entidad_inmueble",
                column: "inmueble_id");

            migrationBuilder.CreateIndex(
                name: "uq_entidad_inmueble_vigente",
                schema: "operacion",
                table: "entidad_inmueble",
                columns: new[] { "entidad_id", "inmueble_id" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_entidad_parte_parte_id",
                schema: "operacion",
                table: "entidad_parte",
                column: "parte_id");

            migrationBuilder.CreateIndex(
                name: "uq_entidad_parte_vigente",
                schema: "operacion",
                table: "entidad_parte",
                columns: new[] { "entidad_id", "parte_id" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_inmueble_documento_documento_id",
                schema: "operacion",
                table: "inmueble_documento",
                column: "documento_id");

            migrationBuilder.CreateIndex(
                name: "uq_inmueble_documento_vigente",
                schema: "operacion",
                table: "inmueble_documento",
                columns: new[] { "inmueble_id", "documento_id" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_inmueble_revision_inmueble_fecha",
                schema: "operacion",
                table: "inmueble_revision",
                columns: new[] { "inmueble_id", "revisado_en" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_inmueble_id",
                schema: "operacion",
                table: "movimiento",
                column: "inmueble_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_documento_documento_id",
                schema: "operacion",
                table: "movimiento_documento",
                column: "documento_id");

            migrationBuilder.CreateIndex(
                name: "uq_movimiento_documento_vigente",
                schema: "operacion",
                table: "movimiento_documento",
                columns: new[] { "movimiento_id", "documento_id" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_version_categoria_id",
                schema: "operacion",
                table: "movimiento_version",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_permiso_inmueble_inmueble_id",
                schema: "operacion",
                table: "permiso_inmueble",
                column: "inmueble_id");

            migrationBuilder.CreateIndex(
                name: "uq_permiso_inmueble_vigente",
                schema: "operacion",
                table: "permiso_inmueble",
                columns: new[] { "usuario_ref", "inmueble_id", "permiso" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_relacion_destino_id",
                schema: "operacion",
                table: "relacion",
                column: "destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_relacion_origen_id",
                schema: "operacion",
                table: "relacion",
                column: "origen_id");

            migrationBuilder.CreateIndex(
                name: "tipo_entidad_codigo_key",
                schema: "operacion",
                table: "tipo_entidad",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipo_relacion_tipo_destino_id",
                schema: "operacion",
                table: "tipo_relacion",
                column: "tipo_destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_tipo_relacion_tipo_origen_id",
                schema: "operacion",
                table: "tipo_relacion",
                column: "tipo_origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_titularidad_parte_id",
                schema: "operacion",
                table: "titularidad",
                column: "parte_id");

            migrationBuilder.CreateIndex(
                name: "unidad_inmueble_id_codigo_key",
                schema: "operacion",
                table: "unidad",
                columns: new[] { "inmueble_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_parte_parte_id",
                schema: "operacion",
                table: "usuario_parte",
                column: "parte_id");

            migrationBuilder.CreateIndex(
                name: "uq_usuario_parte_vigente",
                schema: "operacion",
                table: "usuario_parte",
                columns: new[] { "usuario_ref", "parte_id" },
                unique: true,
                filter: "fecha_eliminacion IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_valor_atributo_atributo_id",
                schema: "operacion",
                table: "valor_atributo",
                column: "atributo_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cierre_20",
                schema: "operacion",
                table: "cierre",
                columns: new[] { "id", "version_publicada" },
                principalSchema: "operacion",
                principalTable: "cierre_version",
                principalColumns: new[] { "cierre_id", "version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cierre_19",
                schema: "operacion",
                table: "cierre");

            migrationBuilder.DropForeignKey(
                name: "fk_cierre_20",
                schema: "operacion",
                table: "cierre");

            migrationBuilder.DropTable(
                name: "auditoria",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "avaluo",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "cierre_evidencia",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "contrato_documento",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "contrato_parte",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "entidad_inmueble",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "entidad_parte",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "inmueble_documento",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "inmueble_revision",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "permiso_inmueble",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "relacion",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "reporte_cierre",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "titularidad",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "usuario_parte",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "valor_atributo",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "cierre_linea",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "movimiento_documento",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "documento_version",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "contrato",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "tipo_relacion",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "parte",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "entidad",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "atributo",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "movimiento_version",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "documento",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "unidad",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "tipo_entidad",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "movimiento",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "categoria",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "inmueble",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "cierre_version",
                schema: "operacion");

            migrationBuilder.DropTable(
                name: "cierre",
                schema: "operacion");
        }
    }
}
