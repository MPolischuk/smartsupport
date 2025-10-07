using Microsoft.EntityFrameworkCore;
using SmartSupport.API.Models;
using SmartSupport.ExternalData;

namespace SmartSupport.API.Services;

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


