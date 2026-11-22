using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Servicios;

public sealed class PerfilSesion(AdminContext context, IConfiguration configuration)
{
    public async Task<Perfil> ObtenerAsync(Usuario usuario, string token, DateTimeOffset? venceEn, CancellationToken ct,
        bool identidadVerificada = true)
    {
        var tenant = await (from t in context.Tenant.AsNoTracking()
                            where t.propietario_id == usuario.id && t.estado == "ACTIVO"
                            select (Guid?)t.id).SingleOrDefaultAsync(ct);
        return new Perfil(usuario.id, usuario.nombres, usuario.correo_electronico, tenant,
            await EsRevisor(usuario.id, ct), token, venceEn,
            identidadVerificada && await EsAdminGlobal(usuario.id, ct));
    }

    public Task<bool> EsRevisor(Guid usuarioId, CancellationToken ct)
    {
        var autorizado = configuration.GetSection("Inmuebles:Revisores").Get<string[]>()?
            .Any(valor => Guid.TryParse(valor, out var id) && id == usuarioId) == true;
        return (from r in context.UsuarioRol.AsNoTracking()
            where r.usuario_id == usuarioId && r.Usuario.estado == "ACTIVO" && r.fecha_eliminacion == null &&
                r.alcance == "GLOBAL" && r.Rol.estado == "ACTIVO" &&
                (autorizado && (r.Rol.codigo == InvitacionesRevision.RolRevisor || r.Rol.codigo == "SUPER_ADMIN") ||
                 r.Rol.codigo == InvitacionesRevision.RolRevisor &&
                 context.InvitacionesRevision.Any(i => i.UsuarioId == usuarioId))
            select r.id).AnyAsync(ct);
    }

    public Task<bool> EsAdminGlobal(Guid usuarioId, CancellationToken ct)
    {
        var habilitado = configuration.GetSection("Global:Administradores").Get<string[]>()?
            .Any(valor => Guid.TryParse(valor, out var id) && id == usuarioId) == true;
        return !habilitado ? Task.FromResult(false) : context.UsuarioRol.AsNoTracking().AnyAsync(r =>
            r.usuario_id == usuarioId && r.Usuario.estado == "ACTIVO" && r.alcance == "GLOBAL" &&
            r.fecha_eliminacion == null && r.Rol.codigo == "SUPER_ADMIN" && r.Rol.estado == "ACTIVO", ct);
    }

    public sealed record Perfil(Guid UsuarioId, string Nombre, string? Correo, Guid? TenantId,
        bool EsRevisor, string Token, DateTimeOffset? VenceEn, bool EsAdminGlobal);
}
