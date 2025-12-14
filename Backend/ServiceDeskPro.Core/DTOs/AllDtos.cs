namespace ServiceDeskPro.Core.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? MasterName { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public bool IsUrgent { get; set; }
}

public class CreateOrderDto
{
    public int ClientId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? SerialNumber { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public int? MasterId { get; set; }
    public bool IsUrgent { get; set; }
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? DiagnosticNotes { get; set; }
    public int? MasterId { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
}