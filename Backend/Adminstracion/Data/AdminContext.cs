using Adminstracion.Dominio.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Data
{
    public class AdminContext(DbContextOptions<AdminContext> options) : DbContext(options)
    {
        public DbSet<Usuario> Usuario { get; set; } = default!;
        public DbSet<Cliente> Cliente { get; set; } = default!;
        public DbSet<Rol> Rol { get; set; } = default!;
        public DbSet<Membresia> Membresia { get; set; } = default!;
        public DbSet<MembresiaRol> MembresiaRol { get; set; } = default!;
        public DbSet<Plan> Plan { get; set; } = default!;
        public DbSet<PlanVersion> PlanVersion { get; set; } = default!;
        public DbSet<Capacidad> Capacidad { get; set; } = default!;
        public DbSet<PlanCapacidad> PlanCapacidad { get; set; } = default!;
        public DbSet<Suscripcion> Suscripcion { get; set; } = default!;
        public DbSet<CobroSuscripcion> CobroSuscripcion { get; set; } = default!;
        public DbSet<Servicio> Servicio { get; set; } = default!;
        public DbSet<SiloServicio> SiloServicio { get; set; } = default!;
        public DbSet<Aprovisionamiento> Aprovisionamiento { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_usuario_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.sujeto_oidc)
                    .IsUnique()
                    .HasDatabaseName("usuario_sujeto_oidc_key");
            });

            modelBuilder.Entity<Cliente>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cliente_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.codigo)
                    .IsUnique()
                    .HasDatabaseName("cliente_codigo_key");
            });

            modelBuilder.Entity<Rol>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_rol_1", "btrim(nombre_visible) <> ''");
                    tabla.HasCheckConstraint("ck_rol_2", "orden_visual >= 0");
                    tabla.HasCheckConstraint("ck_rol_3", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.codigo)
                    .IsUnique()
                    .HasDatabaseName("rol_codigo_key");
            });

            modelBuilder.Entity<Membresia>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_membresia_1", "btrim(estado) <> ''");
                    tabla.HasCheckConstraint("ck_membresia_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.cliente_id, fila.usuario_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_membresia_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Cliente)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_membresia_1");

                entidad.HasOne(fila => fila.Usuario)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_membresia_2");
            });

            modelBuilder.Entity<MembresiaRol>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_membresia_rol_1", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.HasIndex(fila => new { fila.membresia_id, fila.rol_id })
                    .IsUnique()
                    .HasFilter("fecha_eliminacion IS NULL")
                    .HasDatabaseName("uq_membresia_rol_vigente");

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Membresia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_membresia_rol_3");

                entidad.HasOne(fila => fila.Rol)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_membresia_rol_4");
            });

            modelBuilder.Entity<Plan>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_plan_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.codigo)
                    .IsUnique()
                    .HasDatabaseName("plan_codigo_key");
            });

            modelBuilder.Entity<PlanVersion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_plan_version_1", "version > 0");
                });

                entidad.HasOne(fila => fila.Plan)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_plan_version_5");
            });

            modelBuilder.Entity<PlanCapacidad>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_plan_capacidad_1", "plan_version > 0");
                    tabla.HasCheckConstraint("ck_plan_capacidad_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.PlanVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_plan_capacidad_6");

                entidad.HasOne(fila => fila.Capacidad)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_plan_capacidad_7");
            });

            modelBuilder.Entity<Suscripcion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_suscripcion_1", "plan_version > 0");
                    tabla.HasCheckConstraint("ck_suscripcion_2", "btrim(estado) <> ''");
                    tabla.HasCheckConstraint("ck_suscripcion_3", "hasta IS NULL OR hasta > desde");
                });

                entidad.HasOne(fila => fila.Cliente)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_suscripcion_8");

                entidad.HasOne(fila => fila.PlanVersionReferencia)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_suscripcion_9");
            });

            modelBuilder.Entity<CobroSuscripcion>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_cobro_suscripcion_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => new { fila.proveedor, fila.referencia_externa })
                    .IsUnique()
                    .HasDatabaseName("cobro_suscripcion_proveedor_referencia_externa_key");

                entidad.HasOne(fila => fila.Suscripcion)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_cobro_suscripcion_10");
            });

            modelBuilder.Entity<SiloServicio>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_silo_servicio_1", "btrim(estado) <> ''");
                    tabla.HasCheckConstraint("ck_silo_servicio_2", "fecha_eliminacion IS NULL OR fecha_eliminacion >= fecha_creacion");
                });

                entidad.Property(fila => fila.fecha_creacion).HasDefaultValueSql("now()");

                entidad.HasOne(fila => fila.Cliente)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_silo_servicio_11");

                entidad.HasOne(fila => fila.Servicio)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_silo_servicio_12");
            });

            modelBuilder.Entity<Aprovisionamiento>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_aprovisionamiento_1", "btrim(estado) <> ''");
                });

                entidad.HasIndex(fila => fila.idempotencia)
                    .IsUnique()
                    .HasDatabaseName("aprovisionamiento_idempotencia_key");

                entidad.HasOne(fila => fila.SiloServicio)
                    .WithMany()
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("fk_aprovisionamiento_13");
            });

            modelBuilder.Entity<Capacidad>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_capacidad_1", "btrim(estado) <> ''");
                });
            });

            modelBuilder.Entity<Servicio>(entidad =>
            {
                entidad.ToTable(tabla =>
                {
                    tabla.HasCheckConstraint("ck_servicio_1", "btrim(estado) <> ''");
                });
            });
        }
    }
}
