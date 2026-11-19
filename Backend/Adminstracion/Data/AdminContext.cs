using Adminstracion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Data;

public class AdminContext(DbContextOptions<AdminContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuario => Set<Usuario>();
    public DbSet<Tenant> Tenant => Set<Tenant>();
    public DbSet<Rol> Rol => Set<Rol>();
    public DbSet<UsuarioRol> UsuarioRol => Set<UsuarioRol>();
    public DbSet<Plan> Plan => Set<Plan>();
    public DbSet<Suscripcion> Suscripcion => Set<Suscripcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admin");

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuario", t => t.HasCheckConstraint("ck_usuario_identidad_oidc",
                "(emisor_oidc IS NULL) = (sujeto_oidc IS NULL)"));
            e.HasKey(x => x.id);
            e.HasIndex(x => x.ci).IsUnique();
            e.HasIndex(x => x.correo_electronico).IsUnique();
            e.HasIndex(x => new { x.emisor_oidc, x.sujeto_oidc }).IsUnique();
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenant");
            e.HasKey(x => x.id);
            e.HasOne(x => x.Propietario).WithOne().HasForeignKey<Tenant>(x => x.propietario_id)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("rol", t => t.HasCheckConstraint("ck_rol_orden", "orden_visual >= 0"));
            e.HasKey(x => x.id);
            e.HasIndex(x => x.codigo).IsUnique();
        });

        modelBuilder.Entity<UsuarioRol>(e =>
        {
            e.ToTable("usuario_rol", t =>
            {
                t.HasCheckConstraint("ck_usuario_rol_alcance", """
                    (alcance = 'GLOBAL' AND tenant_id IS NULL AND token_ref IS NULL)
                    OR (alcance = 'TENANT' AND tenant_id IS NOT NULL AND token_ref IS NULL)
                    OR (alcance = 'TOKEN' AND tenant_id IS NOT NULL AND token_ref IS NOT NULL)
                    """);
                t.HasCheckConstraint("ck_usuario_rol_fechas", "fecha_eliminacion >= fecha_creacion");
                t.HasCheckConstraint("ck_usuario_rol_silos_alcance", "silos_por_servicio IS NULL OR alcance = 'TENANT'");
            });
            e.HasKey(x => x.id);
            e.Property(x => x.fecha_creacion).HasDefaultValueSql("now()");
            e.Property(x => x.silos_por_servicio).HasColumnType("jsonb");
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.usuario_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.rol_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.tenant_id).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.usuario_id, x.rol_id, x.tenant_id, x.token_ref }).IsUnique()
                .AreNullsDistinct(false).HasFilter("fecha_eliminacion IS NULL");
            e.HasIndex(x => x.tenant_id).IsUnique()
                .HasFilter("fecha_eliminacion IS NULL AND silos_por_servicio IS NOT NULL")
                .HasDatabaseName("uq_usuario_rol_silos_tenant");
        });

        modelBuilder.Entity<Plan>(e =>
        {
            e.ToTable("plan", t =>
            {
                t.HasCheckConstraint("ck_plan_condiciones", """
                    revision > 0 AND precio >= 0 AND max_tokens_propios >= 0 AND max_tokens_como_cliente >= 0
                    """);
                t.HasCheckConstraint("ck_plan_oferta_fechas", "oferta_hasta > oferta_desde");
                t.HasCheckConstraint("ck_plan_transicion", """
                    (plan_origen_id IS NULL OR plan_destino_id IS NOT NULL)
                    AND (plan_origen_id IS NULL OR plan_origen_id <> plan_destino_id)
                    AND (plan_origen_id IS NULL OR plan_origen_id <> id)
                    AND (plan_destino_id IS NULL OR plan_destino_id <> id)
                    """);
            });
            e.HasKey(x => x.id);
            e.HasIndex(x => x.codigo).IsUnique();
            e.Property(x => x.precio).HasPrecision(18, 2);
            e.Property(x => x.moneda).HasMaxLength(12);
            e.HasOne(x => x.PlanOrigen).WithMany().HasForeignKey(x => x.plan_origen_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PlanDestino).WithMany().HasForeignKey(x => x.plan_destino_id).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Suscripcion>(e =>
        {
            e.ToTable("suscripcion", t =>
            {
                t.HasCheckConstraint("ck_suscripcion_vigencia", "hasta > desde");
                t.HasCheckConstraint("ck_suscripcion_pago", """
                    (proveedor_pago IS NULL) = (referencia_pago IS NULL)
                    AND (pago_confirmado_en IS NULL OR (referencia_pago IS NOT NULL AND importe_pagado IS NOT NULL))
                    AND (importe_pagado IS NULL OR importe_pagado >= 0)
                    """);
            });
            e.HasKey(x => x.id);
            e.Property(x => x.condiciones_contratadas).HasColumnType("jsonb");
            e.Property(x => x.importe_pagado).HasPrecision(18, 2);
            e.HasIndex(x => new { x.proveedor_pago, x.referencia_pago }).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.usuario_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.tenant_id).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.plan_id).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
