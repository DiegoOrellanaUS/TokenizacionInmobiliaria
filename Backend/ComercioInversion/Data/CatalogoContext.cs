using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ComercioInversion.Data;

public sealed class CatalogoContext(DbContextOptions<CatalogoContext> options) : DbContext(options)
{
    public DbSet<OfertaPublicada> Ofertas => Set<OfertaPublicada>();
    public DbSet<CompraDemo> Compras => Set<CompraDemo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var oferta = modelBuilder.Entity<OfertaPublicada>();
        oferta.ToTable("oferta_publicada", "catalogo", t =>
        {
            t.HasCheckConstraint("ck_precio_oferta", "precio > 0");
            t.HasCheckConstraint("ck_unidades_oferta", "unidades > 0");
            t.HasCheckConstraint("ck_disponibles_oferta", "disponibles >= 0 AND disponibles <= unidades");
        });
        oferta.HasIndex(o => new { o.TenantId, o.InmuebleId }).IsUnique();
        oferta.Property(o => o.Precio).HasColumnName("precio").HasPrecision(18, 2);
        oferta.Property(o => o.Unidades).HasColumnName("unidades");
        oferta.Property(o => o.Disponibles).HasColumnName("disponibles");
        var compra = modelBuilder.Entity<CompraDemo>();
        compra.ToTable("compra_demo", "catalogo");
        compra.HasOne(c => c.Oferta).WithMany().HasForeignKey(c => c.OfertaId).OnDelete(DeleteBehavior.Restrict);
        compra.HasIndex(c => new { c.CompradorId, c.CreadaEn });
        compra.HasIndex(c => new { c.Estado, c.ReintentarEn });
        compra.HasIndex(c => c.HashPago).IsUnique();
        compra.Property(c => c.PrecioUnitario).HasPrecision(18, 2);
        compra.Property(c => c.Moneda).HasMaxLength(3);
        oferta.Property(o => o.Moneda).HasMaxLength(3);
        oferta.Property(o => o.Codigo).HasMaxLength(12);
    }
}

public sealed class OfertaPublicada
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid InmuebleId { get; init; }
    public required string Nombre { get; init; }
    public required string Ciudad { get; init; }
    public required string Tipo { get; init; }
    public required string Codigo { get; init; }
    public long Unidades { get; init; }
    public long Disponibles { get; set; }
    public decimal Precio { get; init; }
    public required string Moneda { get; init; }
    public DateTimeOffset PublicadaEn { get; init; }
}

public sealed class CatalogoContextFactory : IDesignTimeDbContextFactory<CatalogoContext>
{
    public CatalogoContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CatalogoContext>()
            .UseNpgsql("Host=localhost;Database=catalogo_inmobiliario").Options);
}
