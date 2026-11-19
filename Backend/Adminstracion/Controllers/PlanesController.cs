using Adminstracion.Data;
using Adminstracion.Dominio;
using Adminstracion.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Route("api/[controller]")]
[ApiController]
public class PlanesController(AdminContext context, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("Listar", Name = "ListarPlanes")]
    [HttpGet("ListarReal", Name = "ListarRealPlanes")]
    public async Task<IActionResult> GetPlanes(CancellationToken ct)
    {
        var planes = await (
            from p in context.Plan.AsNoTracking()
            where p.estado.ToUpper() != "BORRADO" && p.estado.ToUpper() != "ELIMINADO"
            orderby p.nombre, p.codigo
            select p
        ).ToListAsync(ct);

        return Ok(planes);
    }

    [HttpGet("Buscar/{id:guid}", Name = "BuscarPlan")]
    public async Task<IActionResult> GetPlan(Guid id, CancellationToken ct)
    {
        var plan = await (
            from p in context.Plan.AsNoTracking()
            where p.id == id && p.estado.ToUpper() != "BORRADO" && p.estado.ToUpper() != "ELIMINADO"
            select p
        ).FirstOrDefaultAsync(ct);

        if (plan == null)
            return NotFound("El plan no fue encontrado.");

        return Ok(plan);
    }

    [HttpPost("crear", Name = "CrearPlan")]
    public Task<IActionResult> PostPlan(CrearPlan registro, CancellationToken ct) =>
        CrearPlanAsync(registro, null, ct);

    [HttpPost("ofertas", Name = "CrearOfertaPlan")]
    public Task<IActionResult> PostOferta(CrearOfertaPlan registro, CancellationToken ct) =>
        CrearPlanAsync(registro, registro.Oferta, ct);

    private async Task<IActionResult> CrearPlanAsync(CrearPlan registro, DatosOfertaPlan? oferta, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var plan = new Plan
        {
            id = Guid.NewGuid(),
            codigo = registro.Codigo.Trim(),
            nombre = registro.Nombre,
            moneda = registro.Moneda,
            periodicidad = registro.Periodicidad
        };
        AplicarDatos(plan, registro, oferta);
        var error = await ValidarPlan(plan, ct);
        if (error != null)
            return BadRequest(error);

        context.Plan.Add(plan);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict("El código del plan ya está registrado.");
        }

        return CreatedAtAction(nameof(GetPlan), new { id = plan.id }, plan);
    }

    [HttpPut("Actualizar/{id:guid}", Name = "ActualizarPlan")]
    public Task<IActionResult> PutPlan(Guid id, EditarPlan registro, CancellationToken ct) =>
        EditarPlanAsync(id, registro, null, ct);

    [HttpPut("ActualizarOferta/{id:guid}", Name = "ActualizarOfertaPlan")]
    public Task<IActionResult> PutOferta(Guid id, EditarOfertaPlan registro, CancellationToken ct) =>
        EditarPlanAsync(id, registro, registro.Oferta, ct);

    private async Task<IActionResult> EditarPlanAsync(Guid id, EditarPlan registro, DatosOfertaPlan? oferta, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from p in context.Plan
            where p.id == id && p.estado.ToUpper() != "BORRADO" && p.estado.ToUpper() != "ELIMINADO"
            select p;
        var plan = await consulta.AsNoTracking().FirstOrDefaultAsync(ct);
        if (plan == null)
            return NotFound("El plan no fue encontrado.");
        if (oferta != null && plan.plan_destino_id == null)
            return BadRequest("El plan indicado no es una oferta.");

        AplicarDatos(plan, registro, oferta);
        if (registro.Estado != null)
            plan.estado = registro.Estado.Trim().ToUpperInvariant();
        var error = await ValidarPlan(plan, ct);
        if (error != null)
            return BadRequest(error);

        var actualizados = await (
            from p in consulta
            where p.revision == plan.revision
            select p
        ).ExecuteUpdateAsync(p => p
            .SetProperty(x => x.nombre, plan.nombre)
            .SetProperty(x => x.descripcion, plan.descripcion)
            .SetProperty(x => x.precio, plan.precio)
            .SetProperty(x => x.moneda, plan.moneda)
            .SetProperty(x => x.periodicidad, plan.periodicidad)
            .SetProperty(x => x.max_tokens_propios, plan.max_tokens_propios)
            .SetProperty(x => x.max_tokens_como_cliente, plan.max_tokens_como_cliente)
            .SetProperty(x => x.plan_origen_id, plan.plan_origen_id)
            .SetProperty(x => x.plan_destino_id, plan.plan_destino_id)
            .SetProperty(x => x.oferta_desde, plan.oferta_desde)
            .SetProperty(x => x.oferta_hasta, plan.oferta_hasta)
            .SetProperty(x => x.estado, plan.estado)
            .SetProperty(x => x.revision, x => x.revision + 1), ct);

        if (actualizados == 0)
            return Conflict("El plan cambió durante la edición. Consulta su estado e inténtalo de nuevo.");

        return NoContent();
    }

    [HttpDelete("Borrar/{id:guid}", Name = "BorrarPlan")]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var consulta =
            from p in context.Plan
            where p.id == id
            select p;
        var estado = await (
            from p in consulta
            select p.estado
        ).FirstOrDefaultAsync(ct);
        if (estado == null)
            return NotFound("El plan no fue encontrado.");

        await (
            from p in consulta
            where p.estado.ToUpper() != "BORRADO" && p.estado.ToUpper() != "ELIMINADO"
            select p
        ).ExecuteUpdateAsync(p => p.SetProperty(x => x.estado, "Borrado"), ct);

        return NoContent();
    }

    private static void AplicarDatos(Plan plan, DatosPlan registro, DatosOfertaPlan? oferta)
    {
        plan.nombre = registro.Nombre.Trim();
        plan.descripcion = registro.Descripcion?.Trim();
        plan.precio = registro.Precio;
        var moneda = registro.Moneda.Trim().ToUpperInvariant();
        plan.moneda = moneda == "BS" ? "BOB" : moneda;
        plan.periodicidad = registro.Periodicidad.Trim().ToUpperInvariant();
        plan.max_tokens_propios = registro.MaxTokensPropios;
        plan.max_tokens_como_cliente = registro.MaxTokensComoCliente;
        if (oferta != null)
        {
            plan.plan_origen_id = oferta.PlanOrigenId;
            plan.plan_destino_id = oferta.PlanDestinoId;
            plan.oferta_desde = oferta.OfertaDesde?.ToUniversalTime();
            plan.oferta_hasta = oferta.OfertaHasta?.ToUniversalTime();
        }
    }

    private async Task<string?> ValidarPlan(Plan plan, CancellationToken ct)
    {
        if (plan.codigo.Length == 0 || plan.nombre.Length == 0 || plan.moneda.Length == 0)
            return "Indica el código, el nombre y la moneda del plan.";
        if (plan.estado is not ("ACTIVO" or "INACTIVO"))
            return "El estado debe ser ACTIVO o INACTIVO. Usa DELETE para dar de baja.";
        if (plan.periodicidad is not ("MENSUAL" or "ANUAL"))
            return "La periodicidad debe ser MENSUAL o ANUAL.";
        if (plan.precio < 0 || plan.precio > 9999999999999999.99m || decimal.Round(plan.precio, 2) != plan.precio)
            return "El precio debe ser no negativo y admite hasta 16 enteros y dos decimales.";
        if (plan.oferta_hasta <= plan.oferta_desde)
            return "La fecha final de la oferta debe ser posterior a su inicio.";
        if (plan.plan_origen_id.HasValue && !plan.plan_destino_id.HasValue)
            return "Una oferta de cambio requiere un plan de destino.";
        if (plan.plan_origen_id == Guid.Empty || plan.plan_destino_id == Guid.Empty)
            return "Selecciona un ID válido para los planes de la oferta.";
        if (plan.plan_origen_id == plan.id || plan.plan_destino_id == plan.id ||
            (plan.plan_origen_id.HasValue && plan.plan_origen_id == plan.plan_destino_id))
            return "Los planes de origen y destino deben ser distintos entre sí y del plan de la oferta.";

        if (plan.plan_destino_id.HasValue)
        {
            var relacionados = await (
                from p in context.Plan.AsNoTracking()
                where p.estado.ToUpper() != "BORRADO" && p.estado.ToUpper() != "ELIMINADO" && p.plan_destino_id == null
                    && (p.id == plan.plan_origen_id || p.id == plan.plan_destino_id)
                select p.id
            ).ToListAsync(ct);
            if (relacionados.Count != (plan.plan_origen_id.HasValue ? 2 : 1))
                return "Selecciona planes base disponibles para el origen y el destino.";
        }

        return null;
    }
}
