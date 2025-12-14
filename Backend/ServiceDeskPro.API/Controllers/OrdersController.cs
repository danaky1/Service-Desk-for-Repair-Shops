using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Core.Entities;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
    {
        try
        {
            var orders = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Master)
                .Select(o => new OrderResponseDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    ClientName = o.Client != null ? o.Client.Name : "Неизвестный клиент",
                    ClientPhone = o.Client != null ? o.Client.Phone : "",
                    Device = o.DeviceName,
                    DeviceModel = o.DeviceModel,
                    SerialNumber = o.SerialNumber,
                    ProblemDescription = o.ProblemDescription,
                    DiagnosticNotes = o.DiagnosticNotes,
                    Status = o.Status,
                    MasterId = o.MasterId,
                    MasterName = o.Master != null ? o.Master.Name : null,
                    TotalAmount = o.TotalCost,
                    PartsCost = o.PartsCost,
                    LaborCost = o.LaborCost,
                    CreatedDate = o.CreatedAt,
                    AcceptedAt = o.AcceptedAt,
                    StartedAt = o.StartedAt,
                    CompletedAt = o.CompletedAt,
                    EstimatedCompletionDate = o.EstimatedCompletionDate,
                    IsUrgent = o.IsUrgent,
                    WarrantyPeriod = o.WarrantyPeriod
                })
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Master)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { error = "Заказ не найден" });

            var orderDto = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ClientName = order.Client != null ? order.Client.Name : "Неизвестный клиент",
                ClientPhone = order.Client != null ? order.Client.Phone : "",
                Device = order.DeviceName,
                DeviceModel = order.DeviceModel,
                SerialNumber = order.SerialNumber,
                ProblemDescription = order.ProblemDescription,
                DiagnosticNotes = order.DiagnosticNotes,
                Status = order.Status,
                MasterId = order.MasterId,
                MasterName = order.Master != null ? order.Master.Name : null,
                TotalAmount = order.TotalCost,
                PartsCost = order.PartsCost,
                LaborCost = order.LaborCost,
                CreatedDate = order.CreatedAt,
                AcceptedAt = order.AcceptedAt,
                StartedAt = order.StartedAt,
                CompletedAt = order.CompletedAt,
                EstimatedCompletionDate = order.EstimatedCompletionDate,
                IsUrgent = order.IsUrgent,
                WarrantyPeriod = order.WarrantyPeriod
            };

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderRequestDto createDto)
    {
        try
        {
            var client = await _context.Clients.FindAsync(createDto.ClientId);
            if (client == null)
                return BadRequest(new { error = "Клиент не найден" });

            Master? master = null;
            if (createDto.MasterId.HasValue)
            {
                master = await _context.Masters.FindAsync(createDto.MasterId.Value);
                if (master == null)
                    return BadRequest(new { error = "Мастер не найден" });
            }

            // Генерируем уникальный номер заказа
            string orderNumber;
            do
            {
                orderNumber = GenerateOrderNumber();
            } while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber));

            // Рассчитываем начальную стоимость
            decimal initialCost = CalculateInitialCost(createDto, master);

            var order = new Order
            {
                OrderNumber = orderNumber,
                ClientId = createDto.ClientId,
                DeviceName = createDto.DeviceName,
                DeviceModel = createDto.DeviceModel,
                SerialNumber = createDto.SerialNumber,
                ProblemDescription = createDto.ProblemDescription,
                Status = "new",
                MasterId = createDto.MasterId,
                TotalCost = initialCost,
                PartsCost = 0,
                LaborCost = master?.HourlyRate * 1.5m ?? 500,
                CreatedAt = DateTime.UtcNow,
                IsUrgent = createDto.IsUrgent,
                AcceptedAt = createDto.MasterId.HasValue ? DateTime.UtcNow : null,
                EstimatedCompletionDate = createDto.IsUrgent 
                    ? DateTime.UtcNow.AddDays(2) 
                    : DateTime.UtcNow.AddDays(5),
                WarrantyPeriod = createDto.WarrantyPeriod // Добавляем поддержку гарантии
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Загружаем связанные данные
            await _context.Entry(order)
                .Reference(o => o.Client)
                .LoadAsync();

            if (order.MasterId.HasValue)
            {
                await _context.Entry(order)
                    .Reference(o => o.Master)
                    .LoadAsync();
            }

            var orderDto = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ClientName = order.Client?.Name ?? "Неизвестный клиент",
                ClientPhone = order.Client?.Phone ?? "",
                Device = order.DeviceName,
                DeviceModel = order.DeviceModel,
                ProblemDescription = order.ProblemDescription,
                Status = order.Status,
                MasterId = order.MasterId,
                MasterName = order.Master?.Name,
                TotalAmount = order.TotalCost,
                PartsCost = order.PartsCost,
                LaborCost = order.LaborCost,
                CreatedDate = order.CreatedAt,
                AcceptedAt = order.AcceptedAt,
                EstimatedCompletionDate = order.EstimatedCompletionDate,
                IsUrgent = order.IsUrgent,
                WarrantyPeriod = order.WarrantyPeriod
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<OrderResponseDto>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequestDto updateDto)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Master)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
                return NotFound(new { error = "Заказ не найден" });

            // Обновляем статус
            order.Status = updateDto.Status;
            
            if (!string.IsNullOrEmpty(updateDto.DiagnosticNotes))
                order.DiagnosticNotes = updateDto.DiagnosticNotes;

            if (updateDto.MasterId.HasValue)
            {
                var master = await _context.Masters.FindAsync(updateDto.MasterId.Value);
                if (master != null)
                {
                    order.MasterId = updateDto.MasterId.Value;
                    order.LaborCost = master.HourlyRate * 2m;
                }
            }

            if (updateDto.EstimatedCompletionDate.HasValue)
                order.EstimatedCompletionDate = updateDto.EstimatedCompletionDate;

            // Обновляем временные метки
            if (updateDto.Status == "accepted" && order.AcceptedAt == null)
                order.AcceptedAt = DateTime.UtcNow;
            else if (updateDto.Status == "in_progress" && order.StartedAt == null)
                order.StartedAt = DateTime.UtcNow;
            else if (updateDto.Status == "completed" && order.CompletedAt == null)
            {
                order.CompletedAt = DateTime.UtcNow;
                order.TotalCost = order.PartsCost + order.LaborCost;
            }

            await _context.SaveChangesAsync();

            var orderDto = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ClientName = order.Client?.Name ?? "Неизвестный клиент",
                ClientPhone = order.Client?.Phone ?? "",
                Device = order.DeviceName,
                DeviceModel = order.DeviceModel,
                ProblemDescription = order.ProblemDescription,
                DiagnosticNotes = order.DiagnosticNotes,
                Status = order.Status,
                MasterId = order.MasterId,
                MasterName = order.Master?.Name,
                TotalAmount = order.TotalCost,
                PartsCost = order.PartsCost,
                LaborCost = order.LaborCost,
                CreatedDate = order.CreatedAt,
                AcceptedAt = order.AcceptedAt,
                StartedAt = order.StartedAt,
                CompletedAt = order.CompletedAt,
                EstimatedCompletionDate = order.EstimatedCompletionDate,
                IsUrgent = order.IsUrgent,
                WarrantyPeriod = order.WarrantyPeriod
            };

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}/cost")]
    public async Task<ActionResult<OrderResponseDto>> UpdateOrderCost(int id, [FromBody] UpdateOrderCostRequestDto updateCostDto)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Master)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
                return NotFound(new { error = "Заказ не найден" });

            order.PartsCost = updateCostDto.PartsCost;
            order.LaborCost = updateCostDto.LaborCost;
            order.TotalCost = order.PartsCost + order.LaborCost;

            await _context.SaveChangesAsync();

            var orderDto = new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ClientName = order.Client?.Name ?? "Неизвестный клиент",
                ClientPhone = order.Client?.Phone ?? "",
                Device = order.DeviceName,
                DeviceModel = order.DeviceModel,
                ProblemDescription = order.ProblemDescription,
                Status = order.Status,
                MasterId = order.MasterId,
                MasterName = order.Master?.Name,
                TotalAmount = order.TotalCost,
                PartsCost = order.PartsCost,
                LaborCost = order.LaborCost,
                CreatedDate = order.CreatedAt,
                EstimatedCompletionDate = order.EstimatedCompletionDate,
                IsUrgent = order.IsUrgent,
                WarrantyPeriod = order.WarrantyPeriod
            };

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { error = "Заказ не найден" });

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Заказ удален" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private string GenerateOrderNumber()
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{date}-{random}";
    }

    private decimal CalculateInitialCost(CreateOrderRequestDto createDto, Master? master)
    {
        decimal cost = 500; // Базовая стоимость диагностики
        
        if (createDto.IsUrgent)
            cost += 1000; // Наценка за срочность
        
        if (master != null)
            cost += master.HourlyRate * 1.5m; // Стоимость работы мастера
        
        return cost;
    }
}

// DTO классы для OrdersController
public class OrderResponseDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? SerialNumber { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public string? DiagnosticNotes { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? MasterId { get; set; }
    public string? MasterName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public bool IsUrgent { get; set; }
    public int WarrantyPeriod { get; set; }
}

public class CreateOrderRequestDto
{
    public int ClientId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? SerialNumber { get; set; }
    public string ProblemDescription { get; set; } = string.Empty;
    public int? MasterId { get; set; }
    public bool IsUrgent { get; set; }
    public int WarrantyPeriod { get; set; } = 90; // Добавляем гарантию
}

public class UpdateOrderStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? DiagnosticNotes { get; set; }
    public int? MasterId { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
}

public class UpdateOrderCostRequestDto
{
    public decimal PartsCost { get; set; }
    public decimal LaborCost { get; set; }
}