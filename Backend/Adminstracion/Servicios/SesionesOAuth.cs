using System.Security.Cryptography;
using System.Text;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Servicios;

public sealed class SesionesOAuth(AdminContext context)
{
    public static string Secreto() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Desafio(string secreto) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(secreto)));

    private static string Hash(string secreto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secreto)));

    public async Task<Guid?> ValidarAsync(string token, CancellationToken ct)
    {
        if (token.Length != 43) return null;
        var hash = Hash(token);
        var ahora = DateTimeOffset.UtcNow;
        return await (from s in context.SesionesOAuth.AsNoTracking()
                      join u in context.Usuario.AsNoTracking() on s.UsuarioId equals u.id
                      where s.AccessHash == hash && s.VenceAccess > ahora && s.VenceEn > ahora &&
                            s.RevocadoEn == null && u.estado == "ACTIVO"
                      select (Guid?)s.UsuarioId).SingleOrDefaultAsync(ct);
    }

    public Task<SesionActiva?> ConsultarActivaAsync(string? refresh, CancellationToken ct)
    {
        if (refresh is not { Length: 43 }) return Task.FromResult<SesionActiva?>(null);
        var hash = Hash(refresh);
        var ahora = DateTimeOffset.UtcNow;
        return (from s in context.SesionesOAuth.AsNoTracking()
                join u in context.Usuario.AsNoTracking() on s.UsuarioId equals u.id
                where s.TokenHash == hash && s.VenceEn > ahora && s.RevocadoEn == null &&
                      s.UsadoEn == null && u.estado == "ACTIVO"
                select new SesionActiva(u.id, s.FamiliaId, u.emisor_oidc)).SingleOrDefaultAsync(ct);
    }

    public async Task<TokensOAuth> CrearAsync(Guid usuarioId, CancellationToken ct)
    {
        var tokens = Agregar(usuarioId, Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(7));
        await context.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<TokensOAuth?> RenovarAsync(string? refresh, CancellationToken ct)
    {
        if (refresh is not { Length: 43 }) return null;

        var hash = Hash(refresh);
        var familiaId = await BuscarFamilia(hash, ct);
        if (familiaId is null) return null;

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        await AprovisionarTenant.Bloquear(context, "oauth:" + familiaId.Value.ToString("N"), ct);
        var anterior = await context.SesionesOAuth.SingleOrDefaultAsync(s => s.TokenHash == hash, ct);
        var ahora = DateTimeOffset.UtcNow;
        if (anterior is null || anterior.VenceEn <= ahora || anterior.RevocadoEn != null) return null;

        var usuarioActivo = await context.Usuario.AnyAsync(u => u.id == anterior.UsuarioId && u.estado == "ACTIVO", ct);
        if (anterior.UsadoEn != null || !usuarioActivo)
        {
            await RevocarFamilia(familiaId.Value, ahora, ct);
            await tx.CommitAsync(ct);
            return null;
        }

        anterior.UsadoEn = ahora;
        var tokens = Agregar(anterior.UsuarioId, anterior.FamiliaId, anterior.VenceEn);
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return tokens;
    }

    public async Task RevocarAsync(string? refresh, CancellationToken ct)
    {
        if (refresh is not { Length: 43 }) return;
        var familiaId = await BuscarFamilia(Hash(refresh), ct);
        if (familiaId is null) return;

        await using var tx = await context.Database.BeginTransactionAsync(ct);
        await AprovisionarTenant.Bloquear(context, "oauth:" + familiaId.Value.ToString("N"), ct);
        await RevocarFamilia(familiaId.Value, DateTimeOffset.UtcNow, ct);
        await tx.CommitAsync(ct);
    }

    private Task<Guid?> BuscarFamilia(string hash, CancellationToken ct) =>
        (from s in context.SesionesOAuth.AsNoTracking()
         where s.TokenHash == hash
         select (Guid?)s.FamiliaId).SingleOrDefaultAsync(ct);

    private Task<int> RevocarFamilia(Guid familiaId, DateTimeOffset ahora, CancellationToken ct) =>
        context.SesionesOAuth.Where(s => s.FamiliaId == familiaId && s.RevocadoEn == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevocadoEn, ahora), ct);

    private TokensOAuth Agregar(Guid usuarioId, Guid familiaId, DateTimeOffset vence)
    {
        var refresh = Secreto();
        var access = Secreto();
        var venceAccess = DateTimeOffset.UtcNow.AddMinutes(10);
        if (venceAccess > vence) venceAccess = vence;
        context.SesionesOAuth.Add(new SesionOAuth
        {
            TokenHash = Hash(refresh), AccessHash = Hash(access), VenceAccess = venceAccess,
            FamiliaId = familiaId, UsuarioId = usuarioId, VenceEn = vence,
        });
        return new TokensOAuth(usuarioId, access, refresh, vence, venceAccess);
    }

    public sealed record TokensOAuth(Guid UsuarioId, string Access, string Refresh,
        DateTimeOffset VenceRefresh, DateTimeOffset VenceAccess);

    public sealed record SesionActiva(Guid UsuarioId, Guid FamiliaId, string? EmisorPrincipal);
}
