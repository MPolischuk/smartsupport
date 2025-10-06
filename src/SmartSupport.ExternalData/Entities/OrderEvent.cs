namespace SmartSupport.ExternalData.Entities;

public class OrderEvent
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public DateTime When { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty; // Labelled, Approved, PlannedDispatch

    public Order? Order { get; set; }
}


