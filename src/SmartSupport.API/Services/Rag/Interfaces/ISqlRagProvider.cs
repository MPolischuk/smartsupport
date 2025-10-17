using SmartSupport.API.Models;

namespace SmartSupport.API.Services;

public interface ISqlRagProvider
{
    Task<(IReadOnlyList<string> facts, IReadOnlyList<AssistCitation> citations)> GetSqlFactsAsync(string? orderNumber, CancellationToken ct = default);
}


