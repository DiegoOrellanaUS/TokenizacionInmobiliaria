using Tokenizacion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Tokenizacion.Data
{
    public class TokenizacionContext(DbContextOptions<TokenizacionContext> options) : DbContext(options)
    {
        public DbSet<Billetera> Billetera { get; set; } = default!;
        public DbSet<ActivoCadena> ActivoCadena { get; set; } = default!;
        public DbSet<BilleteraHabilitada> BilleteraHabilitada { get; set; } = default!;
        public DbSet<InstruccionCadena> InstruccionCadena { get; set; } = default!;
        public DbSet<TransaccionCadena> TransaccionCadena { get; set; } = default!;
        public DbSet<OperacionCadena> OperacionCadena { get; set; } = default!;
        public DbSet<ConciliacionCadena> ConciliacionCadena { get; set; } = default!;
        public DbSet<ConciliacionSaldo> ConciliacionSaldo { get; set; } = default!;
        public DbSet<InstantaneaSaldo> InstantaneaSaldo { get; set; } = default!;
        public DbSet<InstantaneaSaldoLinea> InstantaneaSaldoLinea { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Billetera>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_billetera_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => new { fila.red, fila.direccion })
                    .IsUnique()
                    .HasDatabaseName("billetera_red_direccion_key");
            });

            modelBuilder.Entity<ActivoCadena>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_activo_cadena_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => new { fila.red, fila.emisor, fila.codigo })
                    .IsUnique()
                    .HasDatabaseName("activo_cadena_red_emisor_codigo_key");
            });

            modelBuilder.Entity<BilleteraHabilitada>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_billetera_habilitada_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                    tabla.HasCheckConstraint("ck_billetera_habilitada_2", "hasta IS NULL OR hasta > desde");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Activo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_billetera_habilitada_1");

                entidad.HasOne(fila => fila.Billetera)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_billetera_habilitada_2");
            });

            modelBuilder.Entity<InstruccionCadena>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_instruccion_cadena_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.idempotencia)
                    .IsUnique()
                    .HasDatabaseName("instruccion_cadena_idempotencia_key");

                entidad.HasOne(fila => fila.Activo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instruccion_cadena_3");

                entidad.HasOne(fila => fila.Origen)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instruccion_cadena_4");

                entidad.HasOne(fila => fila.Destino)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instruccion_cadena_5");
            });

            modelBuilder.Entity<TransaccionCadena>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_transaccion_cadena_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => new { fila.red, fila.hash })
                    .IsUnique()
                    .HasDatabaseName("transaccion_cadena_red_hash_key");
            });

            modelBuilder.Entity<OperacionCadena>(entidad =>
            {
                entidad.HasOne(fila => fila.Transaccion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_operacion_cadena_6");

                entidad.HasOne(fila => fila.Instruccion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_operacion_cadena_7");

                entidad.HasOne(fila => fila.Activo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_operacion_cadena_8");

                entidad.HasOne(fila => fila.Origen)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_operacion_cadena_9");

                entidad.HasOne(fila => fila.Destino)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_operacion_cadena_10");
            });

            modelBuilder.Entity<ConciliacionCadena>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_conciliacion_cadena_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Activo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_conciliacion_cadena_11");
            });

            modelBuilder.Entity<ConciliacionSaldo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_conciliacion_saldo_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Conciliacion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_conciliacion_saldo_12");

                entidad.HasOne(fila => fila.Billetera)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_conciliacion_saldo_13");
            });

            modelBuilder.Entity<InstantaneaSaldo>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_instantanea_saldo_1", "btrim(estado) <> ''");
                });

                entidad.HasOne(fila => fila.Activo)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instantanea_saldo_14");
            });

            modelBuilder.Entity<InstantaneaSaldoLinea>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_instantanea_saldo_linea_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Instantanea)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instantanea_saldo_linea_15");

                entidad.HasOne(fila => fila.Billetera)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_instantanea_saldo_linea_16");
            });
        }
    }
}
