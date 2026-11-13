using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expedientes.Dominio;

[Route("api/[controller]")]
[ApiController]
public class EmpleadosController(ExpedientesContext context) : ControllerBase
{
    [HttpGet("Listar")]
    public async Task<IActionResult> GetEmpleados()
    {
        var empleados = await (from e in context.Empleado
                               where e.Estado != "Inactivo"
                               select new { e.CI, e.Nombre })
                               .ToListAsync();
        return Ok(empleados);
    }

    [HttpPost("crear")]
    public async Task<IActionResult> PostEmpleado(string CI, string Nombre)
    {
        var empleado = await (from e in context.Empleado
                              where e.CI == CI
                              select e).FirstOrDefaultAsync();

        if (empleado != null)
            return BadRequest("Ya existe un empleado con ese CI.");

        empleado = new Empleado
        {
            CI = CI,
            Nombre = Nombre.Trim(),
            Estado = "Activo"
        };

        await context.Empleado.AddAsync(empleado);
        await context.SaveChangesAsync();
        return StatusCode(201, "Empleado creado correctamente.");
    }
}