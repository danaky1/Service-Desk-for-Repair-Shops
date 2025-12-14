namespace ServiceDeskPro.Core.Entities;

public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Master
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class SparePart
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int MinStockLevel { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? SerialNumber { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string? DiagnosticNotes { get; set; }
    public string Status { get; set; } = "new";
    public int? MasterId { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public bool IsUrgent { get; set; }
    public int WarrantyPeriod { get; set; } = 90;
    
    public virtual Client? Client { get; set; }
    public virtual Master? Master { get; set; }
}