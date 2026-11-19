-- Tokenización inmobiliaria | PostgreSQL
-- Fuente: data/base-datos-tokenizacion.d2
--
-- Ejecutar sobre una base vacía. El script crea esquemas y tablas en la
-- base a la que ya estás conectado. Las bases físicas se aprovisionan aparte.
-- Para actualizar una base de Admin existente, usar las migraciones de EF Core.
-- Para instalar solo el esquema de un servicio en su base, descomenta la
-- línea SET y elige admin, operacion, caja, comercio, tokenizacion,
-- reportes o global. Sin SET, el valor todos crea 7 esquemas para desarrollo.
-- Cada microservicio de negocio se instala en su propia base por cliente.
-- Admin y Global se instalan en sus correspondientes bases centrales.
--
-- SET tokenizacion.servicio = 'admin';
--
-- Los nombres de tabla usan snake_case. Los UUID los asigna el servicio.
-- Las referencias externas terminadas en _ref se resuelven en la aplicación.
-- Los estados de negocio se asignan en la aplicación y se guardan como texto.
-- Los permisos, el aprovisionamiento, el balance de asientos, la validación
-- del alcance del cierre, la conciliación y el procesamiento de eventos
-- requieren reglas transaccionales en el servicio que posee los datos.
-- Los filtros de baja lógica son responsabilidad de las consultas de negocio.

BEGIN;

DO $seleccion$
DECLARE
    servicio text := COALESCE(NULLIF(current_setting('tokenizacion.servicio', true), ''), 'todos');
BEGIN
    IF servicio NOT IN ('todos', 'admin', 'operacion', 'caja', 'comercio', 'tokenizacion', 'reportes', 'global') THEN
        RAISE EXCEPTION 'Servicio de instalación inválido: %', servicio;
    END IF;
    PERFORM set_config('tokenizacion.servicio', servicio, true);
END;
$seleccion$;

-- Admin | Usuarios, roles, planes y multitenencia
DO $admin$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'admin') THEN
        CREATE SCHEMA "admin";

        CREATE TABLE admin.plan (
            id uuid NOT NULL,
            codigo text NOT NULL,
            nombre text NOT NULL,
            descripcion text,
            revision integer NOT NULL,
            precio numeric(18,2) NOT NULL,
            moneda character varying(12) NOT NULL,
            periodicidad text NOT NULL,
            max_tokens_propios integer NOT NULL,
            max_tokens_como_cliente integer NOT NULL,
            plan_origen_id uuid,
            plan_destino_id uuid,
            oferta_desde timestamp with time zone,
            oferta_hasta timestamp with time zone,
            estado text NOT NULL,
            CONSTRAINT "PK_plan" PRIMARY KEY (id),
            CONSTRAINT ck_plan_condiciones CHECK (revision > 0 AND precio >= 0 AND max_tokens_propios >= 0 AND max_tokens_como_cliente >= 0),
            CONSTRAINT ck_plan_oferta_fechas CHECK (oferta_hasta > oferta_desde),
            CONSTRAINT ck_plan_transicion CHECK ((plan_origen_id IS NULL OR plan_destino_id IS NOT NULL)
        AND (plan_origen_id IS NULL OR plan_origen_id <> plan_destino_id)
        AND (plan_origen_id IS NULL OR plan_origen_id <> id)
        AND (plan_destino_id IS NULL OR plan_destino_id <> id)),
            CONSTRAINT "FK_plan_plan_plan_destino_id" FOREIGN KEY (plan_destino_id) REFERENCES admin.plan (id) ON DELETE RESTRICT,
            CONSTRAINT "FK_plan_plan_plan_origen_id" FOREIGN KEY (plan_origen_id) REFERENCES admin.plan (id) ON DELETE RESTRICT
        );

        CREATE TABLE admin.rol (
            id uuid NOT NULL,
            codigo text NOT NULL,
            nombre_visible text NOT NULL,
            descripcion text,
            orden_visual integer,
            estado text NOT NULL,
            CONSTRAINT "PK_rol" PRIMARY KEY (id),
            CONSTRAINT ck_rol_orden CHECK (orden_visual >= 0)
        );

        CREATE TABLE admin.usuario (
            id uuid NOT NULL,
            nombres text NOT NULL,
            apellidos text NOT NULL,
            ci text,
            correo_electronico text,
            emisor_oidc text,
            sujeto_oidc text,
            contrasenia_hash text,
            estado text NOT NULL,
            CONSTRAINT "PK_usuario" PRIMARY KEY (id),
            CONSTRAINT ck_usuario_identidad_oidc CHECK ((emisor_oidc IS NULL) = (sujeto_oidc IS NULL))
        );

        CREATE TABLE admin.tenant (
            id uuid NOT NULL,
            propietario_id uuid NOT NULL,
            nombre_comercial text,
            estado text NOT NULL,
            CONSTRAINT "PK_tenant" PRIMARY KEY (id),
            CONSTRAINT "FK_tenant_usuario_propietario_id" FOREIGN KEY (propietario_id) REFERENCES admin.usuario (id) ON DELETE RESTRICT
        );

        CREATE TABLE admin.suscripcion (
            id uuid NOT NULL,
            usuario_id uuid NOT NULL,
            tenant_id uuid NOT NULL,
            plan_id uuid NOT NULL,
            condiciones_contratadas jsonb NOT NULL,
            proveedor_pago text,
            referencia_pago text,
            importe_pagado numeric(18,2),
            pago_confirmado_en timestamp with time zone,
            desde timestamp with time zone NOT NULL,
            hasta timestamp with time zone,
            estado text NOT NULL,
            CONSTRAINT "PK_suscripcion" PRIMARY KEY (id),
            CONSTRAINT ck_suscripcion_pago CHECK ((proveedor_pago IS NULL) = (referencia_pago IS NULL)
        AND (pago_confirmado_en IS NULL OR (referencia_pago IS NOT NULL AND importe_pagado IS NOT NULL))
        AND (importe_pagado IS NULL OR importe_pagado >= 0)),
            CONSTRAINT ck_suscripcion_vigencia CHECK (hasta > desde),
            CONSTRAINT "FK_suscripcion_plan_plan_id" FOREIGN KEY (plan_id) REFERENCES admin.plan (id) ON DELETE RESTRICT,
            CONSTRAINT "FK_suscripcion_tenant_tenant_id" FOREIGN KEY (tenant_id) REFERENCES admin.tenant (id) ON DELETE RESTRICT,
            CONSTRAINT "FK_suscripcion_usuario_usuario_id" FOREIGN KEY (usuario_id) REFERENCES admin.usuario (id) ON DELETE RESTRICT
        );

        CREATE TABLE admin.usuario_rol (
            id uuid NOT NULL,
            usuario_id uuid NOT NULL,
            rol_id uuid NOT NULL,
            alcance text NOT NULL,
            tenant_id uuid,
            token_ref uuid,
            silos_por_servicio jsonb,
            fecha_creacion timestamp with time zone NOT NULL DEFAULT (now()),
            fecha_eliminacion timestamp with time zone,
            CONSTRAINT "PK_usuario_rol" PRIMARY KEY (id),
            CONSTRAINT ck_usuario_rol_alcance CHECK ((alcance = 'GLOBAL' AND tenant_id IS NULL AND token_ref IS NULL)
        OR (alcance = 'TENANT' AND tenant_id IS NOT NULL AND token_ref IS NULL)
        OR (alcance = 'TOKEN' AND tenant_id IS NOT NULL AND token_ref IS NOT NULL)),
            CONSTRAINT ck_usuario_rol_fechas CHECK (fecha_eliminacion >= fecha_creacion),
            CONSTRAINT ck_usuario_rol_silos_alcance CHECK (silos_por_servicio IS NULL OR alcance = 'TENANT'),
            CONSTRAINT "FK_usuario_rol_rol_rol_id" FOREIGN KEY (rol_id) REFERENCES admin.rol (id) ON DELETE RESTRICT,
            CONSTRAINT "FK_usuario_rol_tenant_tenant_id" FOREIGN KEY (tenant_id) REFERENCES admin.tenant (id) ON DELETE RESTRICT,
            CONSTRAINT "FK_usuario_rol_usuario_usuario_id" FOREIGN KEY (usuario_id) REFERENCES admin.usuario (id) ON DELETE RESTRICT
        );

        CREATE UNIQUE INDEX "IX_plan_codigo" ON admin.plan (codigo);

        CREATE INDEX "IX_plan_plan_destino_id" ON admin.plan (plan_destino_id);

        CREATE INDEX "IX_plan_plan_origen_id" ON admin.plan (plan_origen_id);

        CREATE UNIQUE INDEX "IX_rol_codigo" ON admin.rol (codigo);

        CREATE INDEX "IX_suscripcion_plan_id" ON admin.suscripcion (plan_id);

        CREATE UNIQUE INDEX "IX_suscripcion_proveedor_pago_referencia_pago" ON admin.suscripcion (proveedor_pago, referencia_pago);

        CREATE INDEX "IX_suscripcion_tenant_id" ON admin.suscripcion (tenant_id);

        CREATE INDEX "IX_suscripcion_usuario_id" ON admin.suscripcion (usuario_id);

        CREATE UNIQUE INDEX "IX_tenant_propietario_id" ON admin.tenant (propietario_id);

        CREATE UNIQUE INDEX "IX_usuario_ci" ON admin.usuario (ci);

        CREATE UNIQUE INDEX "IX_usuario_correo_electronico" ON admin.usuario (correo_electronico);

        CREATE UNIQUE INDEX "IX_usuario_emisor_oidc_sujeto_oidc" ON admin.usuario (emisor_oidc, sujeto_oidc);

        CREATE INDEX "IX_usuario_rol_rol_id" ON admin.usuario_rol (rol_id);

        CREATE UNIQUE INDEX "IX_usuario_rol_usuario_id_rol_id_tenant_id_token_ref" ON admin.usuario_rol (usuario_id, rol_id, tenant_id, token_ref) NULLS NOT DISTINCT WHERE fecha_eliminacion IS NULL;

        CREATE UNIQUE INDEX uq_usuario_rol_silos_tenant ON admin.usuario_rol (tenant_id) WHERE fecha_eliminacion IS NULL AND silos_por_servicio IS NOT NULL;
    END IF;
