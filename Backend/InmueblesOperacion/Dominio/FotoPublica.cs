namespace InmueblesOperacion.Dominio;

public static class FotoPublica
{
    public const string Clase = "FOTO_PUBLICA";
    public const int MaximoBytes = 2 * 1024 * 1024;

    public static string? Tipo(ReadOnlySpan<byte> datos)
    {
        if (datos.StartsWith(new byte[] { 0xff, 0xd8, 0xff }))
            return "image/jpeg";
        if (datos.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
            return "image/png";
        if (datos.Length >= 12 && datos[..4].SequenceEqual("RIFF"u8) && datos[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }
}
