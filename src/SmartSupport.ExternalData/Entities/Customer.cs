namespace SmartSupport.ExternalData.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}


