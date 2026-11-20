using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Tokenizacion.Servicios;

public sealed partial class StellarCli(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<StellarCli> logger)
{
    public const string Codigo = "INM001";
    private const string Red = "testnet";
    private const string Emisora = "emisora";
    private const string Distribuidora = "distribuidora";
    private const string Compradora = "compradora";
    private const long UnidadesPorToken = 10_000_000;
    private const long EmisionBase = 500 * UnidadesPorToken;
    private readonly SemaphoreSlim operaciones = new(1);

    public async Task<T> EjecutarEnOrdenAsync<T>(Func<Task<T>> operacion, CancellationToken cancellationToken)
    {
        await operaciones.WaitAsync(cancellationToken);
        try
        {
            return await operacion();
        }
        finally
        {
            operaciones.Release();
        }
    }

    public async Task<object> CrearCuentasAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var emisora = await PrepararCuentaAsync(tenantId, Emisora, cancellationToken);
        var distribuidora = await PrepararCuentaAsync(tenantId, Distribuidora, cancellationToken);
        var compradora = await PrepararCuentaAsync(tenantId, Compradora, cancellationToken);

        return new { Red, Emisora = emisora, Distribuidora = distribuidora, Compradora = compradora };
    }

    public async Task<object> CrearLineaConfianzaAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var (activo, destino) = await ObtenerActivoAsync(tenantId, cancellationToken);
        var linea = await ConsultarLineaAsync(tenantId, activo, destino, cancellationToken);
        if (linea is not null)
            return new { Estado = "YaExiste", Codigo, Distribuidora = destino };

        var resultado = await EnviarAsync(tenantId, Distribuidora, cancellationToken,
            "change-trust", "--line", activo, "--limit", EmisionBase.ToString(CultureInfo.InvariantCulture));

        return new { Estado = "Confirmada", Codigo, Distribuidora = destino, resultado.Hash, resultado.Ledger };
    }

    public async Task<object> EmitirAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var (activo, destino) = await ObtenerActivoAsync(tenantId, cancellationToken);
        var linea = await ConsultarLineaAsync(tenantId, activo, destino, cancellationToken);
        if (linea is null)
            throw new StellarRequisitoException("Crea primero la línea de confianza.");

        if (linea.Value.Saldo == EmisionBase)
            return new { Estado = "YaEmitido", Codigo, Unidades = 500, Distribuidora = destino };

        if (linea.Value.Saldo != 0 || linea.Value.Limite != EmisionBase)
            throw new StellarRequisitoException("La línea de la demo debe tener saldo cero y límite de 500.");

        var resultado = await EnviarAsync(tenantId, Emisora, cancellationToken,
            "payment", "--destination", destino, "--asset", activo,
            "--amount", EmisionBase.ToString(CultureInfo.InvariantCulture));

        return new { Estado = "Confirmada", Codigo, Unidades = 500, Distribuidora = destino, resultado.Hash, resultado.Ledger };
    }

    public async Task<object> ConsultarSaldoAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var (activo, destino) = await ObtenerActivoAsync(tenantId, cancellationToken);
        var linea = await ConsultarLineaAsync(tenantId, activo, destino, cancellationToken);
        if (linea is null)
            throw new StellarRequisitoException("Crea primero la línea de confianza.");

        return new { Red, Codigo, Distribuidora = destino, Saldo = linea.Value.Saldo / (decimal)UnidadesPorToken };
    }

    private async Task<string> PrepararCuentaAsync(Guid tenantId, string alias, CancellationToken cancellationToken)
    {
        if (!ExisteIdentidad(tenantId, alias))
            await EjecutarAsync(tenantId, cancellationToken, null, "keys", "generate", alias, "--network", Red);

        var direccion = await ObtenerDireccionAsync(tenantId, alias, cancellationToken);
        var cuenta = await EjecutarJsonAsync(tenantId, cancellationToken, null,
            "ledger", "entry", "fetch", "account", "--account", direccion, "--network", Red);

        if (cuenta.GetProperty("entries").GetArrayLength() == 0)
            await EjecutarAsync(tenantId, cancellationToken, null, "keys", "fund", alias, "--network", Red);

        return direccion;
    }

    private async Task<(string Activo, string Destino)> ObtenerActivoAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (!ExisteIdentidad(tenantId, Emisora) || !ExisteIdentidad(tenantId, Distribuidora))
            throw new StellarRequisitoException("Crea primero las cuentas Stellar del tenant.");

        var emisor = await ObtenerDireccionAsync(tenantId, Emisora, cancellationToken);
        var destino = await ObtenerDireccionAsync(tenantId, Distribuidora, cancellationToken);
        return ($"{Codigo}:{emisor}", destino);
    }

    private Task<string> ObtenerDireccionAsync(Guid tenantId, string alias, CancellationToken cancellationToken) =>
        EjecutarAsync(tenantId, cancellationToken, null, "keys", "public-key", alias);

    private async Task<(long Saldo, long Limite)?> ConsultarLineaAsync(
        Guid tenantId, string activo, string destino, CancellationToken cancellationToken)
    {
        var resultado = await EjecutarJsonAsync(tenantId, cancellationToken, null,
            "ledger", "entry", "fetch", "trustline", "--account", destino, "--asset", activo, "--network", Red);
        var entradas = resultado.GetProperty("entries");
        if (entradas.GetArrayLength() == 0)
            return null;

        var linea = entradas[0].GetProperty("val").GetProperty("trustline");
        return (
            long.Parse(linea.GetProperty("balance").GetString()!, CultureInfo.InvariantCulture),
            long.Parse(linea.GetProperty("limit").GetString()!, CultureInfo.InvariantCulture));
    }

    private async Task<(string Hash, long Ledger)> EnviarAsync(
        Guid tenantId, string firmante, CancellationToken cancellationToken, params string[] operacion)
    {
        var xdr = await EjecutarAsync(tenantId, cancellationToken, null,
            ["tx", "new", .. operacion, "--source", firmante, "--network", Red, "--build-only"]);

        var firmado = await EjecutarAsync(tenantId, cancellationToken, xdr,
            "tx", "sign", "--sign-with-key", firmante, "--network", Red);

        var resultado = await EjecutarJsonAsync(tenantId, cancellationToken, firmado, "tx", "send", "--network", Red);
        if (resultado.GetProperty("status").GetString() != "SUCCESS")
            throw new StellarCliException("Stellar todavía no confirmó la operación. Consulta el saldo antes de repetirla.");

        return (resultado.GetProperty("tx_hash").GetString()!, resultado.GetProperty("ledger").GetInt64());
    }

    private bool ExisteIdentidad(Guid tenantId, string alias) =>
        File.Exists(Path.Combine(DirectorioTenant(tenantId), "identity", $"{alias}.toml"));

    private string DirectorioTenant(Guid tenantId)
    {
        var directorio = configuration["Stellar:DirectorioDatos"] ??
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tokenizacion", "Stellar");

        return Path.Combine(directorio, tenantId.ToString("N"));
    }

    private async Task<JsonElement> EjecutarJsonAsync(
        Guid tenantId, CancellationToken cancellationToken, string? entrada, params string[] argumentos)
    {
        var salida = await EjecutarAsync(tenantId, cancellationToken, entrada, argumentos);
        return JsonSerializer.Deserialize<JsonElement>(salida);
    }

    private async Task<string> EjecutarAsync(
        Guid tenantId, CancellationToken cancellationToken, string? entrada, params string[] argumentos)
    {
        var directorio = Directory.CreateDirectory(DirectorioTenant(tenantId)).FullName;
        var ejecutable = configuration["Stellar:Ejecutable"] ??
            Path.Combine(environment.ContentRootPath, "bin", "stellar", OperatingSystem.IsWindows() ? "stellar.exe" : "stellar");

        var inicio = new ProcessStartInfo(ejecutable)
        {
            WorkingDirectory = directorio,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var variable in inicio.Environment.Keys.Where(key => key.StartsWith("STELLAR_", StringComparison.OrdinalIgnoreCase)).ToArray())
            inicio.Environment.Remove(variable);

        inicio.ArgumentList.Add("--config-dir");
        inicio.ArgumentList.Add(directorio);
        inicio.ArgumentList.Add("--quiet");
        inicio.ArgumentList.Add("--no-cache");
        foreach (var argumento in argumentos)
            inicio.ArgumentList.Add(argumento);

        using var proceso = new Process { StartInfo = inicio };
        try
        {
            proceso.Start();
        }
        catch (Win32Exception ex)
        {
            logger.LogError("No se pudo iniciar Stellar CLI. Código del sistema {Codigo}.", ex.NativeErrorCode);
            throw new StellarCliException("No se pudo iniciar Stellar CLI. Verifica la instalación del ejecutable.");
        }

        using var tiempo = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        tiempo.CancelAfter(TimeSpan.FromSeconds(45));
        var salida = proceso.StandardOutput.ReadToEndAsync(tiempo.Token);
        var errores = proceso.StandardError.ReadToEndAsync(tiempo.Token);

        try
        {
            if (entrada is not null)
                await proceso.StandardInput.WriteLineAsync(entrada.AsMemory(), tiempo.Token);

            proceso.StandardInput.Close();
            await proceso.WaitForExitAsync(tiempo.Token);
            await Task.WhenAll(salida, errores);
        }
        catch (OperationCanceledException)
        {
            if (!proceso.HasExited)
            {
                try
                {
                    proceso.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) when (proceso.HasExited)
                {
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException("Stellar CLI tardó demasiado. Consulta el saldo si estabas emitiendo.");
        }

        if (proceso.ExitCode != 0)
        {
            logger.LogWarning("Stellar CLI falló en {Comando} con código {Codigo}.",
                string.Join(" ", argumentos.Take(2)), proceso.ExitCode);
            throw new StellarCliException("Falló el comando de Stellar. Comprueba la conexión y el estado de Testnet.");
        }

        return (await salida).Trim();
    }
}

public sealed class StellarCliException(string message) : Exception(message);

public sealed class StellarRequisitoException(string message) : Exception(message);
