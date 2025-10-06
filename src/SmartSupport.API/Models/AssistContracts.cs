using System.Text.Json.Serialization;

namespace SmartSupport.API.Models;

public enum AssistIntent
{
    Unknown,
    OrderStatus,
    ReturnPolicy,
    DamagedItem
}

public enum AssistStatus
{
    Unknown,
    InTransit,
    Delivered,
    Delayed,
    ReadyToShip
}

public sealed record AssistCitation
{
    public string Source { get; init; } = string.Empty; // pdf | sql | api
    public string? Title { get; init; }
    public string? Table { get; init; }
    public string? Service { get; init; }
    public string? Id { get; init; }
    public int? Page { get; init; }
}

public sealed record AssistResponse
{
    public string Answer { get; init; } = string.Empty;
    public AssistIntent Intent { get; init; } = AssistIntent.Unknown;
    public double Confidence { get; init; }
    public string? OrderId { get; init; }
    public AssistStatus? Status { get; init; }
    public DateTimeOffset? Eta { get; init; }
    public IReadOnlyList<string> Actions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AssistCitation> Citations { get; init; } = Array.Empty<AssistCitation>();
    public bool RawContextUsed { get; init; }
}

public sealed record AssistRequestMetadata
{
    public string Prompt { get; init; } = string.Empty;
    public bool UseSql { get; init; }
    public bool UseApi { get; init; }
}


