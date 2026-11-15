using CajaPagos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace CajaPagos.Data
{
    public class CajaPagosContext(DbContextOptions<CajaPagosContext> options) : DbContext(options)
    {
        public DbSet<Cuenta> Cuenta { get; set; } = default!;
        public DbSet<Pago> Pago { get; set; } = default!;
        public DbSet<ReservaSaldo> ReservaSaldo { get; set; } = default!;
        public DbSet<Asiento> Asiento { get; set; } = default!;
        public DbSet<Apunte> Apunte { get; set; } = default!;
        public DbSet<PagoAsiento> PagoAsiento { get; set; } = default!;
        public DbSet<ReservaAsiento> ReservaAsiento { get; set; } = default!;
        public DbSet<Conciliacion> Conciliacion { get; set; } = default!;
        public DbSet<ConciliacionPago> ConciliacionPago { get; set; } = default!;
        public DbSet<ConversionPago> ConversionPago { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pago>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_pago_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.idempotencia)
                    .IsUnique()
                    .HasDatabaseName("pago_idempotencia_key");

                entidad.HasIndex(fila => new { fila.proveedor, fila.referencia_externa })
                    .IsUnique()
                    .HasDatabaseName("pago_proveedor_referencia_externa_key");

                entidad.HasOne(fila => fila.Cuenta)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_pago_1");
            });

            modelBuilder.Entity<ReservaSaldo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_reserva_saldo_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.idempotencia)
                    .IsUnique()
                    .HasDatabaseName("reserva_saldo_idempotencia_key");

                entidad.HasOne(fila => fila.Cuenta)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reserva_saldo_2");
            });

            modelBuilder.Entity<Asiento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_asiento_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.idempotencia)
                    .IsUnique()
                    .HasDatabaseName("asiento_idempotencia_key");
            });

            modelBuilder.Entity<Apunte>(entidad =>
            {
                entidad.HasOne(fila => fila.Asiento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_apunte_3");

                entidad.HasOne(fila => fila.Cuenta)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_apunte_4");
            });

            modelBuilder.Entity<PagoAsiento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_pago_asiento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Pago)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_pago_asiento_5");

                entidad.HasOne(fila => fila.Asiento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_pago_asiento_6");
            });

            modelBuilder.Entity<ReservaAsiento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_reserva_asiento_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Reserva)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reserva_asiento_7");

                entidad.HasOne(fila => fila.Asiento)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_reserva_asiento_8");
            });

            modelBuilder.Entity<ConciliacionPago>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_conciliacion_pago_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Conciliacion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_conciliacion_pago_9");

                entidad.HasOne(fila => fila.Pago)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_conciliacion_pago_10");
            });

            modelBuilder.Entity<ConversionPago>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_conversion_pago_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Pago)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_conversion_pago_11");
            });

            modelBuilder.Entity<Cuenta>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cuenta_1", "btrim(estado) <> ''");
                });
            });

            modelBuilder.Entity<Conciliacion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_conciliacion_1", "btrim(estado) <> ''");
                });
            });
        }
    }
}
