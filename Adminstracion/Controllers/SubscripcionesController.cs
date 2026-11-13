using Adminstracion.Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class SubscripcionesController : ControllerBase
{
    private readonly AdminstracionContext context;

    public SubscripcionesController(AdminstracionContext context)
    {
        this.context = context;
    }

    [HttpGet("Listar")]
    public async Task<IActionResult> GetSubscripcion()
    {
        var subscripciones = await (from s in context.Subscripcion
                                    where s.Estado != "Inactivo" && s.Usuario.Estado != "Inactivo"
                                    select new { s.SubscripcionId, s.Nombre, s.Descripcion, s.Precio,
                                        s.CodigoPlan, UsuarioId = s.Usuario.UsuarioId, s.Estado }).ToListAsync();
        return Ok(subscripciones);
    }

    [HttpGet("{subscripcionid}")]
    public async Task<IActionResult> GetSubscripcion(int subscripcionid)
    {
        var subscripcion = await (from s in context.Subscripcion
                                  where s.SubscripcionId == subscripcionid
                                  select new { s.SubscripcionId, s.Nombre, s.Descripcion, s.Precio,
                                      s.CodigoPlan, UsuarioId = s.Usuario.UsuarioId, s.Estado,
                                      EstadoUsuario = s.Usuario.Estado }).FirstOrDefaultAsync();
        if (subscripcion == null)
        {
            return NotFound("La subscripcion no fue encontrada.");
        }
        if (subscripcion.Estado == "Inactivo" || subscripcion.EstadoUsuario == "Inactivo")
        {
            return BadRequest("La subscripcion o su usuario se encuentra inactivo.");
        }
        return Ok(subscripcion);
    }

    [HttpPost("crear")]
    public async Task<IActionResult> PostSubscripcion(string Nombre, string Descripcion,
        int Precio, string CodigoPlan, int UsuarioId)
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Descripcion)
            || string.IsNullOrWhiteSpace(CodigoPlan) || Precio < 0 || UsuarioId <= 0)
        {
            return BadRequest("Completa los datos, utiliza un precio no negativo y un usuario valido.");
        }

        var usuario = await (from u in context.Usuario
                             where u.UsuarioId == UsuarioId
                             select u).FirstOrDefaultAsync();
        if (usuario == null)
        {
            return NotFound("El usuario no fue encontrado.");
        }
        if (usuario.Estado == "Inactivo")
        {
            return BadRequest("No puedes crear una subscripcion para un usuario inactivo.");
        }

        var codigo = CodigoPlan.Trim();
        var subscripcion = await (from s in context.Subscripcion
                                  where s.Usuario.UsuarioId == UsuarioId && s.CodigoPlan == codigo
                                  select s).FirstOrDefaultAsync();
        if (subscripcion != null)
        {
            return BadRequest("El usuario ya tiene una subscripcion con ese codigo de plan.");
        }

        subscripcion = new Subscripcion
        {
            Nombre = Nombre.Trim(),
            Descripcion = Descripcion.Trim(),
            Precio = Precio,
            CodigoPlan = codigo,
            Usuarioid = UsuarioId,
            Usuario = usuario,
            Estado = "Activo"
        };
        await context.Subscripcion.AddAsync(subscripcion);
        await context.SaveChangesAsync();
        return StatusCode(201, "Subscripcion creada correctamente.");
    }
}
