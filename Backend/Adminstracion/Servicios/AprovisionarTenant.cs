using System.Text.Json;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Servicios;

public class AprovisionarTenant(AdminContext context, IHttpClientFactory factory,
    IConfiguration configuration, ILogger<AprovisionarTenant> logger)
{
    private static readonly (string Codigo, string Nombre, string Url)[] Servicios =
    [
        ("archivos", "Archivos", "https://localhost:7043"),
        ("expedientes", "Expedientes", "https://localhost:7173")
    ];

    public static Task Bloquear(AdminContext context, string clave, CancellationToken ct) =>
        context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({clave}, 0))", ct);

    public static string SilosPendientes() =>
        JsonSerializer.Serialize(Servicios.ToDictionary(s => s.Codigo, _ => "PENDIENTE"));

    public IQueryable<UsuarioRol> AsignacionesTitular(Guid tenantId) =>
        from asignacion in context.UsuarioRol
        where asignacion.tenant_id == tenantId && asignacion.alcance == "TENANT"
            && asignacion.fecha_eliminacion == null && asignacion.Rol.codigo == "TITULAR_TENANT"
            && asignacion.Tenant!.propietario_id == asignacion.usuario_id
        select asignacion;

    public async Task<Dictionary<string, string>> EstadosSilos(Guid tenantId, CancellationToken ct)
    {
        var json = await AsignacionesTitular(tenantId).Select(r => r.silos_por_servicio).SingleAsync(ct);
        return LeerSilos(json);
    }

    public async Task<bool> Ejecutar(Guid tenantId, CancellationToken ct)
    {
        var listo = true;
        foreach (var servicio in Servicios)
        {
            await using var transaccion = await context.Database.BeginTransactionAsync(ct);
            await Bloquear(context, $"silos:{tenantId:N}", ct);
            var tenantHabilitado = await context.Tenant.AnyAsync(t => t.id == tenantId
                && (t.estado == "ACTIVO" || t.estado == "APROVISIONANDO") && t.Propietario.estado == "ACTIVO", ct);
            if (!tenantHabilitado)
                return false;

            var asignacion = await AsignacionesTitular(tenantId).AsNoTracking().SingleAsync(ct);
            var estados = LeerSilos(asignacion.silos_por_servicio);
            if (estados.GetValueOrDefault(servicio.Codigo) != "LISTO")
            {
                try
                {
                    await PrepararBasesTenant(tenantId, [servicio.Nombre], ct);
                    estados[servicio.Codigo] = "LISTO";
                }
                catch (Exception ex) when (ex is HttpRequestException ||
                    ex is OperationCanceledException && !ct.IsCancellationRequested)
                {
                    listo = false;
                    estados[servicio.Codigo] = "PENDIENTE";
                    logger.LogWarning(ex, "No se pudo preparar {Servicio} del tenant {TenantId}.", servicio.Nombre, tenantId);
                }

                var json = JsonSerializer.Serialize(estados);
                await context.UsuarioRol.Where(r => r.id == asignacion.id)
                    .ExecuteUpdateAsync(r => r.SetProperty(x => x.silos_por_servicio, json), ct);
            }
            await transaccion.CommitAsync(ct);
        }
        return listo;
    }

    public async Task PrepararBasesTenant(Guid tenantId, string[] servicios, CancellationToken ct)
    {
        using var client = factory.CreateClient("AdministracionInterna");
        foreach (var servicio in servicios)
        {
            using var response = await client.PostAsync(
                $"{ObtenerUrl(servicio)}/interno/tenants/{tenantId}/preparar", null, ct);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<Dictionary<string, string>?> EliminarBasesTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var propietarioId = await (
            from t in context.Tenant.AsNoTracking()
            where t.id == tenantId
            select (Guid?)t.propietario_id
        ).FirstOrDefaultAsync(ct);
        if (propietarioId == null)
            return null;

        await using (var bloqueo = await context.Database.BeginTransactionAsync(ct))
        {
            await Bloquear(context, $"usuario:{propietarioId.Value:N}", ct);
            await Bloquear(context, $"silos:{tenantId:N}", ct);

            var marcados = await (
                from t in context.Tenant
                where t.id == tenantId && t.propietario_id == propietarioId.Value
                select t
            ).ExecuteUpdateAsync(t => t.SetProperty(x => x.estado, "BORRANDO_BASES"), ct);
            if (marcados == 0)
                return null;

            await bloqueo.CommitAsync(ct);
        }

        await using var transaccion = await context.Database.BeginTransactionAsync(ct);
        await Bloquear(context, $"silos:{tenantId:N}", ct);
        var titular = await AsignacionesTitular(tenantId).AsNoTracking().FirstOrDefaultAsync(ct);
        var estados = LeerSilos(titular?.silos_por_servicio);
        var resultado = new Dictionary<string, string>();
        foreach (var servicio in Servicios)
        {
            var estado = "BORRADA";
            try
            {
                await EliminarBaseAsync(servicio.Nombre, tenantId, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException ||
                ex is OperationCanceledException && !ct.IsCancellationRequested)
            {
                estado = "PENDIENTE_BORRADO";
                logger.LogWarning(ex, "No se pudo borrar la base de {Servicio} del tenant {TenantId}.", servicio.Nombre, tenantId);
            }

            estados[servicio.Codigo] = estado;
            resultado[servicio.Nombre] = estado;
        }

        if (titular != null)
        {
            var json = JsonSerializer.Serialize(estados);
            await (
                from r in context.UsuarioRol
                where r.id == titular.id
                select r
            ).ExecuteUpdateAsync(r => r.SetProperty(x => x.silos_por_servicio, json), ct);
        }

        var estadoTenant = resultado.Values.All(estado => estado == "BORRADA") ? "BASES_BORRADAS" : "BORRADO_PARCIAL";
        await (
            from t in context.Tenant
            where t.id == tenantId
            select t
        ).ExecuteUpdateAsync(t => t.SetProperty(x => x.estado, estadoTenant), ct);
        await transaccion.CommitAsync(ct);
        return resultado;
    }

    private async Task EliminarBaseAsync(string servicio, Guid tenantId, CancellationToken ct)
    {
        using var client = factory.CreateClient("AdministracionInterna");
        using var response = await client.DeleteAsync($"{ObtenerUrl(servicio)}/interno/tenants/{tenantId}/base", ct);
        response.EnsureSuccessStatusCode();
    }

    private static Dictionary<string, string> LeerSilos(string? json) =>
        json == null ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

    private string ObtenerUrl(string servicio)
    {
        foreach (var destino in Servicios)
        {
            var url = (configuration[$"Aprovisionamiento:{destino.Nombre}"] ?? destino.Url).TrimEnd('/');
            if (destino.Nombre == servicio || string.Equals(destino.Url, servicio, StringComparison.Ordinal)
                || string.Equals(url, servicio, StringComparison.Ordinal))
                return url;
        }
        throw new ArgumentException("Microservicio no permitido.", nameof(servicio));
    }
}
