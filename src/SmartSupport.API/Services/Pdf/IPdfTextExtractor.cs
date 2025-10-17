namespace SmartSupport.API.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
}


