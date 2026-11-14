using Adminstracion.Dominio;
using Microsoft.EntityFrameworkCore;

public class AdminstracionContext(DbContextOptions<AdminstracionContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuario { get; set; } = default!;
    public DbSet<Subscripcion> Subscripcion { get; set; } = default!;


}
