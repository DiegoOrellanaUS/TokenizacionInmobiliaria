using InmueblesOperacion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace InmueblesOperacion.Data
{
    public class InmueblesOperacionContext(DbContextOptions<InmueblesOperacionContext> options) : DbContext(options)
    {
        public DbSet<Parte> Parte { get; set; } = default!;
        public DbSet<UsuarioParte> UsuarioParte { get; set; } = default!;
        public DbSet<Inmueble> Inmueble { get; set; } = default!;
        public DbSet<Unidad> Unidad { get; set; } = default!;
        public DbSet<PermisoInmueble> PermisoInmueble { get; set; } = default!;
        public DbSet<Titularidad> Titularidad { get; set; } = default!;
        public DbSet<Contrato> Contrato { get; set; } = default!;
        public DbSet<ContratoParte> ContratoParte { get; set; } = default!;
        public DbSet<Documento> Documento { get; set; } = default!;
        public DbSet<DocumentoVersion> DocumentoVersion { get; set; } = default!;
        public DbSet<InmuebleDocumento> InmuebleDocumento { get; set; } = default!;
        public DbSet<ContratoDocumento> ContratoDocumento { get; set; } = default!;
        public DbSet<Categoria> Categoria { get; set; } = default!;
        public DbSet<Movimiento> Movimiento { get; set; } = default!;
        public DbSet<MovimientoVersion> MovimientoVersion { get; set; } = default!;
        public DbSet<MovimientoDocumento> MovimientoDocumento { get; set; } = default!;
        public DbSet<Cierre> Cierre { get; set; } = default!;
        public DbSet<CierreVersion> CierreVersion { get; set; } = default!;
        public DbSet<CierreLinea> CierreLinea { get; set; } = default!;
        public DbSet<CierreEvidencia> CierreEvidencia { get; set; } = default!;
        public DbSet<ReporteCierre> ReporteCierre { get; set; } = default!;
        public DbSet<Auditoria> Auditoria { get; set; } = default!;
        public DbSet<TipoEntidad> TipoEntidad { get; set; } = default!;
        public DbSet<Entidad> Entidad { get; set; } = default!;
        public DbSet<TipoRelacion> TipoRelacion { get; set; } = default!;
        public DbSet<Relacion> Relacion { get; set; } = default!;
        public DbSet<Atributo> Atributo { get; set; } = default!;
        public DbSet<ValorAtributo> ValorAtributo { get; set; } = default!;
        public DbSet<EntidadInmueble> EntidadInmueble { get; set; } = default!;
        public DbSet<EntidadParte> EntidadParte { get; set; } = default!;
        public DbSet<Avaluo> Avaluo { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioParte>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_usuario_parte_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.usuario_ref, fila.parte_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_usuario_parte_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Parte)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_usuario_parte_1");
            });

            modelBuilder.Entity<Unidad>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_unidad_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => new { fila.inmueble_id, fila.codigo })
                    .IsUnique()
                    .HasDatabaseName("unidad_inmueble_id_codigo_key");

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_unidad_2");
            });

            modelBuilder.Entity<PermisoInmueble>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_permiso_inmueble_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.usuario_ref, fila.inmueble_id, fila.permiso })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_permiso_inmueble_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_permiso_inmueble_3");
            });

            modelBuilder.Entity<Titularidad>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_titularidad_1", "cuota > 0 AND cuota <= 1");
                    tabla.HasCheckConstraint("ck_titularidad_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    tabla.HasCheckConstraint("ck_titularidad_3", "hasta IS NULL OR hasta > desde");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_titularidad_4");

                entidad.HasOne(fila => fila.Parte)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_titularidad_5");
            });

            modelBuilder.Entity<Contrato>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_contrato_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Unidad)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_contrato_6");
            });

            modelBuilder.Entity<ContratoParte>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_contrato_parte_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.contrato_id, fila.parte_id, fila.rol })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_contrato_parte_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Contrato)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_contrato_parte_7");

                entidad.HasOne(fila => fila.Parte)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_contrato_parte_8");
            });

            modelBuilder.Entity<DocumentoVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_documento_version_1", "version > 0");
                });

                entidad.HasOne(fila => fila.Documento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_documento_version_9");
            });

            modelBuilder.Entity<InmuebleDocumento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_inmueble_documento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.inmueble_id, fila.documento_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_inmueble_documento_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_inmueble_documento_10");

                entidad.HasOne(fila => fila.Documento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_inmueble_documento_11");
            });

            modelBuilder.Entity<ContratoDocumento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_contrato_documento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.contrato_id, fila.documento_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_contrato_documento_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Contrato)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_contrato_documento_12");

                entidad.HasOne(fila => fila.Documento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_contrato_documento_13");
            });

            modelBuilder.Entity<Categoria>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_categoria_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.codigo)
                    .IsUnique()
                    .HasDatabaseName("categoria_codigo_key");
            });

            modelBuilder.Entity<Movimiento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_movimiento_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_movimiento_14");
            });

            modelBuilder.Entity<MovimientoVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_movimiento_version_1", "version > 0");
                });

                entidad.HasOne(fila => fila.Movimiento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_movimiento_version_15");

                entidad.HasOne(fila => fila.Categoria)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_movimiento_version_16");
            });

            modelBuilder.Entity<MovimientoDocumento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_movimiento_documento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.movimiento_id, fila.documento_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_movimiento_documento_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Movimiento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_movimiento_documento_17");

                entidad.HasOne(fila => fila.Documento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_movimiento_documento_18");
            });

            modelBuilder.Entity<Cierre>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cierre_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_cierre_2", "version_publicada > 0");
                    tabla.HasCheckConstraint("ck_cierre_3", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => new { fila.inmueble_id, fila.mes, fila.moneda })
                    .IsUnique()
                    .HasDatabaseName("cierre_inmueble_id_mes_moneda_key");

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_19");

                entidad.HasOne(fila => fila.CierreVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_20");
            });

            modelBuilder.Entity<CierreVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cierre_version_1", "version > 0");
                    tabla.HasCheckConstraint("ck_cierre_version_2", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Cierre)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_version_21");
            });

            modelBuilder.Entity<CierreLinea>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cierre_linea_1", "version > 0");
                    tabla.HasCheckConstraint("ck_cierre_linea_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.CierreVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_linea_22");

                entidad.HasOne(fila => fila.MovimientoVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_linea_23");
            });

            modelBuilder.Entity<CierreEvidencia>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cierre_evidencia_1", "version > 0");
                    tabla.HasCheckConstraint("ck_cierre_evidencia_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.CierreLinea)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_evidencia_24");

                entidad.HasOne(fila => fila.MovimientoDocumento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_evidencia_25");

                entidad.HasOne(fila => fila.DocumentoVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cierre_evidencia_26");
            });

            modelBuilder.Entity<ReporteCierre>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_reporte_cierre_1", "version > 0");
                    tabla.HasCheckConstraint("ck_reporte_cierre_2", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.CierreVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reporte_cierre_27");
            });

            modelBuilder.Entity<TipoEntidad>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_tipo_entidad_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.codigo)
                    .IsUnique()
                    .HasDatabaseName("tipo_entidad_codigo_key");
            });

            modelBuilder.Entity<Entidad>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_entidad_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Tipo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_entidad_28");
            });

            modelBuilder.Entity<TipoRelacion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_tipo_relacion_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.TipoOrigen)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_tipo_relacion_29");

                entidad.HasOne(fila => fila.TipoDestino)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_tipo_relacion_30");
            });

            modelBuilder.Entity<Relacion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_relacion_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    tabla.HasCheckConstraint("ck_relacion_2", "hasta IS NULL OR hasta > desde");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.TipoRelacion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_relacion_31");

                entidad.HasOne(fila => fila.Origen)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_relacion_32");

                entidad.HasOne(fila => fila.Destino)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_relacion_33");
            });

            modelBuilder.Entity<Atributo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_atributo_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.TipoEntidad)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_atributo_34");
            });

            modelBuilder.Entity<ValorAtributo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_valor_atributo_1", "num_nonnulls(valor_texto, valor_numero, valor_fecha, valor_booleano) = 1");
                    tabla.HasCheckConstraint("ck_valor_atributo_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Entidad)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_valor_atributo_35");

                entidad.HasOne(fila => fila.Atributo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_valor_atributo_36");
            });

            modelBuilder.Entity<EntidadInmueble>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_entidad_inmueble_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.entidad_id, fila.inmueble_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_entidad_inmueble_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Entidad)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_entidad_inmueble_37");

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_entidad_inmueble_38");
            });

            modelBuilder.Entity<EntidadParte>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_entidad_parte_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.entidad_id, fila.parte_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_entidad_parte_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Entidad)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_entidad_parte_39");

                entidad.HasOne(fila => fila.Parte)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_entidad_parte_40");
            });

            modelBuilder.Entity<Avaluo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_avaluo_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Inmueble)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_avaluo_41");

                entidad.HasOne(fila => fila.Tasador)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_avaluo_42");

                entidad.HasOne(fila => fila.DocumentoVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_avaluo_43");
            });

            modelBuilder.Entity<Parte>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_parte_1", "btrim(estado) <> ''");
                });
            });

            modelBuilder.Entity<Inmueble>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_inmueble_1", "btrim(estado) <> ''");
                });
            });

            modelBuilder.Entity<Documento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_documento_1", "btrim(estado) <> ''");
                });
            });
        }
    }
}
