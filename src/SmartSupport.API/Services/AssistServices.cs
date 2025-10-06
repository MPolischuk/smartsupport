using Microsoft.EntityFrameworkCore;
using SmartSupport.API.Models;
using SmartSupport.ExternalData;

namespace SmartSupport.API.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
}

public sealed class NaivePdfTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        // Demo: no parse real, retornamos marcador de posición
        return Task.FromResult($"[PDF:{fileName}] Contenido simulado para demo.");
    }
}

public interface ISqlRagProvider
{
    Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetSqlFactsAsync(string? orderNumber, CancellationToken ct = default);
}

public sealed class SqlRagProvider : ISqlRagProvider
{
    private readonly ExternalDataDbContext _db;
    public SqlRagProvider(ExternalDataDbContext db) => _db = db;

    public async Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetSqlFactsAsync(string? orderNumber, CancellationToken ct = default)
    {
        var facts = new List<string>();
        var cites = new List<AssistCitation>();
        if (string.IsNullOrWhiteSpace(orderNumber)) return (facts, cites);

        var order = await _db.Orders.Include(o => o.Customer).Include(o => o.Events)
            .FirstOrDefaultAsync(o => o.Id == orderNumber, ct);
        if (order is null) return (facts, cites);

        facts.Add($"Order {order.Id} carrier {order.Carrier} status {order.Status}");
        cites.Add(new AssistCitation { Source = "sql", Title = "Orders", Table = "Orders", Id = order.Id });

        foreach (var e in order.Events.OrderByDescending(x => x.When).Take(3))
        {
            facts.Add($"Event {e.Kind} at {e.When:u}: {e.Description}");
            cites.Add(new AssistCitation { Source = "sql", Title = e.Description, Table = "OrderEvents", Id = e.Id.ToString() });
        }

        var ticket = await _db.Tickets.Where(t => t.OrderId == order.Id)
            .OrderByDescending(t => t.OpenedAt).FirstOrDefaultAsync(ct);
        if (ticket != null)
        {
            facts.Add($"Ticket {ticket.Code} status {ticket.Status} topic {ticket.Topic}");
            cites.Add(new AssistCitation { Source = "sql", Title = ticket.Code, Table = "Tickets", Id = ticket.Id.ToString() });
        }

        return (facts, cites);
    }
}

public interface IApiRagProvider
{
    Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetApiFactsAsync(string? trackingNumber, CancellationToken ct = default);
}

public sealed class ApiRagProvider : IApiRagProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ApiRagProvider(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetApiFactsAsync(string? trackingNumber, CancellationToken ct = default)
    {
        var facts = new List<string>();
        var cites = new List<AssistCitation>();
        if (string.IsNullOrWhiteSpace(trackingNumber)) return (facts, cites);

        var client = _httpClientFactory.CreateClient("ExternalService");
        var resp = await client.GetAsync($"/tracking/{trackingNumber}", ct);
        if (!resp.IsSuccessStatusCode) return (facts, cites);
        var json = await resp.Content.ReadAsStringAsync(ct);
        facts.Add($"Tracking raw: {json}");
        cites.Add(new AssistCitation { Source = "api", Service = "ExternalService", Id = trackingNumber });
        return (facts, cites);
    }
}

public interface IOpenAiClient
{
    Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default);
}

public sealed class MockOpenAiClient : IOpenAiClient
{
    public Task<AssistResponse> GetAnswerAsync(string prompt, string pdfText, IReadOnlyList<string> sqlFacts, IReadOnlyList<string> apiFacts, IReadOnlyList<AssistCitation> citations, CancellationToken ct = default)
    {
        var answer = $"[MOCK] {prompt} -> PDF:{(pdfText.Length > 48 ? pdfText[..48] + "..." : pdfText)} | SQL:{sqlFacts.Count} | API:{apiFacts.Count}";
        return Task.FromResult(new AssistResponse
        {
            Answer = answer,
            Confidence = 0.8,
            Citations = citations,
            Intent = AssistIntent.OrderStatus,
            Status = AssistStatus.InTransit,
            Eta = DateTimeOffset.UtcNow.AddDays(1),
            Actions = new[] { "track:/tracking" },
            RawContextUsed = true
        });
    }
}

public sealed class AssistOrchestrator
{
    private readonly IPdfTextExtractor _pdf;
    private readonly ISqlRagProvider _sql;
    private readonly IApiRagProvider _api;
    private readonly IOpenAiClient _ai;
    private readonly ExternalDataDbContext _db;

    public AssistOrchestrator(IPdfTextExtractor pdf, ISqlRagProvider sql, IApiRagProvider api, IOpenAiClient ai, ExternalDataDbContext db)
    {
        _pdf = pdf;
        _sql = sql;
        _api = api;
        _ai = ai;
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

        return await _ai.GetAnswerAsync(prompt, pdfText, sqlFacts, apiFacts, citations, ct);
    }
}


