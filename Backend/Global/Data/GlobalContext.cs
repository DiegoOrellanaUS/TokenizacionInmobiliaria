using Global.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Global.Data
{
    public class GlobalContext(DbContextOptions<GlobalContext> options) : DbContext(options)
    {
        public DbSet<ClienteLectura> ClienteLectura { get; set; } = default!;
        public DbSet<AlcanceConsulta> AlcanceConsulta { get; set; } = default!;
        public DbSet<EventoRecibido> EventoRecibido { get; set; } = default!;
        public DbSet<CompraMensualCliente> CompraMensualCliente { get; set; } = default!;
        public DbSet<CompraMensualClienteVersion> CompraMensualClienteVersion { get; set; } = default!;
        public DbSet<CompraMensualGlobal> CompraMensualGlobal { get; set; } = default!;
        public DbSet<CompraMensualGlobalVersion> CompraMensualGlobalVersion { get; set; } = default!;
        public DbSet<CompraMensualGlobalCambio> CompraMensualGlobalCambio { get; set; } = default!;
        public DbSet<Indicador> Indicador { get; set; } = default!;
        public DbSet<IndicadorCliente> IndicadorCliente { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AlcanceConsulta>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_alcance_consulta_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.operador_ref, fila.cliente_ref, fila.permiso })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_alcance_consulta_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.ClienteLecturaReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_alcance_consulta_1");
            });

            modelBuilder.Entity<EventoRecibido>(entidad =>
            {
                entidad.HasOne(fila => fila.ClienteLecturaReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_evento_recibido_2");
            });

            modelBuilder.Entity<CompraMensualCliente>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_cliente_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_cliente_2", "version_vigente > 0");
                });

                entidad.HasOne(fila => fila.ClienteLecturaReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_cliente_3");

                entidad.HasOne(fila => fila.CompraMensualClienteVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_cliente_5");
            });

            modelBuilder.Entity<CompraMensualClienteVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_cliente_version_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_cliente_version_2", "version_origen > 0");
                    tabla.HasCheckConstraint("ck_compra_mensual_cliente_version_3", "cantidad_compras >= 0");
                });

                entidad.HasOne(fila => fila.CompraMensualCliente)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_cliente_version_4");
            });

            modelBuilder.Entity<CompraMensualGlobal>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_global_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_global_2", "version_vigente > 0");
                });

                entidad.HasOne(fila => fila.CompraMensualGlobalVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_global_7");
            });

            modelBuilder.Entity<CompraMensualGlobalVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_global_version_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_global_version_2", "version > 0");
                    tabla.HasCheckConstraint("ck_compra_mensual_global_version_3", "cantidad_compras >= 0");
                });

                entidad.HasOne(fila => fila.CompraMensualGlobal)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_global_version_6");
            });

            modelBuilder.Entity<CompraMensualGlobalCambio>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_compra_mensual_global_cambio_1", "EXTRACT(DAY FROM mes) = 1");
                    tabla.HasCheckConstraint("ck_compra_mensual_global_cambio_2", "version_global > 0");
                    tabla.HasCheckConstraint("ck_compra_mensual_global_cambio_3", "version_origen > 0");
                    tabla.HasCheckConstraint("ck_compra_mensual_global_cambio_4", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.CompraMensualGlobalVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_global_cambio_8");

                entidad.HasOne(fila => fila.CompraMensualClienteVersion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_compra_mensual_global_cambio_9");
            });

            modelBuilder.Entity<IndicadorCliente>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_indicador_cliente_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.ClienteLecturaReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_indicador_cliente_10");

                entidad.HasOne(fila => fila.Indicador)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_indicador_cliente_11");
            });

            modelBuilder.Entity<ClienteLectura>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cliente_lectura_1", "btrim(estado) <> ''");
                });
            });

            modelBuilder.Entity<Indicador>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_indicador_1", "btrim(estado) <> ''");
                });
            });
        }
    }
}
