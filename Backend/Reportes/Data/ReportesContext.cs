using Reportes.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Reportes.Data
{
    public class ReportesContext(DbContextOptions<ReportesContext> options) : DbContext(options)
    {
        public DbSet<EventoRecibido> EventoRecibido { get; set; } = default!;
        public DbSet<CompraLectura> CompraLectura { get; set; } = default!;
        public DbSet<CompraMensual> CompraMensual { get; set; } = default!;
        public DbSet<CompraMensualVersion> CompraMensualVersion { get; set; } = default!;
        public DbSet<EventoTotal> EventoTotal { get; set; } = default!;
        public DbSet<Plantilla> Plantilla { get; set; } = default!;
        public DbSet<PlantillaVersion> PlantillaVersion { get; set; } = default!;
        public DbSet<ReporteEmitido> ReporteEmitido { get; set; } = default!;
        public DbSet<ReporteCompraMensual> ReporteCompraMensual { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompraMensual>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_2", "version_vigente > 0");
                });

                entidad.HasOne(fila => fila.CompraMensualVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_2");
            });

            modelBuilder.Entity<CompraMensualVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_version_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_version_2", "version > 0");
                    tabla.HasCheckConstraint("ck_compra_mensual_version_3", "cantidad_compras >= 0");
                });

                entidad.HasOne(fila => fila.CompraMensual)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_version_1");
            });

            modelBuilder.Entity<EventoTotal>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_evento_total_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_evento_total_2", "version_total > 0");
                });

                entidad.HasIndex(fila => new { fila.mes, fila.moneda, fila.version_total })
                    .IsUnique()
                    .HasDatabaseName("evento_total_mes_moneda_version_total_key");

                entidad.HasOne(fila => fila.CompraMensualVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_evento_total_3");
            });

            modelBuilder.Entity<Plantilla>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_plantilla_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.codigo)
                    .IsUnique()
                    .HasDatabaseName("plantilla_codigo_key");
            });

            modelBuilder.Entity<PlantillaVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_plantilla_version_1", "version > 0");
                });

                entidad.HasOne(fila => fila.Plantilla)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_plantilla_version_4");
            });

            modelBuilder.Entity<ReporteEmitido>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_reporte_emitido_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.PlantillaVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reporte_emitido_5");
            });

            modelBuilder.Entity<ReporteCompraMensual>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_reporte_compra_mensual_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_reporte_compra_mensual_2", "version_total > 0");
                    tabla.HasCheckConstraint("ck_reporte_compra_mensual_3", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Reporte)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reporte_compra_mensual_6");

                entidad.HasOne(fila => fila.CompraMensualVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reporte_compra_mensual_7");
            });

            modelBuilder.Entity<CompraLectura>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_lectura_1", "version_origen > 0");
                    tabla.HasCheckConstraint("ck_compra_lectura_2", "EXTRACT(DAY FROM mes) = 1");
                });
            });
        }
    }
}
