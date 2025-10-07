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
        // Demo: extractor sencillo; en producción usar una librería de PDF.
        return Task.FromResult($"[PDF:{fileName}] Contenido simulado para demo.");
    }
}


