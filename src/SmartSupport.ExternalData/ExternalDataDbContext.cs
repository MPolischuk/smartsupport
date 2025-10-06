using Microsoft.EntityFrameworkCore;
using SmartSupport.ExternalData.Entities;

namespace SmartSupport.ExternalData;

public class ExternalDataDbContext : DbContext
{
    public ExternalDataDbContext(DbContextOptions<ExternalDataDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(256);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(32);
            e.Property(x => x.TrackingNumber).HasMaxLength(64);
            e.Property(x => x.Carrier).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasOne(x => x.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<OrderEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderId).HasMaxLength(32);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.Kind).HasMaxLength(64);
            e.HasOne(x => x.Order)
                .WithMany(o => o.Events)
                .HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderId).HasMaxLength(32);
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Topic).HasMaxLength(64);
            e.HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId);
        });

        // Seed determinista para la demo
        var customerId = Guid.NewGuid();
        var orderId = "AT-1003";
        var replacementId = "AT-1003-R";
        var ticketId = Guid.NewGuid();
        var event1Id = Guid.NewGuid();
        var event2Id = Guid.NewGuid();
        var event3Id = Guid.NewGuid();

        modelBuilder.Entity<Customer>().HasData(new Customer
        {
            Id = customerId,
            Email = "juan@example.com",
            FullName = "Juan Pérez"
        });

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = orderId,
                CustomerId = customerId,
                TrackingNumber = "1Z999SMART",
                Carrier = "SmartCarrier",
                Status = "ReadyToShip"
            },
            new Order
            {
                Id = replacementId,
                CustomerId = customerId,
                TrackingNumber = "1Z999SMART",
                Carrier = "SmartCarrier",
                Status = "ReadyToShip"
            }
        );

        modelBuilder.Entity<OrderEvent>().HasData(
            new OrderEvent
            {
                Id = event1Id,
                OrderId = replacementId,
                When = DateTime.SpecifyKind(new DateTime(2025, 10, 5, 10, 0, 0), DateTimeKind.Utc),
                Description = "Inspección aprobada",
                Kind = "Approved"
            },
            new OrderEvent
            {
                Id = event2Id,
                OrderId = replacementId,
                When = DateTime.SpecifyKind(new DateTime(2025, 10, 6, 9, 0, 0), DateTimeKind.Utc),
                Description = "Etiquetado",
                Kind = "Labelled"
            },
            new OrderEvent
            {
                Id = event3Id,
                OrderId = replacementId,
                When = DateTime.SpecifyKind(new DateTime(2025, 10, 7, 8, 30, 0), DateTimeKind.Utc),
                Description = "Planificado despacho",
                Kind = "PlannedDispatch"
            }
        );

        modelBuilder.Entity<Ticket>().HasData(new Ticket
        {
            Id = ticketId,
            OrderId = orderId,
            Code = "T-501",
            Status = "InProgress",
            OpenedAt = DateTime.SpecifyKind(new DateTime(2025, 10, 4, 12, 0, 0), DateTimeKind.Utc),
            Topic = "DamagedItem"
        });
    }
}