END;
$admin$;

-- Inmuebles y Operación | Expediente, alquiler y cierre
DO $operacion$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'operacion') THEN
        CREATE SCHEMA "operacion";

        CREATE TABLE "operacion"."parte" (
            "id" uuid NOT NULL,
            "nombre" text,
            "clase" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."usuario_parte" (
            "usuario_ref" uuid NOT NULL,
            "parte_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("usuario_ref", "parte_id", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_usuario_parte_vigente" ON "operacion"."usuario_parte" ("usuario_ref", "parte_id")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."inmueble" (
            "id" uuid NOT NULL,
            "nombre" text NOT NULL,
            "direccion" text,
            "estado" text NOT NULL,
            "tipo" text,
            "ciudad" text,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."unidad" (
            "id" uuid NOT NULL,
            "inmueble_id" uuid NOT NULL,
            "codigo" text NOT NULL,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            UNIQUE ("inmueble_id", "codigo"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."permiso_inmueble" (
            "usuario_ref" uuid NOT NULL,
            "inmueble_id" uuid NOT NULL,
            "permiso" text NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("usuario_ref", "inmueble_id", "permiso", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_permiso_inmueble_vigente" ON "operacion"."permiso_inmueble" ("usuario_ref", "inmueble_id", "permiso")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."titularidad" (
            "inmueble_id" uuid NOT NULL,
            "parte_id" uuid NOT NULL,
            "desde" date NOT NULL,
            "hasta" date,
            "cuota" numeric,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("inmueble_id", "parte_id", "desde"),
            CHECK (cuota > 0 AND cuota <= 1),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion),
            CHECK (hasta IS NULL OR hasta > desde)
        );

        CREATE TABLE "operacion"."contrato" (
            "id" uuid NOT NULL,
            "unidad_id" uuid NOT NULL,
            "inicio" date,
            "fin" date,
            "moneda" varchar(12),
            "renta" numeric,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."contrato_parte" (
            "contrato_id" uuid NOT NULL,
            "parte_id" uuid NOT NULL,
            "rol" text NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("contrato_id", "parte_id", "rol", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_contrato_parte_vigente" ON "operacion"."contrato_parte" ("contrato_id", "parte_id", "rol")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."documento" (
            "id" uuid NOT NULL,
            "clase" text,
            "creado_por_ref" uuid,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."documento_version" (
            "documento_id" uuid NOT NULL,
            "version" int NOT NULL,
            "objeto" text NOT NULL,
            "hash" text NOT NULL,
            "fecha" timestamptz NOT NULL,
            PRIMARY KEY ("documento_id", "version"),
            CHECK (version > 0)
        );

        CREATE TABLE "operacion"."inmueble_documento" (
            "inmueble_id" uuid NOT NULL,
            "documento_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("inmueble_id", "documento_id", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_inmueble_documento_vigente" ON "operacion"."inmueble_documento" ("inmueble_id", "documento_id")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."contrato_documento" (
            "contrato_id" uuid NOT NULL,
            "documento_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("contrato_id", "documento_id", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_contrato_documento_vigente" ON "operacion"."contrato_documento" ("contrato_id", "documento_id")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."categoria" (
            "id" uuid NOT NULL,
            "codigo" text UNIQUE,
            "nombre" text,
            "naturaleza" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."movimiento" (
            "id" uuid NOT NULL,
            "inmueble_id" uuid NOT NULL,
            "creado_en" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."movimiento_version" (
            "movimiento_id" uuid NOT NULL,
            "version" int NOT NULL,
            "categoria_id" uuid NOT NULL,
            "fecha" date NOT NULL,
            "importe" numeric NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "referencia" text,
            PRIMARY KEY ("movimiento_id", "version"),
            CHECK (version > 0)
        );

        CREATE TABLE "operacion"."movimiento_documento" (
            "movimiento_id" uuid NOT NULL,
            "documento_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("movimiento_id", "documento_id", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_movimiento_documento_vigente" ON "operacion"."movimiento_documento" ("movimiento_id", "documento_id")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."cierre" (
            "id" uuid NOT NULL,
            "inmueble_id" uuid NOT NULL,
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_publicada" int,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            UNIQUE ("inmueble_id", "mes", "moneda"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_publicada > 0),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."cierre_version" (
            "cierre_id" uuid NOT NULL,
            "version" int NOT NULL,
            "creado_por_ref" uuid,
            "fecha" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("cierre_id", "version"),
            CHECK (version > 0),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."cierre_linea" (
            "cierre_id" uuid NOT NULL,
            "version" int NOT NULL,
            "movimiento_id" uuid NOT NULL,
            "movimiento_version" int NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("cierre_id", "version", "movimiento_id"),
            CHECK (version > 0),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "operacion"."cierre_evidencia" (
            "cierre_id" uuid NOT NULL,
            "version" int NOT NULL,
            "movimiento_id" uuid NOT NULL,
            "documento_id" uuid NOT NULL,
            "documento_version" int NOT NULL,
            "vinculo_creado_en" timestamptz NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("cierre_id", "version", "movimiento_id", "documento_id", "documento_version"),
            CHECK (version > 0),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "operacion"."reporte_cierre" (
            "cierre_id" uuid NOT NULL,
            "version" int NOT NULL,
            "formato" text NOT NULL,
            "objeto" text,
            "generado_en" timestamptz,
            "hash" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("cierre_id", "version", "formato"),
            CHECK (version > 0),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."auditoria" (
            "id" uuid NOT NULL,
            "actor_ref" uuid,
            "accion" text,
            "recurso" text,
            "objeto_id" uuid,
            "fecha" timestamptz,
            "correlacion" uuid,
            PRIMARY KEY ("id")
        );

        CREATE TABLE "operacion"."tipo_entidad" (
            "id" uuid NOT NULL,
            "codigo" text UNIQUE,
            "nombre" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."entidad" (
            "id" uuid NOT NULL,
            "tipo_id" uuid NOT NULL,
            "nombre" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."tipo_relacion" (
            "id" uuid NOT NULL,
            "tipo_origen_id" uuid NOT NULL,
            "tipo_destino_id" uuid NOT NULL,
            "codigo" text,
            "nombre_visible" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."relacion" (
            "tipo_relacion_id" uuid NOT NULL,
            "origen_id" uuid NOT NULL,
            "destino_id" uuid NOT NULL,
            "desde" timestamptz NOT NULL,
            "hasta" timestamptz,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("tipo_relacion_id", "origen_id", "destino_id", "desde"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion),
            CHECK (hasta IS NULL OR hasta > desde)
        );

        CREATE TABLE "operacion"."atributo" (
            "id" uuid NOT NULL,
            "tipo_entidad_id" uuid NOT NULL,
            "codigo" text,
            "clase_valor" text,
            "nombre_visible" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "operacion"."valor_atributo" (
            "entidad_id" uuid NOT NULL,
            "atributo_id" uuid NOT NULL,
            "valor_texto" text,
            "valor_numero" numeric,
            "valor_fecha" date,
            "valor_booleano" boolean,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("entidad_id", "atributo_id"),
            CHECK (num_nonnulls(valor_texto, valor_numero, valor_fecha, valor_booleano) = 1),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "operacion"."entidad_inmueble" (
            "entidad_id" uuid NOT NULL,
            "inmueble_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("entidad_id", "inmueble_id", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_entidad_inmueble_vigente" ON "operacion"."entidad_inmueble" ("entidad_id", "inmueble_id")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."entidad_parte" (
            "entidad_id" uuid NOT NULL,
            "parte_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("entidad_id", "parte_id", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_entidad_parte_vigente" ON "operacion"."entidad_parte" ("entidad_id", "parte_id")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "operacion"."avaluo" (
            "id" uuid NOT NULL,
            "inmueble_id" uuid NOT NULL,
            "tasador_id" uuid NOT NULL,
            "metodo" text,
            "fecha_valuacion" date,
            "importe" numeric,
            "moneda" varchar(12),
            "documento_id" uuid NOT NULL,
            "documento_version" int NOT NULL,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        ALTER TABLE "operacion"."usuario_parte" ADD CONSTRAINT "fk_usuario_parte_1"
            FOREIGN KEY ("parte_id") REFERENCES "operacion"."parte" ("id");

        ALTER TABLE "operacion"."unidad" ADD CONSTRAINT "fk_unidad_2"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."permiso_inmueble" ADD CONSTRAINT "fk_permiso_inmueble_3"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."titularidad" ADD CONSTRAINT "fk_titularidad_4"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."titularidad" ADD CONSTRAINT "fk_titularidad_5"
            FOREIGN KEY ("parte_id") REFERENCES "operacion"."parte" ("id");

        ALTER TABLE "operacion"."contrato" ADD CONSTRAINT "fk_contrato_6"
            FOREIGN KEY ("unidad_id") REFERENCES "operacion"."unidad" ("id");

        ALTER TABLE "operacion"."contrato_parte" ADD CONSTRAINT "fk_contrato_parte_7"
            FOREIGN KEY ("contrato_id") REFERENCES "operacion"."contrato" ("id");

        ALTER TABLE "operacion"."contrato_parte" ADD CONSTRAINT "fk_contrato_parte_8"
            FOREIGN KEY ("parte_id") REFERENCES "operacion"."parte" ("id");

        ALTER TABLE "operacion"."documento_version" ADD CONSTRAINT "fk_documento_version_9"
            FOREIGN KEY ("documento_id") REFERENCES "operacion"."documento" ("id");

        ALTER TABLE "operacion"."inmueble_documento" ADD CONSTRAINT "fk_inmueble_documento_10"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."inmueble_documento" ADD CONSTRAINT "fk_inmueble_documento_11"
            FOREIGN KEY ("documento_id") REFERENCES "operacion"."documento" ("id");

        ALTER TABLE "operacion"."contrato_documento" ADD CONSTRAINT "fk_contrato_documento_12"
            FOREIGN KEY ("contrato_id") REFERENCES "operacion"."contrato" ("id");

        ALTER TABLE "operacion"."contrato_documento" ADD CONSTRAINT "fk_contrato_documento_13"
            FOREIGN KEY ("documento_id") REFERENCES "operacion"."documento" ("id");

        ALTER TABLE "operacion"."movimiento" ADD CONSTRAINT "fk_movimiento_14"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."movimiento_version" ADD CONSTRAINT "fk_movimiento_version_15"
            FOREIGN KEY ("movimiento_id") REFERENCES "operacion"."movimiento" ("id");

        ALTER TABLE "operacion"."movimiento_version" ADD CONSTRAINT "fk_movimiento_version_16"
            FOREIGN KEY ("categoria_id") REFERENCES "operacion"."categoria" ("id");

        ALTER TABLE "operacion"."movimiento_documento" ADD CONSTRAINT "fk_movimiento_documento_17"
            FOREIGN KEY ("movimiento_id") REFERENCES "operacion"."movimiento" ("id");

        ALTER TABLE "operacion"."movimiento_documento" ADD CONSTRAINT "fk_movimiento_documento_18"
            FOREIGN KEY ("documento_id") REFERENCES "operacion"."documento" ("id");

        ALTER TABLE "operacion"."cierre" ADD CONSTRAINT "fk_cierre_19"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."cierre" ADD CONSTRAINT "fk_cierre_20"
            FOREIGN KEY ("id", "version_publicada") REFERENCES "operacion"."cierre_version" ("cierre_id", "version");

        ALTER TABLE "operacion"."cierre_version" ADD CONSTRAINT "fk_cierre_version_21"
            FOREIGN KEY ("cierre_id") REFERENCES "operacion"."cierre" ("id");

        ALTER TABLE "operacion"."cierre_linea" ADD CONSTRAINT "fk_cierre_linea_22"
            FOREIGN KEY ("cierre_id", "version") REFERENCES "operacion"."cierre_version" ("cierre_id", "version");

        ALTER TABLE "operacion"."cierre_linea" ADD CONSTRAINT "fk_cierre_linea_23"
            FOREIGN KEY ("movimiento_id", "movimiento_version") REFERENCES "operacion"."movimiento_version" ("movimiento_id", "version");

        ALTER TABLE "operacion"."cierre_evidencia" ADD CONSTRAINT "fk_cierre_evidencia_24"
            FOREIGN KEY ("cierre_id", "version", "movimiento_id") REFERENCES "operacion"."cierre_linea" ("cierre_id", "version", "movimiento_id");

        ALTER TABLE "operacion"."cierre_evidencia" ADD CONSTRAINT "fk_cierre_evidencia_25"
            FOREIGN KEY ("movimiento_id", "documento_id", "vinculo_creado_en") REFERENCES "operacion"."movimiento_documento" ("movimiento_id", "documento_id", "fecha_creacion");

        ALTER TABLE "operacion"."cierre_evidencia" ADD CONSTRAINT "fk_cierre_evidencia_26"
            FOREIGN KEY ("documento_id", "documento_version") REFERENCES "operacion"."documento_version" ("documento_id", "version");

        ALTER TABLE "operacion"."reporte_cierre" ADD CONSTRAINT "fk_reporte_cierre_27"
            FOREIGN KEY ("cierre_id", "version") REFERENCES "operacion"."cierre_version" ("cierre_id", "version");

        ALTER TABLE "operacion"."entidad" ADD CONSTRAINT "fk_entidad_28"
            FOREIGN KEY ("tipo_id") REFERENCES "operacion"."tipo_entidad" ("id");

        ALTER TABLE "operacion"."tipo_relacion" ADD CONSTRAINT "fk_tipo_relacion_29"
            FOREIGN KEY ("tipo_origen_id") REFERENCES "operacion"."tipo_entidad" ("id");

        ALTER TABLE "operacion"."tipo_relacion" ADD CONSTRAINT "fk_tipo_relacion_30"
            FOREIGN KEY ("tipo_destino_id") REFERENCES "operacion"."tipo_entidad" ("id");

        ALTER TABLE "operacion"."relacion" ADD CONSTRAINT "fk_relacion_31"
            FOREIGN KEY ("tipo_relacion_id") REFERENCES "operacion"."tipo_relacion" ("id");

        ALTER TABLE "operacion"."relacion" ADD CONSTRAINT "fk_relacion_32"
            FOREIGN KEY ("origen_id") REFERENCES "operacion"."entidad" ("id");

        ALTER TABLE "operacion"."relacion" ADD CONSTRAINT "fk_relacion_33"
            FOREIGN KEY ("destino_id") REFERENCES "operacion"."entidad" ("id");

        ALTER TABLE "operacion"."atributo" ADD CONSTRAINT "fk_atributo_34"
            FOREIGN KEY ("tipo_entidad_id") REFERENCES "operacion"."tipo_entidad" ("id");

        ALTER TABLE "operacion"."valor_atributo" ADD CONSTRAINT "fk_valor_atributo_35"
            FOREIGN KEY ("entidad_id") REFERENCES "operacion"."entidad" ("id");

        ALTER TABLE "operacion"."valor_atributo" ADD CONSTRAINT "fk_valor_atributo_36"
            FOREIGN KEY ("atributo_id") REFERENCES "operacion"."atributo" ("id");

        ALTER TABLE "operacion"."entidad_inmueble" ADD CONSTRAINT "fk_entidad_inmueble_37"
            FOREIGN KEY ("entidad_id") REFERENCES "operacion"."entidad" ("id");

        ALTER TABLE "operacion"."entidad_inmueble" ADD CONSTRAINT "fk_entidad_inmueble_38"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."entidad_parte" ADD CONSTRAINT "fk_entidad_parte_39"
            FOREIGN KEY ("entidad_id") REFERENCES "operacion"."entidad" ("id");

        ALTER TABLE "operacion"."entidad_parte" ADD CONSTRAINT "fk_entidad_parte_40"
            FOREIGN KEY ("parte_id") REFERENCES "operacion"."parte" ("id");

        ALTER TABLE "operacion"."avaluo" ADD CONSTRAINT "fk_avaluo_41"
            FOREIGN KEY ("inmueble_id") REFERENCES "operacion"."inmueble" ("id");

        ALTER TABLE "operacion"."avaluo" ADD CONSTRAINT "fk_avaluo_42"
            FOREIGN KEY ("tasador_id") REFERENCES "operacion"."parte" ("id");

        ALTER TABLE "operacion"."avaluo" ADD CONSTRAINT "fk_avaluo_43"
            FOREIGN KEY ("documento_id", "documento_version") REFERENCES "operacion"."documento_version" ("documento_id", "version");
    END IF;
END;
$operacion$;

-- Caja y Pagos | Fondos, reservas y libro mayor
DO $caja$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'caja') THEN
        CREATE SCHEMA "caja";

        CREATE TABLE "caja"."cuenta" (
            "id" uuid NOT NULL,
            "titular_ref" uuid,
            "tipo" text NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "caja"."pago" (
            "id" uuid NOT NULL,
            "cuenta_id" uuid NOT NULL,
            "proveedor" text,
            "referencia_externa" text,
            "clase" text,
            "estado" text NOT NULL,
            "importe" numeric NOT NULL,
            "fecha" timestamptz,
            "idempotencia" text UNIQUE,
            PRIMARY KEY ("id"),
            UNIQUE ("proveedor", "referencia_externa"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "caja"."reserva_saldo" (
            "id" uuid NOT NULL,
            "cuenta_id" uuid NOT NULL,
            "orden_ref" uuid,
            "importe" numeric,
            "estado" text NOT NULL,
            "creada_en" timestamptz,
            "vence_en" timestamptz,
            "idempotencia" text UNIQUE,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "caja"."asiento" (
            "id" uuid NOT NULL,
            "registrado_en" timestamptz,
            "estado" text NOT NULL,
            "idempotencia" text UNIQUE,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "caja"."apunte" (
            "asiento_id" uuid NOT NULL,
            "linea" int NOT NULL,
            "cuenta_id" uuid NOT NULL,
            "lado" text NOT NULL,
            "importe" numeric NOT NULL,
            PRIMARY KEY ("asiento_id", "linea")
        );

        CREATE TABLE "caja"."pago_asiento" (
            "pago_id" uuid NOT NULL,
            "asiento_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("pago_id", "asiento_id"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "caja"."reserva_asiento" (
            "reserva_id" uuid NOT NULL,
            "asiento_id" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("reserva_id", "asiento_id"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "caja"."conciliacion" (
            "id" uuid NOT NULL,
            "proveedor" text,
            "corte" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "caja"."conciliacion_pago" (
            "conciliacion_id" uuid NOT NULL,
            "pago_id" uuid NOT NULL,
            "resultado" text,
            "diferencia" numeric,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("conciliacion_id", "pago_id"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "caja"."conversion_pago" (
            "id" uuid NOT NULL,
            "pago_id" uuid,
            "proveedor" text,
            "orden_proveedor" text,
            "moneda_origen" varchar(12),
            "monto_origen" numeric,
            "moneda_destino" varchar(12),
            "monto_destino" numeric,
            "tipo_cambio_ejecutado" numeric,
            "ejecutada_en" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        ALTER TABLE "caja"."pago" ADD CONSTRAINT "fk_pago_1"
            FOREIGN KEY ("cuenta_id") REFERENCES "caja"."cuenta" ("id");

        ALTER TABLE "caja"."reserva_saldo" ADD CONSTRAINT "fk_reserva_saldo_2"
            FOREIGN KEY ("cuenta_id") REFERENCES "caja"."cuenta" ("id");

        ALTER TABLE "caja"."apunte" ADD CONSTRAINT "fk_apunte_3"
            FOREIGN KEY ("asiento_id") REFERENCES "caja"."asiento" ("id");

        ALTER TABLE "caja"."apunte" ADD CONSTRAINT "fk_apunte_4"
            FOREIGN KEY ("cuenta_id") REFERENCES "caja"."cuenta" ("id");

        ALTER TABLE "caja"."pago_asiento" ADD CONSTRAINT "fk_pago_asiento_5"
            FOREIGN KEY ("pago_id") REFERENCES "caja"."pago" ("id");

        ALTER TABLE "caja"."pago_asiento" ADD CONSTRAINT "fk_pago_asiento_6"
            FOREIGN KEY ("asiento_id") REFERENCES "caja"."asiento" ("id");

        ALTER TABLE "caja"."reserva_asiento" ADD CONSTRAINT "fk_reserva_asiento_7"
            FOREIGN KEY ("reserva_id") REFERENCES "caja"."reserva_saldo" ("id");

        ALTER TABLE "caja"."reserva_asiento" ADD CONSTRAINT "fk_reserva_asiento_8"
            FOREIGN KEY ("asiento_id") REFERENCES "caja"."asiento" ("id");

        ALTER TABLE "caja"."conciliacion_pago" ADD CONSTRAINT "fk_conciliacion_pago_9"
            FOREIGN KEY ("conciliacion_id") REFERENCES "caja"."conciliacion" ("id");

        ALTER TABLE "caja"."conciliacion_pago" ADD CONSTRAINT "fk_conciliacion_pago_10"
            FOREIGN KEY ("pago_id") REFERENCES "caja"."pago" ("id");

        ALTER TABLE "caja"."conversion_pago" ADD CONSTRAINT "fk_conversion_pago_11"
            FOREIGN KEY ("pago_id") REFERENCES "caja"."pago" ("id");
    END IF;
END;
$caja$;

-- Comercio e Inversión | KYC, ofertas, órdenes y compras
DO $comercio$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'comercio') THEN
        CREATE SCHEMA "comercio";

        CREATE TABLE "comercio"."participante" (
            "id" uuid NOT NULL,
            "usuario_ref" uuid,
            "parte_ref" uuid,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."kyc_caso" (
            "id" uuid NOT NULL,
            "participante_id" uuid NOT NULL,
            "proveedor" text,
            "referencia_externa" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."kyc_decision" (
            "id" uuid NOT NULL,
            "caso_id" uuid NOT NULL,
            "revisor_ref" uuid,
            "decision" text,
            "fecha" timestamptz,
            PRIMARY KEY ("id")
        );

        CREATE TABLE "comercio"."kyc_evidencia" (
            "id" uuid NOT NULL,
            "caso_id" uuid NOT NULL,
            "tipo" text,
            "objeto_privado" text,
            "hash" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."instrumento" (
            "id" uuid NOT NULL,
            "emisor_id" uuid NOT NULL,
            "derecho" text,
            "unidad" text,
            "nombre_visible" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."instrumento_inmueble" (
            "instrumento_id" uuid NOT NULL,
            "inmueble_ref" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("instrumento_id", "inmueble_ref", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_instrumento_inmueble_vigente" ON "comercio"."instrumento_inmueble" ("instrumento_id", "inmueble_ref")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "comercio"."oferta" (
            "id" uuid NOT NULL,
            "instrumento_id" uuid NOT NULL,
            "oferente_id" uuid NOT NULL,
            "clase" text,
            "estado" text NOT NULL,
            "moneda" varchar(12),
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."oferta_version" (
            "oferta_id" uuid NOT NULL,
            "version" int NOT NULL,
            "precio" numeric,
            "unidades" numeric,
            "fecha" timestamptz,
            PRIMARY KEY ("oferta_id", "version"),
            CHECK (version > 0)
        );

        CREATE TABLE "comercio"."oferta_documento" (
            "oferta_id" uuid NOT NULL,
            "oferta_version" int NOT NULL,
            "documento_ref" uuid NOT NULL,
            "documento_version" int NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("oferta_id", "oferta_version", "documento_ref", "documento_version"),
            CHECK (oferta_version > 0),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "comercio"."orden" (
            "id" uuid NOT NULL,
            "participante_id" uuid NOT NULL,
            "oferta_id" uuid NOT NULL,
            "oferta_version" int NOT NULL,
            "lado" text,
            "cantidad" numeric,
            "precio_limite" numeric,
            "estado" text NOT NULL,
            "idempotencia" text UNIQUE,
            PRIMARY KEY ("id"),
            CHECK (oferta_version > 0),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."vinculo_reserva" (
            "orden_id" uuid NOT NULL,
            "reserva_ref" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("orden_id", "reserva_ref"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "comercio"."ejecucion" (
            "id" uuid NOT NULL,
            "orden_compra_id" uuid NOT NULL,
            "orden_venta_id" uuid,
            "cantidad" numeric,
            "precio" numeric,
            "fecha" timestamptz,
            PRIMARY KEY ("id")
        );

        CREATE TABLE "comercio"."liquidacion" (
            "id" uuid NOT NULL,
            "ejecucion_id" uuid NOT NULL,
            "asiento_ref" uuid,
            "instruccion_cadena_ref" uuid,
            "estado" text NOT NULL,
            "fecha" timestamptz,
            "conversion_ref" uuid,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."compra" (
            "id" uuid NOT NULL,
            "ejecucion_id" uuid NOT NULL UNIQUE,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."compra_version" (
            "compra_id" uuid NOT NULL,
            "version" bigint NOT NULL,
            "estado" text NOT NULL,
            "importe_neto" numeric NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "fecha_efectiva" date NOT NULL,
            "registrada_en" timestamptz,
            PRIMARY KEY ("compra_id", "version"),
            CHECK (version > 0),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."evento_compra" (
            "id" uuid NOT NULL,
            "compra_id" uuid NOT NULL,
            "compra_version" bigint NOT NULL,
            "tipo" text,
            "creado_en" timestamptz,
            "publicado_en" timestamptz,
            PRIMARY KEY ("id"),
            UNIQUE ("compra_id", "compra_version"),
            CHECK (compra_version > 0)
        );

        CREATE TABLE "comercio"."distribucion" (
            "id" uuid NOT NULL,
            "instrumento_id" uuid NOT NULL,
            "fecha_corte" timestamptz,
            "moneda" varchar(12),
            "estado" text NOT NULL,
            "instantanea_ref" uuid,
            "moneda_origen" varchar(12),
            "tipo_cambio_referencia" numeric,
            "fuente_tipo_cambio" text,
            "conversion_ref" uuid,
            "informe_hash" text,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."distribucion_cierre" (
            "distribucion_id" uuid NOT NULL,
            "cierre_ref" uuid NOT NULL,
            "cierre_version" int NOT NULL,
            "importe_aplicado" numeric,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("distribucion_id", "cierre_ref", "cierre_version"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "comercio"."distribucion_linea" (
            "distribucion_id" uuid NOT NULL,
            "participante_id" uuid NOT NULL,
            "unidades_corte_base" bigint,
            "importe_neto" numeric,
            "asiento_ref" uuid,
            "importe_bruto" numeric,
            "retencion" numeric,
            "instruccion_pago_ref" uuid,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("distribucion_id", "participante_id"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "comercio"."valuacion_instrumento" (
            "id" uuid NOT NULL,
            "instrumento_id" uuid NOT NULL,
            "fecha_corte" date,
            "moneda" varchar(12),
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "comercio"."valuacion_version" (
            "valuacion_id" uuid NOT NULL,
            "version" int NOT NULL,
            "unidades_circulacion_base" bigint,
            "publicado_en" timestamptz,
            PRIMARY KEY ("valuacion_id", "version"),
            CHECK (version > 0)
        );

        CREATE TABLE "comercio"."valuacion_componente" (
            "valuacion_id" uuid NOT NULL,
            "version" int NOT NULL,
            "concepto" text NOT NULL,
            "importe_neto" numeric,
            PRIMARY KEY ("valuacion_id", "version", "concepto"),
            CHECK (version > 0)
        );

        CREATE TABLE "comercio"."valuacion_avaluo" (
            "valuacion_id" uuid NOT NULL,
            "version" int NOT NULL,
            "avaluo_ref" uuid NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("valuacion_id", "version", "avaluo_ref"),
            CHECK (version > 0),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "comercio"."cotizacion_compra" (
            "id" uuid NOT NULL,
            "orden_id" uuid NOT NULL,
            "cantidad_base" bigint NOT NULL,
            "precio_unitario" numeric NOT NULL,
            "monto_a_pagar" numeric NOT NULL,
            "moneda_pago" varchar(12) NOT NULL,
            "reservada_en" timestamptz NOT NULL,
            "vence_en" timestamptz NOT NULL,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (vence_en > reservada_en),
            CHECK (cantidad_base > 0),
            CHECK (btrim(estado) <> '')
        );

        ALTER TABLE "comercio"."kyc_caso" ADD CONSTRAINT "fk_kyc_caso_1"
            FOREIGN KEY ("participante_id") REFERENCES "comercio"."participante" ("id");

        ALTER TABLE "comercio"."kyc_decision" ADD CONSTRAINT "fk_kyc_decision_2"
            FOREIGN KEY ("caso_id") REFERENCES "comercio"."kyc_caso" ("id");

        ALTER TABLE "comercio"."kyc_evidencia" ADD CONSTRAINT "fk_kyc_evidencia_3"
            FOREIGN KEY ("caso_id") REFERENCES "comercio"."kyc_caso" ("id");

        ALTER TABLE "comercio"."instrumento" ADD CONSTRAINT "fk_instrumento_4"
            FOREIGN KEY ("emisor_id") REFERENCES "comercio"."participante" ("id");

        ALTER TABLE "comercio"."instrumento_inmueble" ADD CONSTRAINT "fk_instrumento_inmueble_5"
            FOREIGN KEY ("instrumento_id") REFERENCES "comercio"."instrumento" ("id");

        ALTER TABLE "comercio"."oferta" ADD CONSTRAINT "fk_oferta_6"
            FOREIGN KEY ("instrumento_id") REFERENCES "comercio"."instrumento" ("id");

        ALTER TABLE "comercio"."oferta" ADD CONSTRAINT "fk_oferta_7"
            FOREIGN KEY ("oferente_id") REFERENCES "comercio"."participante" ("id");

        ALTER TABLE "comercio"."oferta_version" ADD CONSTRAINT "fk_oferta_version_8"
            FOREIGN KEY ("oferta_id") REFERENCES "comercio"."oferta" ("id");

        ALTER TABLE "comercio"."oferta_documento" ADD CONSTRAINT "fk_oferta_documento_9"
            FOREIGN KEY ("oferta_id", "oferta_version") REFERENCES "comercio"."oferta_version" ("oferta_id", "version");

        ALTER TABLE "comercio"."orden" ADD CONSTRAINT "fk_orden_10"
            FOREIGN KEY ("participante_id") REFERENCES "comercio"."participante" ("id");

        ALTER TABLE "comercio"."orden" ADD CONSTRAINT "fk_orden_11"
            FOREIGN KEY ("oferta_id", "oferta_version") REFERENCES "comercio"."oferta_version" ("oferta_id", "version");

        ALTER TABLE "comercio"."vinculo_reserva" ADD CONSTRAINT "fk_vinculo_reserva_12"
            FOREIGN KEY ("orden_id") REFERENCES "comercio"."orden" ("id");

        ALTER TABLE "comercio"."ejecucion" ADD CONSTRAINT "fk_ejecucion_13"
            FOREIGN KEY ("orden_compra_id") REFERENCES "comercio"."orden" ("id");

        ALTER TABLE "comercio"."ejecucion" ADD CONSTRAINT "fk_ejecucion_14"
            FOREIGN KEY ("orden_venta_id") REFERENCES "comercio"."orden" ("id");

        ALTER TABLE "comercio"."liquidacion" ADD CONSTRAINT "fk_liquidacion_15"
            FOREIGN KEY ("ejecucion_id") REFERENCES "comercio"."ejecucion" ("id");

        ALTER TABLE "comercio"."compra" ADD CONSTRAINT "fk_compra_16"
            FOREIGN KEY ("ejecucion_id") REFERENCES "comercio"."ejecucion" ("id");

        ALTER TABLE "comercio"."compra_version" ADD CONSTRAINT "fk_compra_version_17"
            FOREIGN KEY ("compra_id") REFERENCES "comercio"."compra" ("id");

        ALTER TABLE "comercio"."evento_compra" ADD CONSTRAINT "fk_evento_compra_18"
            FOREIGN KEY ("compra_id", "compra_version") REFERENCES "comercio"."compra_version" ("compra_id", "version");

        ALTER TABLE "comercio"."distribucion" ADD CONSTRAINT "fk_distribucion_19"
            FOREIGN KEY ("instrumento_id") REFERENCES "comercio"."instrumento" ("id");

        ALTER TABLE "comercio"."distribucion_cierre" ADD CONSTRAINT "fk_distribucion_cierre_20"
            FOREIGN KEY ("distribucion_id") REFERENCES "comercio"."distribucion" ("id");

        ALTER TABLE "comercio"."distribucion_linea" ADD CONSTRAINT "fk_distribucion_linea_21"
            FOREIGN KEY ("distribucion_id") REFERENCES "comercio"."distribucion" ("id");

        ALTER TABLE "comercio"."distribucion_linea" ADD CONSTRAINT "fk_distribucion_linea_22"
            FOREIGN KEY ("participante_id") REFERENCES "comercio"."participante" ("id");

        ALTER TABLE "comercio"."valuacion_instrumento" ADD CONSTRAINT "fk_valuacion_instrumento_23"
            FOREIGN KEY ("instrumento_id") REFERENCES "comercio"."instrumento" ("id");

        ALTER TABLE "comercio"."valuacion_version" ADD CONSTRAINT "fk_valuacion_version_24"
            FOREIGN KEY ("valuacion_id") REFERENCES "comercio"."valuacion_instrumento" ("id");

        ALTER TABLE "comercio"."valuacion_componente" ADD CONSTRAINT "fk_valuacion_componente_25"
            FOREIGN KEY ("valuacion_id", "version") REFERENCES "comercio"."valuacion_version" ("valuacion_id", "version");

        ALTER TABLE "comercio"."valuacion_avaluo" ADD CONSTRAINT "fk_valuacion_avaluo_26"
            FOREIGN KEY ("valuacion_id", "version") REFERENCES "comercio"."valuacion_version" ("valuacion_id", "version");

        ALTER TABLE "comercio"."cotizacion_compra" ADD CONSTRAINT "fk_cotizacion_compra_27"
            FOREIGN KEY ("orden_id") REFERENCES "comercio"."orden" ("id");
    END IF;
END;
$comercio$;

-- Tokenización | Emisión, transferencias y conciliación en cadena
DO $tokenizacion$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'tokenizacion') THEN
        CREATE SCHEMA "tokenizacion";

        CREATE TABLE "tokenizacion"."billetera" (
            "id" uuid NOT NULL,
            "titular_ref" uuid,
            "red" text NOT NULL,
            "direccion" text NOT NULL,
            "proposito" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            UNIQUE ("red", "direccion"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "tokenizacion"."activo_cadena" (
            "id" uuid NOT NULL,
            "instrumento_ref" uuid,
            "red" text NOT NULL,
            "emisor" text NOT NULL,
            "codigo" text NOT NULL,
            "oferta_maxima_base" bigint,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            UNIQUE ("red", "emisor", "codigo"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "tokenizacion"."billetera_habilitada" (
            "activo_id" uuid NOT NULL,
            "billetera_id" uuid NOT NULL,
            "desde" timestamptz NOT NULL,
            "hasta" timestamptz,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("activo_id", "billetera_id", "desde"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion),
            CHECK (hasta IS NULL OR hasta > desde)
        );

        CREATE TABLE "tokenizacion"."instruccion_cadena" (
            "id" uuid NOT NULL,
            "activo_id" uuid NOT NULL,
            "origen_id" uuid,
            "destino_id" uuid,
            "liquidacion_ref" uuid,
            "clase" text,
            "cantidad_base" bigint,
            "idempotencia" text UNIQUE,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "tokenizacion"."transaccion_cadena" (
            "id" uuid NOT NULL,
            "red" text NOT NULL,
            "hash" text NOT NULL,
            "confirmada_en" timestamptz,
            "estado" text NOT NULL,
            "numero_ledger" bigint,
            PRIMARY KEY ("id"),
            UNIQUE ("red", "hash"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "tokenizacion"."operacion_cadena" (
            "transaccion_id" uuid NOT NULL,
            "indice" int NOT NULL,
            "instruccion_id" uuid,
            "activo_id" uuid NOT NULL,
            "origen_id" uuid,
            "destino_id" uuid,
            "clase" text,
            "cantidad_base" bigint,
            PRIMARY KEY ("transaccion_id", "indice")
        );

        CREATE TABLE "tokenizacion"."conciliacion_cadena" (
            "id" uuid NOT NULL,
            "activo_id" uuid NOT NULL,
            "corte" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "tokenizacion"."conciliacion_saldo" (
            "conciliacion_id" uuid NOT NULL,
            "billetera_id" uuid NOT NULL,
            "esperado_base" bigint,
            "observado_base" bigint,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("conciliacion_id", "billetera_id"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "tokenizacion"."instantanea_saldo" (
            "id" uuid NOT NULL,
            "activo_id" uuid NOT NULL,
            "numero_ledger" bigint,
            "fecha_corte" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "tokenizacion"."instantanea_saldo_linea" (
            "instantanea_id" uuid NOT NULL,
            "billetera_id" uuid NOT NULL,
            "cantidad_base" bigint,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("instantanea_id", "billetera_id"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        ALTER TABLE "tokenizacion"."billetera_habilitada" ADD CONSTRAINT "fk_billetera_habilitada_1"
            FOREIGN KEY ("activo_id") REFERENCES "tokenizacion"."activo_cadena" ("id");

        ALTER TABLE "tokenizacion"."billetera_habilitada" ADD CONSTRAINT "fk_billetera_habilitada_2"
            FOREIGN KEY ("billetera_id") REFERENCES "tokenizacion"."billetera" ("id");

        ALTER TABLE "tokenizacion"."instruccion_cadena" ADD CONSTRAINT "fk_instruccion_cadena_3"
            FOREIGN KEY ("activo_id") REFERENCES "tokenizacion"."activo_cadena" ("id");

        ALTER TABLE "tokenizacion"."instruccion_cadena" ADD CONSTRAINT "fk_instruccion_cadena_4"
            FOREIGN KEY ("origen_id") REFERENCES "tokenizacion"."billetera" ("id");

        ALTER TABLE "tokenizacion"."instruccion_cadena" ADD CONSTRAINT "fk_instruccion_cadena_5"
            FOREIGN KEY ("destino_id") REFERENCES "tokenizacion"."billetera" ("id");

        ALTER TABLE "tokenizacion"."operacion_cadena" ADD CONSTRAINT "fk_operacion_cadena_6"
            FOREIGN KEY ("transaccion_id") REFERENCES "tokenizacion"."transaccion_cadena" ("id");

        ALTER TABLE "tokenizacion"."operacion_cadena" ADD CONSTRAINT "fk_operacion_cadena_7"
            FOREIGN KEY ("instruccion_id") REFERENCES "tokenizacion"."instruccion_cadena" ("id");

        ALTER TABLE "tokenizacion"."operacion_cadena" ADD CONSTRAINT "fk_operacion_cadena_8"
            FOREIGN KEY ("activo_id") REFERENCES "tokenizacion"."activo_cadena" ("id");

        ALTER TABLE "tokenizacion"."operacion_cadena" ADD CONSTRAINT "fk_operacion_cadena_9"
            FOREIGN KEY ("origen_id") REFERENCES "tokenizacion"."billetera" ("id");

        ALTER TABLE "tokenizacion"."operacion_cadena" ADD CONSTRAINT "fk_operacion_cadena_10"
            FOREIGN KEY ("destino_id") REFERENCES "tokenizacion"."billetera" ("id");

        ALTER TABLE "tokenizacion"."conciliacion_cadena" ADD CONSTRAINT "fk_conciliacion_cadena_11"
            FOREIGN KEY ("activo_id") REFERENCES "tokenizacion"."activo_cadena" ("id");

        ALTER TABLE "tokenizacion"."conciliacion_saldo" ADD CONSTRAINT "fk_conciliacion_saldo_12"
            FOREIGN KEY ("conciliacion_id") REFERENCES "tokenizacion"."conciliacion_cadena" ("id");

        ALTER TABLE "tokenizacion"."conciliacion_saldo" ADD CONSTRAINT "fk_conciliacion_saldo_13"
            FOREIGN KEY ("billetera_id") REFERENCES "tokenizacion"."billetera" ("id");

        ALTER TABLE "tokenizacion"."instantanea_saldo" ADD CONSTRAINT "fk_instantanea_saldo_14"
            FOREIGN KEY ("activo_id") REFERENCES "tokenizacion"."activo_cadena" ("id");

        ALTER TABLE "tokenizacion"."instantanea_saldo_linea" ADD CONSTRAINT "fk_instantanea_saldo_linea_15"
            FOREIGN KEY ("instantanea_id") REFERENCES "tokenizacion"."instantanea_saldo" ("id");

        ALTER TABLE "tokenizacion"."instantanea_saldo_linea" ADD CONSTRAINT "fk_instantanea_saldo_linea_16"
            FOREIGN KEY ("billetera_id") REFERENCES "tokenizacion"."billetera" ("id");
    END IF;
END;
$tokenizacion$;

-- Reportes | Proyecciones y plantillas por silo
DO $reportes$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'reportes') THEN
        CREATE SCHEMA "reportes";

        CREATE TABLE "reportes"."evento_recibido" (
            "fuente" text NOT NULL,
            "evento_ref" uuid NOT NULL,
            "procesado_en" timestamptz,
            PRIMARY KEY ("fuente", "evento_ref")
        );

        CREATE TABLE "reportes"."compra_lectura" (
            "compra_ref" uuid NOT NULL,
            "version_origen" bigint NOT NULL,
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "cuenta_en_total" boolean NOT NULL,
            "importe_neto" numeric NOT NULL,
            "actualizado_en" timestamptz,
            PRIMARY KEY ("compra_ref"),
            CHECK (version_origen > 0),
            CHECK (EXTRACT(DAY FROM mes) = 1)
        );

        CREATE TABLE "reportes"."compra_mensual" (
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_vigente" bigint,
            PRIMARY KEY ("mes", "moneda"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_vigente > 0)
        );

        CREATE TABLE "reportes"."compra_mensual_version" (
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version" bigint NOT NULL,
            "cantidad_compras" bigint NOT NULL,
            "importe_neto" numeric NOT NULL,
            "calculado_en" timestamptz,
            PRIMARY KEY ("mes", "moneda", "version"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version > 0),
            CHECK (cantidad_compras >= 0)
        );

        CREATE TABLE "reportes"."evento_total" (
            "id" uuid NOT NULL,
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_total" bigint NOT NULL,
            "creado_en" timestamptz,
            "publicado_en" timestamptz,
            PRIMARY KEY ("id"),
            UNIQUE ("mes", "moneda", "version_total"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_total > 0)
        );

        CREATE TABLE "reportes"."plantilla" (
            "id" uuid NOT NULL,
            "codigo" text UNIQUE,
            "nombre" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "reportes"."plantilla_version" (
            "plantilla_id" uuid NOT NULL,
            "version" int NOT NULL,
            "definicion" jsonb,
            "creada_en" timestamptz,
            PRIMARY KEY ("plantilla_id", "version"),
            CHECK (version > 0)
        );

        CREATE TABLE "reportes"."reporte_emitido" (
            "id" uuid NOT NULL,
            "plantilla_id" uuid NOT NULL,
            "plantilla_version" int NOT NULL,
            "solicitado_por_ref" uuid,
            "formato" text,
            "objeto" text,
            "generado_en" timestamptz,
            "estado" text NOT NULL,
            PRIMARY KEY ("id"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "reportes"."reporte_compra_mensual" (
            "reporte_id" uuid NOT NULL,
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_total" bigint NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("reporte_id", "mes", "moneda"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_total > 0),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        ALTER TABLE "reportes"."compra_mensual_version" ADD CONSTRAINT "fk_compra_mensual_version_1"
            FOREIGN KEY ("mes", "moneda") REFERENCES "reportes"."compra_mensual" ("mes", "moneda");

        ALTER TABLE "reportes"."compra_mensual" ADD CONSTRAINT "fk_compra_mensual_2"
            FOREIGN KEY ("mes", "moneda", "version_vigente") REFERENCES "reportes"."compra_mensual_version" ("mes", "moneda", "version");

        ALTER TABLE "reportes"."evento_total" ADD CONSTRAINT "fk_evento_total_3"
            FOREIGN KEY ("mes", "moneda", "version_total") REFERENCES "reportes"."compra_mensual_version" ("mes", "moneda", "version");

        ALTER TABLE "reportes"."plantilla_version" ADD CONSTRAINT "fk_plantilla_version_4"
            FOREIGN KEY ("plantilla_id") REFERENCES "reportes"."plantilla" ("id");

        ALTER TABLE "reportes"."reporte_emitido" ADD CONSTRAINT "fk_reporte_emitido_5"
            FOREIGN KEY ("plantilla_id", "plantilla_version") REFERENCES "reportes"."plantilla_version" ("plantilla_id", "version");

        ALTER TABLE "reportes"."reporte_compra_mensual" ADD CONSTRAINT "fk_reporte_compra_mensual_6"
            FOREIGN KEY ("reporte_id") REFERENCES "reportes"."reporte_emitido" ("id");

        ALTER TABLE "reportes"."reporte_compra_mensual" ADD CONSTRAINT "fk_reporte_compra_mensual_7"
            FOREIGN KEY ("mes", "moneda", "version_total") REFERENCES "reportes"."compra_mensual_version" ("mes", "moneda", "version");
    END IF;
END;
$reportes$;

-- Global | Totales administrativos de todos los clientes
DO $global$
BEGIN
    IF current_setting('tokenizacion.servicio') IN ('todos', 'global') THEN
        CREATE SCHEMA "global";

        CREATE TABLE "global"."cliente_lectura" (
            "cliente_ref" uuid NOT NULL,
            "nombre_mostrado" text,
            "estado" text NOT NULL,
            "actualizado_en" timestamptz,
            PRIMARY KEY ("cliente_ref"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "global"."alcance_consulta" (
            "operador_ref" uuid NOT NULL,
            "cliente_ref" uuid NOT NULL,
            "permiso" text NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("operador_ref", "cliente_ref", "permiso", "fecha_creacion"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE UNIQUE INDEX "uq_alcance_consulta_vigente" ON "global"."alcance_consulta" ("operador_ref", "cliente_ref", "permiso")
            WHERE fecha_eliminacion IS NULL;

        CREATE TABLE "global"."evento_recibido" (
            "cliente_ref" uuid NOT NULL,
            "evento_ref" uuid NOT NULL,
            "procesado_en" timestamptz,
            PRIMARY KEY ("cliente_ref", "evento_ref")
        );

        CREATE TABLE "global"."compra_mensual_cliente" (
            "cliente_ref" uuid NOT NULL,
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_vigente" bigint,
            PRIMARY KEY ("cliente_ref", "mes", "moneda"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_vigente > 0)
        );

        CREATE TABLE "global"."compra_mensual_cliente_version" (
            "cliente_ref" uuid NOT NULL,
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_origen" bigint NOT NULL,
            "cantidad_compras" bigint NOT NULL,
            "importe_neto" numeric NOT NULL,
            "calculado_en_origen" timestamptz,
            "recibido_en" timestamptz,
            PRIMARY KEY ("cliente_ref", "mes", "moneda", "version_origen"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_origen > 0),
            CHECK (cantidad_compras >= 0)
        );

        CREATE TABLE "global"."compra_mensual_global" (
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_vigente" bigint,
            PRIMARY KEY ("mes", "moneda"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_vigente > 0)
        );

        CREATE TABLE "global"."compra_mensual_global_version" (
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version" bigint NOT NULL,
            "cantidad_compras" bigint NOT NULL,
            "importe_neto" numeric NOT NULL,
            "calculado_en" timestamptz,
            PRIMARY KEY ("mes", "moneda", "version"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version > 0),
            CHECK (cantidad_compras >= 0)
        );

        CREATE TABLE "global"."compra_mensual_global_cambio" (
            "mes" date NOT NULL,
            "moneda" varchar(12) NOT NULL,
            "version_global" bigint NOT NULL,
            "cliente_ref" uuid NOT NULL,
            "version_origen" bigint NOT NULL,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("mes", "moneda", "version_global", "cliente_ref"),
            CHECK (EXTRACT(DAY FROM mes) = 1),
            CHECK (version_global > 0),
            CHECK (version_origen > 0),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        CREATE TABLE "global"."indicador" (
            "codigo" text NOT NULL,
            "nombre" text,
            "unidad" text,
            "estado" text NOT NULL,
            PRIMARY KEY ("codigo"),
            CHECK (btrim(estado) <> '')
        );

        CREATE TABLE "global"."indicador_cliente" (
            "cliente_ref" uuid NOT NULL,
            "indicador_codigo" text NOT NULL,
            "fuente_servicio" text NOT NULL,
            "periodo" date NOT NULL,
            "dimension" text NOT NULL,
            "valor" numeric,
            "actualizado_en" timestamptz,
            "fecha_creacion" timestamptz DEFAULT now() NOT NULL,
            "fecha_eliminacion" timestamptz,
            PRIMARY KEY ("cliente_ref", "indicador_codigo", "fuente_servicio", "periodo", "dimension"),
            CHECK (fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion)
        );

        ALTER TABLE "global"."alcance_consulta" ADD CONSTRAINT "fk_alcance_consulta_1"
            FOREIGN KEY ("cliente_ref") REFERENCES "global"."cliente_lectura" ("cliente_ref");

        ALTER TABLE "global"."evento_recibido" ADD CONSTRAINT "fk_evento_recibido_2"
            FOREIGN KEY ("cliente_ref") REFERENCES "global"."cliente_lectura" ("cliente_ref");

        ALTER TABLE "global"."compra_mensual_cliente" ADD CONSTRAINT "fk_compra_mensual_cliente_3"
            FOREIGN KEY ("cliente_ref") REFERENCES "global"."cliente_lectura" ("cliente_ref");

        ALTER TABLE "global"."compra_mensual_cliente_version" ADD CONSTRAINT "fk_compra_mensual_cliente_version_4"
            FOREIGN KEY ("cliente_ref", "mes", "moneda") REFERENCES "global"."compra_mensual_cliente" ("cliente_ref", "mes", "moneda");

        ALTER TABLE "global"."compra_mensual_cliente" ADD CONSTRAINT "fk_compra_mensual_cliente_5"
            FOREIGN KEY ("cliente_ref", "mes", "moneda", "version_vigente") REFERENCES "global"."compra_mensual_cliente_version" ("cliente_ref", "mes", "moneda", "version_origen");

        ALTER TABLE "global"."compra_mensual_global_version" ADD CONSTRAINT "fk_compra_mensual_global_version_6"
            FOREIGN KEY ("mes", "moneda") REFERENCES "global"."compra_mensual_global" ("mes", "moneda");

        ALTER TABLE "global"."compra_mensual_global" ADD CONSTRAINT "fk_compra_mensual_global_7"
            FOREIGN KEY ("mes", "moneda", "version_vigente") REFERENCES "global"."compra_mensual_global_version" ("mes", "moneda", "version");

        ALTER TABLE "global"."compra_mensual_global_cambio" ADD CONSTRAINT "fk_compra_mensual_global_cambio_8"
            FOREIGN KEY ("mes", "moneda", "version_global") REFERENCES "global"."compra_mensual_global_version" ("mes", "moneda", "version");

        ALTER TABLE "global"."compra_mensual_global_cambio" ADD CONSTRAINT "fk_compra_mensual_global_cambio_9"
            FOREIGN KEY ("cliente_ref", "mes", "moneda", "version_origen") REFERENCES "global"."compra_mensual_cliente_version" ("cliente_ref", "mes", "moneda", "version_origen");

        ALTER TABLE "global"."indicador_cliente" ADD CONSTRAINT "fk_indicador_cliente_10"
            FOREIGN KEY ("cliente_ref") REFERENCES "global"."cliente_lectura" ("cliente_ref");

        ALTER TABLE "global"."indicador_cliente" ADD CONSTRAINT "fk_indicador_cliente_11"
            FOREIGN KEY ("indicador_codigo") REFERENCES "global"."indicador" ("codigo");
    END IF;
END;
$global$;

COMMIT;
