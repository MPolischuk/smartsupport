namespace SmartSupport.ExternalData.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // e.g., T-501
    public string Status { get; set; } = string.Empty; // Open, InProgress, Closed
    public DateTime OpenedAt { get; set; }
    public string Topic { get; set; } = string.Empty; // DamagedItem

    public Order? Order { get; set; }
}


