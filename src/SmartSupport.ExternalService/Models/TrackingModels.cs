namespace SmartSupport.ExternalService.Models;

public sealed record TrackingResponse
{
    public string Status { get; init; } = string.Empty;
    public DateTime? Eta { get; init; }
    public LastScan? LastScan { get; init; }
}

public sealed record LastScan
{
    public DateTime When { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

