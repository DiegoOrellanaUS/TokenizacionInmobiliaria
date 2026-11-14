using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Archivos.Dominio;

[Route("api/[controller]")]
[ApiController]
public class ArchivosController : ControllerBase
{
    private readonly ArchivosContext context;
    public ArchivosController(ArchivosContext context)
    {
        this.context = context;
    }

    // GET: api/Archivo
    [HttpGet("Listar")]
    public async Task<IActionResult> GetArchivo()
    {
        var archivos = await (from a in context.Archivo
                              where a.Estado != "Inactivo"
                              select a).ToListAsync();
        return Ok(archivos);
    }

    // GET: api/Archivo/5
    [HttpGet("{codigo}")]
    public async Task<ActionResult<Archivo>> GetArchivo(string codigo)
    {
        var archivo = await (from a in context.Archivo
                             where a.Codigo == codigo
                             select a).FirstOrDefaultAsync();
        if (archivo == null)
        {
            return NotFound("El archivo no fue encontrado.");
        }
        if (archivo.Estado == "Inactivo")
        {
            return BadRequest("El archivo se encuentra inactivo.");
        }
        return Ok(archivo);
    }


    // POST: api/Archivo
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost("crear")]
    public async Task<IActionResult> PostArchivo(string Codigo, string Nombre,string Ruta, string Tipo, string Contenido)
    {
        var archivo = await (from a in context.Archivo
                             where a.Codigo == Codigo
                             select a).FirstOrDefaultAsync();

        if (archivo != null)
        {
            return BadRequest("un archivo con ese código ya existe.");
        }

        archivo = new Archivo
        {
            Codigo = Codigo,
            Nombre = Nombre,
            Tipo = Tipo,
            Ruta = Ruta,
            Contenido = Contenido,
            Estado = "Activo"
        };
        await context.Archivo.AddAsync(archivo);
        await context.SaveChangesAsync();

        return StatusCode(201, "Archivo creado correctamente.");
    }


}
