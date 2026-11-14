using Adminstracion.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly AdminstracionContext context;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;

    public UsuariosController(AdminstracionContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        this.context = context;
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
    }

    [HttpGet("Listar")]
    public async Task<IActionResult> GetUsuario()
    {
        var usuarios = await (from u in context.Usuario
                              where u.Estado != "Inactivo"
                              select new {u.UsuarioId, u.TenantId, u.CI, u.Nombre, u.CorreoElectronico, u.Estado, u.EstadoBaseDatos}
                              ).ToListAsync();
        return Ok(usuarios);
    }

    [HttpGet("{usuarioid}")]
    public async Task<IActionResult> GetUsuario(int usuarioid)
    {
        var usuario = await (from u in context.Usuario
                             where u.UsuarioId == usuarioid
                             select new { u.UsuarioId, u.TenantId, u.CI, u.Nombre, u.CorreoElectronico, u.Estado, u.EstadoBaseDatos }
                              ).FirstOrDefaultAsync();
        if (usuario == null)
        {
            return NotFound("El usuario no fue encontrado.");
        }
        if (usuario.Estado == "Inactivo")
        {
            return BadRequest("El usuario se encuentra inactivo.");
        }
        return Ok(usuario);
    }

    [HttpPost("crear")]
    public async Task<IActionResult> PostUsuario(string CI, string Nombre,
        string CorreoElectronico, string Contrasenia)
    {
        var usuario = await (from u in context.Usuario
                             where u.CI == CI || u.CorreoElectronico.ToUpper() == CorreoElectronico.ToUpper()
                             select u).FirstOrDefaultAsync();
        if (usuario != null)
        {
            return BadRequest("Ya existe un usuario con ese CI o correo electronico.");
        }
        var tenantID = Guid.NewGuid();

        usuario = new Usuario
        {
            CI = CI,
            Nombre = Nombre,
            CorreoElectronico = CorreoElectronico,
            Contrasenia = Contrasenia,
            TenantId = tenantID,
            Estado = "Activo"
        };
        await context.Usuario.AddAsync(usuario);
        await context.SaveChangesAsync();
        try
        {
            await PrepararBasesTenant(tenantID, "https://localhost:7043", "https://localhost:7173");
        }
        catch (Exception ex)
        {
            return Accepted(new
            {
                mensaje = "Cuenta registrada. Preparacion pendiente de confirmacion.",
                usuario.UsuarioId,
                usuario.TenantId,
                usuario.EstadoBaseDatos
            });
        }

        usuario.EstadoBaseDatos = "Lista";

        await context.SaveChangesAsync();
        return StatusCode(201, new
        {
            mensaje = "Usuario y base de datos creados correctamente.",
            usuario.UsuarioId,
            tenantId = tenantID,
            usuario.EstadoBaseDatos
        });
    }

    public async Task PrepararBasesTenant(Guid tenantId, params string[] servicios) 
    {
        var client = httpClientFactory.CreateClient();

        foreach (var servicio in servicios)
        {
            using var response = await client.PostAsync($"{servicio}/interno/tenants/{tenantId}/preparar",null);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task EliminarBaseAsync(string servicio, Guid tenantId, CancellationToken ct)
    {
        var url = servicio switch
        {
            "Archivos" => "https://localhost:7043",
            "Expedientes" => "https://localhost:7173",
            _ => throw new ArgumentException("Microservicio no permitido.", nameof(servicio))
        };

        using var client = httpClientFactory.CreateClient("AdministracionInterna");
        using var response = await client.DeleteAsync($"{url}/interno/tenants/{tenantId}/base", ct);
        response.EnsureSuccessStatusCode();
    }

    [HttpDelete("{usuarioId:int}/bases/archivos")]
    public async Task<IActionResult> DarDeBajaArchivos(
    int usuarioId, CancellationToken ct)
    {
        var usuario = await (
            from u in context.Usuario
            where u.UsuarioId == usuarioId
            select u
        ).FirstOrDefaultAsync(ct);

        if (usuario == null)
            return NotFound("Usuario no encontrado.");

        if (usuario.Estado != "Inactivo")
            return Conflict("Primero debes deshabilitar el tenant.");

        try
        {
            await EliminarBaseAsync("Archivos", usuario.TenantId, ct);
            return NoContent();
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, "No se pudo confirmar el borrado en Archivos.");
        }
    }

}
