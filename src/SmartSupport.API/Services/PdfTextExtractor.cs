using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
}

public sealed class NaivePdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        // Demo: intento de leer algunos bytes del PDF para simular contenido
        using var ms = new MemoryStream();
        pdfStream.CopyTo(ms);
        var bytes = ms.ToArray();
        string teaser = bytes.Length > 0 ? $"{bytes.Length} bytes" : "sin contenido";
        return Task.FromResult($"[PDF:{fileName}] (preview: {teaser}) Políticas: Envío estándar 3–5 días; Express 24–48h; Devoluciones 30 días sin costo; SLA soporte 24h.");
    }
}


