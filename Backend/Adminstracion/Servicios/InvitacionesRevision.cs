using System.Security.Cryptography;
using System.Text;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Servicios;

public sealed class InvitacionesRevision(AdminContext context, IConfiguration config)
{
    public const string RolRevisor = "REVISOR_INMUEBLE";
    public const string MensajeInvalida = "El código de revisor no es válido o ya fue utilizado.";
    internal const string ClaveBloqueo = "revision:invitaciones";

    private static readonly string[] CodigosDemo = [
        "UMBRAL-JURADO-8VM4QK", "UMBRAL-JURADO-3XP7DW", "UMBRAL-JURADO-6HJ2NT",
        "UMBRAL-JURADO-9RB5FS", "UMBRAL-JURADO-4CL8ZY", "UMBRAL-JURADO-2WU6MG",
        "UMBRAL-JURADO-7KD3VP", "UMBRAL-JURADO-5NX9HR"
    ];

    private bool Demostracion => config.GetValue<bool>("Revision:Demostracion");
    private IEnumerable<string> CodigosConfigurados =>
        config.GetSection("Revision:Codigos").Get<string[]>() ?? [];

    public async Task<string?> SiguienteDemoAsync(CancellationToken ct)
    {
        if (!Demostracion) return null;
        var hashes = CodigosDemo.Select(Hash).ToArray();
        var usados = await context.InvitacionesRevision.AsNoTracking()
            .Where(i => hashes.Contains(i.CodigoHash)).Select(i => i.CodigoHash).ToListAsync(ct);
        return CodigosDemo.FirstOrDefault(c => !usados.Contains(Hash(c)));
    }

    public Task<bool> FueCanjeadaPorAsync(string codigo, Guid usuarioId, CancellationToken ct)
    {
        var hash = Hash(codigo);
        return context.InvitacionesRevision.AnyAsync(i => i.CodigoHash == hash && i.UsuarioId == usuarioId, ct);
    }

    public async Task<bool> PrepararCanjeAsync(string codigo, Usuario usuario, CancellationToken ct)
    {
        var valor = codigo.Trim();
        if (!CodigosConfigurados.Any(c => string.Equals(c, valor, StringComparison.Ordinal)) &&
            !(Demostracion && CodigosDemo.Contains(valor, StringComparer.Ordinal)))
            return false;

        // El bloqueo cubre la creación inicial del rol y el canje en la transacción del alta.
        await AprovisionarTenant.Bloquear(context, ClaveBloqueo, ct);
        var hash = Hash(valor);
        if (await context.InvitacionesRevision.AnyAsync(i => i.CodigoHash == hash, ct))
            return false;

        var rol = await context.Rol.SingleOrDefaultAsync(r => r.codigo == RolRevisor, ct);
        if (rol is { estado: not "ACTIVO" }) return false;
        if (rol is null)
        {
            rol = new Rol { id = Guid.NewGuid(), codigo = RolRevisor, nombre_visible = "Revisor de inmuebles" };
            context.Rol.Add(rol);
        }

        context.UsuarioRol.Add(new UsuarioRol { Usuario = usuario, Rol = rol, alcance = "GLOBAL" });
        context.InvitacionesRevision.Add(new InvitacionRevisionCanjeada
        {
            CodigoHash = hash, UsuarioId = usuario.id, CanjeadaEn = DateTimeOffset.UtcNow
        });
        return true;
    }

    private static string Hash(string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo.Trim())));
}
