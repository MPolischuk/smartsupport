namespace SmartSupport.ExternalData.Entities;

public class Order
{
    public string Id { get; set; } = string.Empty; // e.g., AT-1003
    public Guid CustomerId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // ReadyToShip, InTransit, Delivered

    public Customer? Customer { get; set; }
    public ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
}


