using iText.IO.Source;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;
using SmartSupport.API.Models;
using ITextPdfTextExtractor = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor;

namespace SmartSupport.API.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
}

public sealed class NaivePdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(ct);
        }

        ArgumentNullException.ThrowIfNull(pdfStream);

        if (pdfStream.CanSeek)
        {
            pdfStream.Position = 0;
        }

        var stringBuilder = new StringBuilder(capacity: 8 * 1024);

        var readerProperties = new ReaderProperties();
        using (var pdfReader = new PdfReader(pdfStream, readerProperties))
        using (var pdfDocument = new PdfDocument(pdfReader))
        {
            int totalPages = pdfDocument.GetNumberOfPages();

            for (int pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                ct.ThrowIfCancellationRequested();

                var page = pdfDocument.GetPage(pageNumber);
                var strategy = new SimpleTextExtractionStrategy();
                string pageText = ITextPdfTextExtractor.GetTextFromPage(page, strategy);

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    stringBuilder.AppendLine(pageText);
                    stringBuilder.AppendLine();
                }
            }
        }

        return Task.FromResult(stringBuilder.ToString());
    }
}


