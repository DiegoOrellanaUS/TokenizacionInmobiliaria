using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Servicios;

public sealed class InvitacionRevisionInvalidaException : Exception;

public sealed class IdentidadesOAuth(AdminContext context, IConfiguration config, InvitacionesRevision invitaciones)
{
    public static string Origen(IConfiguration config, string seccion)
    {
        if (!Uri.TryCreate(config[seccion + ":Origen"], UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.PathAndQuery != "/" || uri.Fragment.Length != 0)
            throw new InvalidOperationException($"Configura {seccion}:Origen como un origen HTTPS.");

        return uri.GetLeftPart(UriPartial.Authority);
    }

    public async Task<Usuario?> ObtenerAsync(string seccion, string emisor, string sujeto,
        string? correo, string? nombre, string nombrePredeterminado, CancellationToken ct, string? codigoRevisor = null)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        await AprovisionarTenant.Bloquear(context, emisor + ":" + sujeto, ct);

        var cuenta = await context.Usuario.SingleOrDefaultAsync(
            u => u.emisor_oidc == emisor && u.sujeto_oidc == sujeto, ct);
        if (cuenta is not null && codigoRevisor is not null &&
            !await invitaciones.FueCanjeadaPorAsync(codigoRevisor, cuenta.id, ct))
            throw new InvitacionRevisionInvalidaException();

        if (cuenta is null)
        {
            var vinculo = config[seccion + ":Vinculos:" + sujeto];
            if (vinculo is not null)
            {
                if (codigoRevisor is not null) throw new InvitacionRevisionInvalidaException();
                if (!Guid.TryParse(vinculo, out var existente)) return null;
                await AprovisionarTenant.Bloquear(context, "oauth-usuario:" + existente.ToString("D"), ct);
                cuenta = await context.Usuario.FindAsync([existente], ct);
                if (cuenta is null || cuenta.emisor_oidc is not null) return null;
            }
            else
            {
                correo = correo?.Trim().ToLowerInvariant();
                if (correo is not { Length: > 0 and <= 254 }) return null;
                await AprovisionarTenant.Bloquear(context, "oauth-correo:" + correo, ct);
                if (await context.Usuario.AnyAsync(u => u.correo_electronico == correo, ct)) return null;

                nombre = nombre?.Trim();
                if (string.IsNullOrWhiteSpace(nombre)) nombre = nombrePredeterminado;
                cuenta = new Usuario
                {
                    id = Guid.NewGuid(), nombres = nombre[..Math.Min(nombre.Length, 160)], apellidos = "",
                    correo_electronico = correo,
                };
                context.Usuario.Add(cuenta);
                if (codigoRevisor is not null && !await invitaciones.PrepararCanjeAsync(codigoRevisor, cuenta, ct))
                    throw new InvitacionRevisionInvalidaException();
            }
            cuenta.emisor_oidc = emisor;
            cuenta.sujeto_oidc = sujeto;
        }

        if (cuenta.estado != "ACTIVO") return null;
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return cuenta;
    }
}
