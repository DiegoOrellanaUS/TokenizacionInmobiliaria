using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tokenizacion.Migrations
{
    /// <inheritdoc />
    public partial class InicialTokenizacionInmuebles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tokenizacion");

            migrationBuilder.CreateTable(
                name: "activo_cadena",
                schema: "tokenizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrumento_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    red = table.Column<string>(type: "text", nullable: false),
                    emisor = table.Column<string>(type: "text", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    oferta_maxima_base = table.Column<long>(type: "bigint", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activo_cadena", x => x.id);
                    table.CheckConstraint("ck_activo_cadena_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "billetera",
                schema: "tokenizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titular_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    red = table.Column<string>(type: "text", nullable: false),
                    direccion = table.Column<string>(type: "text", nullable: false),
                    proposito = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billetera", x => x.id);
                    table.CheckConstraint("ck_billetera_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "transaccion_cadena",
                schema: "tokenizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    red = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    confirmada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    numero_ledger = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaccion_cadena", x => x.id);
                    table.CheckConstraint("ck_transaccion_cadena_1", "btrim(estado) <> ''");
                });

            migrationBuilder.CreateTable(
                name: "conciliacion_cadena",
                schema: "tokenizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    corte = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conciliacion_cadena", x => x.id);
                    table.CheckConstraint("ck_conciliacion_cadena_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_conciliacion_cadena_11",
                        column: x => x.activo_id,
                        principalSchema: "tokenizacion",
                        principalTable: "activo_cadena",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "instantanea_saldo",
                schema: "tokenizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_ledger = table.Column<long>(type: "bigint", nullable: true),
                    fecha_corte = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instantanea_saldo", x => x.id);
                    table.CheckConstraint("ck_instantanea_saldo_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_instantanea_saldo_14",
                        column: x => x.activo_id,
                        principalSchema: "tokenizacion",
                        principalTable: "activo_cadena",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "billetera_habilitada",
                schema: "tokenizacion",
                columns: table => new
                {
                    activo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billetera_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billetera_habilitada", x => new { x.activo_id, x.billetera_id, x.desde });
                    table.CheckConstraint("ck_billetera_habilitada_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.CheckConstraint("ck_billetera_habilitada_2", "hasta IS NULL OR hasta > desde");
                    table.ForeignKey(
                        name: "fk_billetera_habilitada_1",
                        column: x => x.activo_id,
                        principalSchema: "tokenizacion",
                        principalTable: "activo_cadena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_billetera_habilitada_2",
                        column: x => x.billetera_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "instruccion_cadena",
                schema: "tokenizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    liquidacion_ref = table.Column<Guid>(type: "uuid", nullable: true),
                    clase = table.Column<string>(type: "text", nullable: true),
                    cantidad_base = table.Column<long>(type: "bigint", nullable: true),
                    idempotencia = table.Column<string>(type: "text", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instruccion_cadena", x => x.id);
                    table.CheckConstraint("ck_instruccion_cadena_1", "btrim(estado) <> ''");
                    table.ForeignKey(
                        name: "fk_instruccion_cadena_3",
                        column: x => x.activo_id,
                        principalSchema: "tokenizacion",
                        principalTable: "activo_cadena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_instruccion_cadena_4",
                        column: x => x.origen_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_instruccion_cadena_5",
                        column: x => x.destino_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "conciliacion_saldo",
                schema: "tokenizacion",
                columns: table => new
                {
                    conciliacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billetera_id = table.Column<Guid>(type: "uuid", nullable: false),
                    esperado_base = table.Column<long>(type: "bigint", nullable: true),
                    observado_base = table.Column<long>(type: "bigint", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conciliacion_saldo", x => new { x.conciliacion_id, x.billetera_id });
                    table.CheckConstraint("ck_conciliacion_saldo_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_conciliacion_saldo_12",
                        column: x => x.conciliacion_id,
                        principalSchema: "tokenizacion",
                        principalTable: "conciliacion_cadena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_conciliacion_saldo_13",
                        column: x => x.billetera_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "instantanea_saldo_linea",
                schema: "tokenizacion",
                columns: table => new
                {
                    instantanea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    billetera_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cantidad_base = table.Column<long>(type: "bigint", nullable: true),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_eliminacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instantanea_saldo_linea", x => new { x.instantanea_id, x.billetera_id });
                    table.CheckConstraint("ck_instantanea_saldo_linea_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    table.ForeignKey(
                        name: "fk_instantanea_saldo_linea_15",
                        column: x => x.instantanea_id,
                        principalSchema: "tokenizacion",
                        principalTable: "instantanea_saldo",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_instantanea_saldo_linea_16",
                        column: x => x.billetera_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "emision_inmueble",
                schema: "tokenizacion",
                columns: table => new
                {
                    inmueble_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    instruccion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por_ref = table.Column<Guid>(type: "uuid", nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    transaccion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sobre_firmado = table.Column<string>(type: "text", nullable: true),
                    hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emision_inmueble", x => x.inmueble_ref);
                    table.ForeignKey(
                        name: "FK_emision_inmueble_instruccion_cadena_instruccion_id",
                        column: x => x.instruccion_id,
                        principalSchema: "tokenizacion",
                        principalTable: "instruccion_cadena",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_emision_inmueble_transaccion_cadena_transaccion_id",
                        column: x => x.transaccion_id,
                        principalSchema: "tokenizacion",
                        principalTable: "transaccion_cadena",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operacion_cadena",
                schema: "tokenizacion",
                columns: table => new
                {
                    transaccion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    indice = table.Column<int>(type: "integer", nullable: false),
                    instruccion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    destino_id = table.Column<Guid>(type: "uuid", nullable: true),
                    clase = table.Column<string>(type: "text", nullable: true),
                    cantidad_base = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operacion_cadena", x => new { x.transaccion_id, x.indice });
                    table.ForeignKey(
                        name: "fk_operacion_cadena_10",
                        column: x => x.destino_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_operacion_cadena_6",
                        column: x => x.transaccion_id,
                        principalSchema: "tokenizacion",
                        principalTable: "transaccion_cadena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_operacion_cadena_7",
                        column: x => x.instruccion_id,
                        principalSchema: "tokenizacion",
                        principalTable: "instruccion_cadena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_operacion_cadena_8",
                        column: x => x.activo_id,
                        principalSchema: "tokenizacion",
                        principalTable: "activo_cadena",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_operacion_cadena_9",
                        column: x => x.origen_id,
                        principalSchema: "tokenizacion",
                        principalTable: "billetera",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "activo_cadena_red_emisor_codigo_key",
                schema: "tokenizacion",
                table: "activo_cadena",
                columns: new[] { "red", "emisor", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "billetera_red_direccion_key",
                schema: "tokenizacion",
                table: "billetera",
                columns: new[] { "red", "direccion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_billetera_habilitada_billetera_id",
                schema: "tokenizacion",
                table: "billetera_habilitada",
                column: "billetera_id");

            migrationBuilder.CreateIndex(
                name: "IX_conciliacion_cadena_activo_id",
                schema: "tokenizacion",
                table: "conciliacion_cadena",
                column: "activo_id");

            migrationBuilder.CreateIndex(
                name: "IX_conciliacion_saldo_billetera_id",
                schema: "tokenizacion",
                table: "conciliacion_saldo",
                column: "billetera_id");

            migrationBuilder.CreateIndex(
                name: "IX_emision_inmueble_instruccion_id",
                schema: "tokenizacion",
                table: "emision_inmueble",
                column: "instruccion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_emision_inmueble_transaccion_id",
                schema: "tokenizacion",
                table: "emision_inmueble",
                column: "transaccion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instantanea_saldo_activo_id",
                schema: "tokenizacion",
                table: "instantanea_saldo",
                column: "activo_id");

            migrationBuilder.CreateIndex(
                name: "IX_instantanea_saldo_linea_billetera_id",
                schema: "tokenizacion",
                table: "instantanea_saldo_linea",
                column: "billetera_id");

            migrationBuilder.CreateIndex(
                name: "instruccion_cadena_idempotencia_key",
                schema: "tokenizacion",
                table: "instruccion_cadena",
                column: "idempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instruccion_cadena_activo_id",
                schema: "tokenizacion",
                table: "instruccion_cadena",
                column: "activo_id");

            migrationBuilder.CreateIndex(
                name: "IX_instruccion_cadena_destino_id",
                schema: "tokenizacion",
                table: "instruccion_cadena",
                column: "destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_instruccion_cadena_origen_id",
                schema: "tokenizacion",
                table: "instruccion_cadena",
                column: "origen_id");

            migrationBuilder.CreateIndex(
                name: "IX_operacion_cadena_activo_id",
                schema: "tokenizacion",
                table: "operacion_cadena",
                column: "activo_id");

            migrationBuilder.CreateIndex(
                name: "IX_operacion_cadena_destino_id",
                schema: "tokenizacion",
                table: "operacion_cadena",
                column: "destino_id");

            migrationBuilder.CreateIndex(
                name: "IX_operacion_cadena_instruccion_id",
                schema: "tokenizacion",
                table: "operacion_cadena",
                column: "instruccion_id");

            migrationBuilder.CreateIndex(
                name: "IX_operacion_cadena_origen_id",
                schema: "tokenizacion",
                table: "operacion_cadena",
                column: "origen_id");

            migrationBuilder.CreateIndex(
                name: "transaccion_cadena_red_hash_key",
                schema: "tokenizacion",
                table: "transaccion_cadena",
                columns: new[] { "red", "hash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billetera_habilitada",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "conciliacion_saldo",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "emision_inmueble",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "instantanea_saldo_linea",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "operacion_cadena",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "conciliacion_cadena",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "instantanea_saldo",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "transaccion_cadena",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "instruccion_cadena",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "activo_cadena",
                schema: "tokenizacion");

            migrationBuilder.DropTable(
                name: "billetera",
                schema: "tokenizacion");
        }
    }
}
