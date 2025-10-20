namespace SmartSupport.API.Services.Pdf.Interfaces;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
}


