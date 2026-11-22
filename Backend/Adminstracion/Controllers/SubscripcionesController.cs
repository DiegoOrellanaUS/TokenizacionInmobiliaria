using System.Text.Json;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Adminstracion.DTOs;
using Adminstracion.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class SubscripcionesController(AdminContext context, AprovisionarTenant aprovisionador,
    IWebHostEnvironment environment, SesionDemo sesiones) : ControllerBase
{
    private const string Proveedor = "PILOTO";

    private IQueryable<Suscripcion> Suscripciones() =>
        context.Suscripcion.AsNoTracking().Include(s => s.Plan);

    private static object Datos(Suscripcion s) => new
    {
        SubscripcionId = s.id, UsuarioId = s.usuario_id, TenantId = s.tenant_id, PlanId = s.plan_id,
        Nombre = s.Plan.nombre, Descripcion = s.Plan.descripcion, Precio = s.importe_pagado,
        CodigoPlan = s.Plan.codigo, Estado = s.estado, Desde = s.desde, Hasta = s.hasta
    };

    [HttpGet("Listar", Name = "ListarSubscripciones")]
    [HttpGet("ListarReal", Name = "ListarRealSubscripciones")]
    public async Task<IActionResult> GetSubscripcion(CancellationToken ct)
    {
        var filas = await (from s in Suscripciones()
                           where s.estado.ToUpper() != "BORRADO" && s.estado.ToUpper() != "ELIMINADO"
                           select s).ToListAsync(ct);
        return Ok(filas.Select(Datos));
    }

    [HttpGet("Buscar/{subscripcionid:guid}", Name = "BuscarSubscripcion")]
    public async Task<IActionResult> GetSubscripcion(Guid subscripcionid, CancellationToken ct)
    {
        var suscripcion = await (
            from s in Suscripciones()
            where s.id == subscripcionid && s.estado.ToUpper() != "BORRADO" && s.estado.ToUpper() != "ELIMINADO"
            select s
        ).FirstOrDefaultAsync(ct);
        if (suscripcion == null)
            return NotFound("La suscripción no fue encontrada.");
        return Ok(Datos(suscripcion));
    }

    [HttpPost("crear", Name = "CrearSubscripcion")]
    public async Task<IActionResult> PostSubscripcion(RegistroSubscripcion registro, CancellationToken ct)
    {
        var piloto = environment.IsEnvironment("Piloto");
        if (!piloto && !environment.IsDevelopment())
            return NotFound();

        if ((piloto || sesiones.SoloOAuth || Request.Headers.ContainsKey("Authorization")) &&
            (await sesiones.LeerAsync(HttpContext, ct) != registro.UsuarioId))
            return StatusCode(StatusCodes.Status403Forbidden);

        var codigo = registro.CodigoPlan.Trim();
        var referencia = registro.ReferenciaPago.Trim();
        if (codigo.Length == 0 || referencia.Length == 0 || registro.UsuarioId == Guid.Empty)
            return BadRequest("Indica el plan, el usuario y la referencia de pago.");

        Guid tenantId;
        Guid suscripcionId;
        var creada = false;
        await using (var transaccion = await context.Database.BeginTransactionAsync(ct))
        {
            await AprovisionarTenant.Bloquear(context, $"usuario:{registro.UsuarioId:N}", ct);
            await AprovisionarTenant.Bloquear(context, $"pago:{referencia}", ct);

            var usuario = await context.Usuario.FindAsync([registro.UsuarioId], ct);
            if (usuario == null)
                return NotFound("El usuario no fue encontrado.");
            if (usuario.estado != "ACTIVO")
                return Conflict("El usuario se encuentra inactivo.");

            var tenant = await context.Tenant.AsNoTracking().SingleOrDefaultAsync(t => t.propietario_id == usuario.id, ct);
            if (tenant != null && tenant.estado is not ("ACTIVO" or "APROVISIONANDO"))
                return Conflict("El tenant se encuentra deshabilitado.");

            var pago = await Suscripciones().SingleOrDefaultAsync(s => s.proveedor_pago == Proveedor
                && s.referencia_pago == referencia, ct);
            if (pago != null)
            {
                if (tenant == null || pago.usuario_id != usuario.id || pago.tenant_id != tenant.id || pago.Plan.codigo != codigo ||
                    pago.estado is not ("ACTIVA" or "APROVISIONANDO"))
                    return Conflict("La referencia ya está asociada a otra operación o la suscripción no está disponible.");

                tenantId = pago.tenant_id;
                suscripcionId = pago.id;
            }
            else
            {
                var ahora = DateTimeOffset.UtcNow;
                var plan = await context.Plan.AsNoTracking().Include(p => p.PlanDestino)
                    .SingleOrDefaultAsync(p => p.codigo == codigo, ct);
                if (plan == null || plan.estado != "ACTIVO" || plan.oferta_desde > ahora || plan.oferta_hasta <= ahora)
                    return BadRequest("El plan seleccionado no está disponible.");

                var destino = plan.PlanDestino ?? plan;
                if (destino.estado != "ACTIVO" || destino.plan_destino_id != null)
                    return Conflict("El plan de destino no está disponible.");

                var tenantActualId = tenant?.id;
                var vigente = await (from s in context.Suscripcion.AsNoTracking()
                                     where s.usuario_id == usuario.id && s.tenant_id == tenantActualId && s.estado == "ACTIVA"
                                         && (s.hasta == null || s.hasta > ahora)
                                     orderby s.desde descending
                                     select s).FirstOrDefaultAsync(ct);
                if (plan.plan_origen_id.HasValue &&
                    (vigente == null || PlanEfectivo(vigente) != plan.plan_origen_id.Value))
                    return Conflict("El plan vigente del usuario no cumple las condiciones de la oferta.");
                var hayPendiente = await (
                    from s in context.Suscripcion
                    where s.usuario_id == usuario.id && s.tenant_id == tenantActualId && s.estado == "APROVISIONANDO"
                    select s.id
                ).AnyAsync(ct);
                if (hayPendiente)
                    return Conflict("Hay una suscripción pendiente. Reintenta con la referencia original.");

                var hasta = plan.periodicidad switch
                {
                    "MENSUAL" => ahora.AddMonths(1),
                    "ANUAL" => ahora.AddYears(1),
                    _ => (DateTimeOffset?)null
                };
                if (hasta == null)
                    return Conflict("El plan debe tener periodicidad MENSUAL o ANUAL.");

                if (tenant == null)
                {
                    await AprovisionarTenant.Bloquear(context, "rol:TITULAR_TENANT", ct);
                    var rol = await context.Rol.SingleOrDefaultAsync(r => r.codigo == "TITULAR_TENANT", ct);
                    if (rol == null)
                    {
                        rol = new Rol { id = Guid.NewGuid(), codigo = "TITULAR_TENANT", nombre_visible = "Titular del tenant" };
                        context.Rol.Add(rol);
                    }
                    if (rol.estado != "ACTIVO")
                        return Conflict("El rol del titular no está disponible.");

                    tenant = new Tenant { id = Guid.NewGuid(), propietario_id = usuario.id, nombre_comercial = usuario.nombres };
                    context.Tenant.Add(tenant);
                    context.UsuarioRol.Add(new UsuarioRol
                    {
                        usuario_id = usuario.id, rol_id = rol.id, tenant_id = tenant.id, alcance = "TENANT",
                        silos_por_servicio = AprovisionarTenant.SilosPendientes()
                    });
                }
                tenantId = tenant.id;
                suscripcionId = Guid.NewGuid();
                context.Suscripcion.Add(new Suscripcion
                {
                    id = suscripcionId, usuario_id = usuario.id, tenant_id = tenantId, plan_id = plan.id, estado = "APROVISIONANDO",
                    desde = ahora, hasta = hasta, proveedor_pago = Proveedor, referencia_pago = referencia,
                    importe_pagado = plan.precio, pago_confirmado_en = ahora,
                    condiciones_contratadas = JsonSerializer.Serialize(new
                    {
                        plan.revision, plan.precio, plan.moneda, plan.periodicidad, plan_efectivo_id = destino.id,
                        destino.max_tokens_propios, destino.max_tokens_como_cliente
                    })
                });
                await context.SaveChangesAsync(ct);
                creada = true;
            }
            await transaccion.CommitAsync(ct);
        }

        var estado = await context.Suscripcion.Where(s => s.id == suscripcionId).Select(s => s.estado).SingleAsync(ct);
        var silosListos = estado == "ACTIVA" &&
            (await aprovisionador.EstadosSilos(tenantId, ct)).Values.All(silo => silo == "LISTO");
        if (!silosListos && estado is "APROVISIONANDO" or "ACTIVA")
            silosListos = await aprovisionador.Ejecutar(tenantId, ct);

        if (estado == "APROVISIONANDO" && silosListos)
        {
            await using var transaccion = await context.Database.BeginTransactionAsync(ct);
            await AprovisionarTenant.Bloquear(context, $"usuario:{registro.UsuarioId:N}", ct);
            var habilitado = await context.Tenant.AnyAsync(t => t.id == tenantId
                && (t.estado == "APROVISIONANDO" || t.estado == "ACTIVO") && t.Propietario.estado == "ACTIVO", ct);
            if (habilitado)
            {
                var activada = await (
                    from s in context.Suscripcion
                    where s.id == suscripcionId && s.tenant_id == tenantId && s.estado == "APROVISIONANDO"
                    select s
                ).ExecuteUpdateAsync(s => s.SetProperty(x => x.estado, "ACTIVA"), ct);
                if (activada > 0)
                {
                    await (
                        from s in context.Suscripcion
                        where s.usuario_id == registro.UsuarioId && s.tenant_id == tenantId
                            && s.id != suscripcionId && s.estado == "ACTIVA"
                        select s
                    ).ExecuteUpdateAsync(s => s.SetProperty(x => x.estado, "INACTIVO"), ct);
                    await context.Tenant.Where(t => t.id == tenantId && t.estado == "APROVISIONANDO")
                        .ExecuteUpdateAsync(t => t.SetProperty(x => x.estado, "ACTIVO"), ct);
                }
            }
            await transaccion.CommitAsync(ct);
            estado = await context.Suscripcion.Where(s => s.id == suscripcionId).Select(s => s.estado).SingleAsync(ct);
        }

        var servicios = (await aprovisionador.EstadosSilos(tenantId, ct))
            .OrderBy(s => s.Key).Select(s => new { Servicio = s.Key, Estado = s.Value });
        var resultado = new { SubscripcionId = suscripcionId, ClienteId = tenantId, TenantId = tenantId, Estado = estado, Servicios = servicios };
        if (estado == "APROVISIONANDO" || estado == "ACTIVA" && !silosListos)
            return AcceptedAtAction(nameof(GetSubscripcion), new { subscripcionid = suscripcionId }, resultado);
        return creada
            ? CreatedAtAction(nameof(GetSubscripcion), new { subscripcionid = suscripcionId }, resultado)
            : Ok(resultado);
    }

    private static Guid PlanEfectivo(Suscripcion suscripcion)
    {
        using var condiciones = JsonDocument.Parse(suscripcion.condiciones_contratadas);
        return condiciones.RootElement.TryGetProperty("plan_efectivo_id", out var id) && id.TryGetGuid(out var plan)
            ? plan : suscripcion.plan_id;
    }
}
