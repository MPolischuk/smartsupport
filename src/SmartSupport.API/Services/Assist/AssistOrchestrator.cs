using Microsoft.EntityFrameworkCore;
using SmartSupport.API.Models;
using SmartSupport.API.Services.Llm.Interfaces;
using SmartSupport.API.Services.Pdf.Interfaces;
using SmartSupport.ExternalData;

namespace SmartSupport.API.Services;

public sealed class AssistOrchestrator
{
    private readonly IPdfTextExtractor _pdf;
    private readonly ISqlRagProvider _sql;
    private readonly IApiRagProvider _api;
    private readonly ILlmClient _llm;
    private readonly ExternalDataDbContext _db;

    public AssistOrchestrator(IPdfTextExtractor pdf, ISqlRagProvider sql, IApiRagProvider api, ILlmClient llm, ExternalDataDbContext db)
    {
        _pdf = pdf;
        _sql = sql;
        _api = api;
        _llm = llm;
        _db = db;
    }

    public async Task<AssistResponse> HandleAsync(string prompt, string? orderNumber, bool useSql, bool useApi, Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        var pdfText = await _pdf.ExtractTextAsync(pdfStream, fileName, ct);

        var sqlFacts = Array.Empty<string>();
        var apiFacts = Array.Empty<string>();
        var citations = new List<AssistCitation> { new AssistCitation { Source = "pdf", Title = fileName, Page = 1 } };

        string? tracking = null;
        if (useSql)
        {
            var (facts, cites) = await _sql.GetSqlFactsAsync(orderNumber, ct);
            sqlFacts = facts.ToArray();
            citations.AddRange(cites);

            if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderNumber, ct);
                tracking = order?.TrackingNumber;
            }
        }

        if (useApi && !string.IsNullOrWhiteSpace(tracking))
        {
            var (facts, cites) = await _api.GetApiFactsAsync(tracking, ct);
            apiFacts = facts.ToArray();
            citations.AddRange(cites);
        }

        return await _llm.GetAnswerAsync(prompt, pdfText, sqlFacts, apiFacts, citations, ct);
    }
}


