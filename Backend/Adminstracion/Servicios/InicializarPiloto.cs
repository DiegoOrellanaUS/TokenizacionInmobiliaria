using System.ComponentModel.DataAnnotations;
using Adminstracion.Data;
using Adminstracion.Dominio;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Adminstracion.Servicios;

public static class InicializarPiloto
{
    private static readonly Guid RevisorId = new("2036ed4a-8139-40ce-8a0a-f252efa50fa4");
    private const string RolRevisor = "REVISOR_INMUEBLE";
    private const string PlanPiloto = "UMBRAL-PILOTO";

    public static async Task PrepararAsync(WebApplication app)
    {
        var config = app.Configuration;


        var correo = config["Piloto:RevisorEmail"]?.Trim().ToLowerInvariant();
        var contrasenia = config["Piloto:RevisorContrasenia"];
        if (correo is null || correo.Length > 254 || !new EmailAddressAttribute().IsValid(correo) ||
            contrasenia is not { Length: >= 12 and <= 128 })
            throw new InvalidOperationException("Configura el correo y la contraseña del revisor piloto.");

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var ct = app.Lifetime.ApplicationStopping;
        await context.Database.MigrateAsync(ct);
        await using var tx = await context.Database.BeginTransactionAsync(ct);
        await AprovisionarTenant.Bloquear(context, "inicializar:piloto", ct);

        if (!await context.Plan.AnyAsync(p => p.codigo == PlanPiloto, ct))
            context.Plan.Add(new Plan
            {
                codigo = PlanPiloto, nombre = "Plan piloto Umbral", descripcion = "Demostración con pago simulado.",
                precio = 0m, moneda = "BOB", periodicidad = "MENSUAL",
                max_tokens_propios = 3, max_tokens_como_cliente = 10,
            });

        var rol = await context.Rol.SingleOrDefaultAsync(r => r.codigo == RolRevisor, ct);
        if (rol is null)
        {
            rol = new Rol { id = Guid.NewGuid(), codigo = RolRevisor, nombre_visible = "Revisor de inmuebles" };
            context.Rol.Add(rol);
        }
        if (await context.Usuario.AnyAsync(u => u.correo_electronico == correo && u.id != RevisorId, ct))
            throw new InvalidOperationException("El correo del revisor ya corresponde a otra cuenta.");

        var revisor = await context.Usuario.FindAsync([RevisorId], ct);
        if (revisor is null)
        {
            revisor = new Usuario { id = RevisorId, nombres = "Revisor", apellidos = "Umbral" };
            context.Usuario.Add(revisor);
        }
        revisor.correo_electronico = correo;
        if (revisor.contrasenia_hash is not { } hash ||
            hasher.VerifyHashedPassword(revisor, hash, contrasenia) == PasswordVerificationResult.Failed)
            revisor.contrasenia_hash = hasher.HashPassword(revisor, contrasenia);

        if (!await context.UsuarioRol.AnyAsync(r => r.usuario_id == RevisorId && r.rol_id == rol.id &&
            r.alcance == "GLOBAL" && r.fecha_eliminacion == null, ct))
            context.UsuarioRol.Add(new UsuarioRol { usuario_id = RevisorId, rol_id = rol.id, alcance = "GLOBAL" });

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        config["Inmuebles:Revisores:0"] = RevisorId.ToString("D");
    }
}
