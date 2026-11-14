using Expedientes.Dominio;
using Expedientes.Dominio.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class EmpleadoArchivosController : ControllerBase
{
    private readonly ExpedientesContext context;
    private readonly IHttpClientFactory httpClientFactory;
    public EmpleadoArchivosController(ExpedientesContext context, IHttpClientFactory httpClientFactory)
    {
        this.context = context;
        this.httpClientFactory = httpClientFactory;
    }

    [HttpPost("crear")]
    public async Task<IActionResult> PostEmpleado(string CI, string CodigoArchivo)
    {

        var empleado = await (from e in context.Empleado
                              where e.CI == CI && e.Estado != "Inactivo"
                              select e).FirstOrDefaultAsync();

        if (empleado == null)
            return NotFound("El empleado no existe o esta inactivo.");

        var relacion = await (from ea in context.EmpleadoArchivo
                              where ea.EmpleadoId == empleado.EmpleadoId
                                  && ea.CodigoArchivo == CodigoArchivo
                              select ea).FirstOrDefaultAsync();

        if (relacion != null)
            return BadRequest("El archivo ya esta asociado al empleado.");

        var archivos = await httpClientFactory.CreateClient("Archivos").GetFromJsonAsync<List<DTOArchivo>>("api/Archivos/Listar");

        var archivo = (from a in archivos
                       where a.Codigo == CodigoArchivo
                       select a).FirstOrDefault();

        if (archivo == null)
            return NotFound("El archivo no existe o esta inactivo.");


        var empleadoArchivo = new EmpleadoArchivo
        {
            EmpleadoId = empleado.EmpleadoId,
            Empleado = empleado,
            CodigoArchivo = archivo.Codigo!,
            Estado = "Activo"
        };

        await context.EmpleadoArchivo.AddAsync(empleadoArchivo);
        await context.SaveChangesAsync();

        return StatusCode(201, "Archivo asociado al empleado correctamente.");
    }

    [HttpGet("Query")]
    public async Task<IActionResult> Query()
    {
        var client = httpClientFactory.CreateClient("Archivos");

        var archivos = await client.GetFromJsonAsync<List<DTOArchivo>>("api/Archivos/Listar");
        var empleados = await (from e in context.Empleado
                               where e.Estado != "Inactivo"
                               select e).ToListAsync();

        var relaciones = await (from ea in context.EmpleadoArchivo
                                where ea.Estado != "Inactivo"
                                select ea).ToListAsync();

        var resultado = (from e in empleados
                         join ea in relaciones on e.EmpleadoId equals ea.EmpleadoId 
                         join a in archivos on ea.CodigoArchivo equals a.Codigo
                         select new
                         {
                             e.EmpleadoId,
                             Empleado = e.Nombre,
                             e.CI,
                             CodigoArchivo = a.Codigo,
                             Archivo = a.Nombre,
                             a.Tipo
                         }).ToList();

        return Ok(resultado);
    }

}
