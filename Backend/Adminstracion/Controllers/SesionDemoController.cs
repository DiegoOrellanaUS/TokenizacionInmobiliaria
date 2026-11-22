using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Controllers;

[ApiController]
[Route("api/SesionDemo")]
public sealed class SesionDemoController(AdminContext context, SesionDemo sesiones, PerfilSesion perfiles,
    IWebHostEnvironment environment, IPasswordHasher<Usuario> hasher, InvitacionesRevision invitaciones,
    IHttpClientFactory clientes, IConfiguration config) : ControllerBase
{
    [HttpGet("Modo")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Modo() => Ok(new
    {
        GitHub = sesiones.GitHubHabilitado,
        Facebook = sesiones.FacebookHabilitado,
        Contrasena = !sesiones.SoloOAuth && sesiones.Disponible(HttpContext),
    });

    [HttpGet("CodigoRevisionDemo")]
    [EnableRateLimiting("cuentas")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CodigoRevisionDemo(CancellationToken ct)
    {
        if (!sesiones.Disponible(HttpContext)) return NotFound();
        return Ok(new { Codigo = await invitaciones.SiguienteDemoAsync(ct) });
    }

    [HttpPost("Iniciar", Name = "IniciarSesionDemo")]
    [EnableRateLimiting("cuentas")]
    public async Task<IActionResult> Iniciar(CorreoSesion solicitud, CancellationToken ct)
    {
        if (sesiones.SoloOAuth || !sesiones.Disponible(HttpContext))
            return NotFound();

        var correo = solicitud.Correo.Trim().ToLowerInvariant();
        var usuario = await (from u in context.Usuario.AsNoTracking()
                             where u.correo_electronico == correo && u.estado == "ACTIVO"
                             select u).SingleOrDefaultAsync(ct);
        var requiereContrasenia = environment.IsEnvironment("Piloto") ||
            usuario is not null && (await perfiles.EsRevisor(usuario.id, ct) ||
                await perfiles.EsAdminGlobal(usuario.id, ct));
        if (requiereContrasenia &&
            (usuario is null || usuario.contrasenia_hash is not { } hash || solicitud.Contrasenia is not { Length: >= 12 } ||
             hasher.VerifyHashedPassword(usuario, hash, solicitud.Contrasenia) == PasswordVerificationResult.Failed))
            return Unauthorized("Correo o contraseña incorrectos.");
        if (usuario is null)
            return NotFound("No encontramos un usuario activo con ese correo.");

        return Ok(await perfiles.ObtenerAsync(usuario, sesiones.Emitir(usuario.id, requiereContrasenia), null, ct, requiereContrasenia));
    }

    [HttpGet("EspaciosRevision", Name = "ListarEspaciosRevision")]
    public async Task<IActionResult> ListarEspaciosRevision(CancellationToken ct)
    {
        if (!sesiones.Disponible(HttpContext))
            return NotFound();
        if (await sesiones.LeerAsync(HttpContext, ct) is not { } usuarioId)
            return Unauthorized();
        if (!await perfiles.EsRevisor(usuarioId, ct))
            return StatusCode(StatusCodes.Status403Forbidden);

        var ahora = DateTimeOffset.UtcNow;
        var espacios = await (from t in context.Tenant.AsNoTracking()
                              where t.estado == "ACTIVO" && t.Propietario.estado == "ACTIVO" &&
                                  t.propietario_id != usuarioId &&
                                  context.Suscripcion.Any(s => s.tenant_id == t.id && s.usuario_id == t.propietario_id &&
                                      s.estado == "ACTIVA" && s.desde <= ahora && (s.hasta == null || s.hasta > ahora))
                              orderby t.nombre_comercial ?? t.Propietario.nombres, t.id
                              select new { TenantId = t.id, Nombre = t.nombre_comercial ?? t.Propietario.nombres })
                              .ToListAsync(ct);
        return Ok(espacios);
    }

    [HttpGet("MuroRevision")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> MuroRevision(CancellationToken ct)
    {
        if (await sesiones.LeerAsync(HttpContext, ct, exigirIdentidad: true) is not { } usuarioId)
            return Unauthorized();
        if (!await perfiles.EsRevisor(usuarioId, ct))
            return StatusCode(StatusCodes.Status403Forbidden);

        var personas = await (from u in context.Usuario.AsNoTracking()
                              where u.estado == "ACTIVO" && u.id != usuarioId
                              join t in context.Tenant.AsNoTracking().Where(t => t.estado == "ACTIVO")
                                  on u.id equals t.propietario_id into espacios
                              from t in espacios.DefaultIfEmpty()
                              orderby u.nombres, u.apellidos, u.id
                              select new PersonaRevision(u.id, u.nombres, u.apellidos,
                                  (Guid?)t!.id, t!.nombre_comercial)).ToListAsync(ct);
        var filas = new FilaRevision[personas.Count];
        var url = (config["Aprovisionamiento:InmueblesOperacion"] ?? "https://localhost:7311").TrimEnd('/');
        var autorizacion = Request.Headers.Authorization.ToString();
        using var cliente = clientes.CreateClient("AdministracionInterna");
        await Parallel.ForEachAsync(Enumerable.Range(0, personas.Count),
            new ParallelOptions { MaxDegreeOfParallelism = 6, CancellationToken = ct }, async (index, token) =>
        {
            var persona = personas[index];
            if (persona.TenantId is not { } tenantId)
            {
                filas[index] = new(persona, [], false);
                return;
            }
            try
            {
                using var solicitud = new HttpRequestMessage(HttpMethod.Get, url + "/api/Inmuebles/BandejaRevision");
                solicitud.Headers.TryAddWithoutValidation("Authorization", autorizacion);
                solicitud.Headers.Add("X-Tenant", tenantId.ToString("D"));
                using var respuesta = await cliente.SendAsync(solicitud, token);
                respuesta.EnsureSuccessStatusCode();
                var inmuebles = await respuesta.Content.ReadFromJsonAsync<InmuebleRevision[]>(token);
                filas[index] = new(persona, inmuebles ?? [], inmuebles is null);
            }
            catch (Exception ex) when (!token.IsCancellationRequested &&
                ex is HttpRequestException or JsonException or OperationCanceledException)
            {
                filas[index] = new(persona, [], true);
            }
        });
        return Ok(filas);
    }

    private sealed record PersonaRevision(Guid Id, string Nombres, string? Apellidos, Guid? TenantId, string? Espacio);
    private sealed record InmuebleRevision(Guid Id, string Nombre, string EstadoRevision, DateTimeOffset? SolicitadoEn);
    private sealed record FilaRevision(PersonaRevision Persona, InmuebleRevision[] Inmuebles, bool NoDisponible);

    public sealed record CorreoSesion([Required, EmailAddress, StringLength(254)] string Correo,
        [StringLength(128)] string? Contrasenia = null);
}
