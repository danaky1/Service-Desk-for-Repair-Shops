using ServiceDeskPro.Core.DTOs;
using ServiceDeskPro.Core.Entities;
using ServiceDeskPro.Core.Interfaces;

namespace ServiceDeskPro.Core.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto);
    Task<OrderDto> GetOrderAsync(int id);
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(string status);
    Task<OrderDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IMasterRepository _masterRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IClientRepository clientRepository,
        IMasterRepository masterRepository)
    {
        _orderRepository = orderRepository;
        _clientRepository = clientRepository;
        _masterRepository = masterRepository;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createDto)
    {
        var client = await _clientRepository.GetByIdAsync(createDto.ClientId);
        if (client == null)
            throw new Exception("Клиент не найден");

        Master? master = null;
        if (createDto.MasterId.HasValue)
        {
            master = await _masterRepository.GetByIdAsync(createDto.MasterId.Value);
        }

        var order = new Order
        {
            ClientId = createDto.ClientId,
            DeviceName = createDto.DeviceName,
            DeviceModel = createDto.DeviceModel,
            SerialNumber = createDto.SerialNumber,
            ProblemDescription = createDto.ProblemDescription,
            MasterId = createDto.MasterId,
            Status = "new",
            IsUrgent = createDto.IsUrgent,
            CreatedAt = DateTime.UtcNow,
            AcceptedAt = createDto.MasterId.HasValue ? DateTime.UtcNow : null
        };

        var createdOrder = await _orderRepository.CreateAsync(order);
        return MapToDto(createdOrder);
    }

    public async Task<OrderDto> GetOrderAsync(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Заказ не найден");

        return MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(string status)
    {
        var orders = await _orderRepository.GetByStatusAsync(status);
        return orders.Select(MapToDto);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto updateDto)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            throw new Exception("Заказ не найден");

        order.Status = updateDto.Status;
        
        if (!string.IsNullOrEmpty(updateDto.DiagnosticNotes))
            order.DiagnosticNotes = updateDto.DiagnosticNotes;

        if (updateDto.MasterId.HasValue)
            order.MasterId = updateDto.MasterId.Value;

        if (updateDto.EstimatedCompletionDate.HasValue)
            order.EstimatedCompletionDate = updateDto.EstimatedCompletionDate;

        // Обновляем временные метки в зависимости от статуса
        if (updateDto.Status == "accepted" && order.AcceptedAt == null)
            order.AcceptedAt = DateTime.UtcNow;
        else if (updateDto.Status == "in_progress" && order.StartedAt == null)
            order.StartedAt = DateTime.UtcNow;
        else if (updateDto.Status == "completed" && order.CompletedAt == null)
            order.CompletedAt = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order);
        return MapToDto(order);
    }

    private OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            ClientName = order.Client?.Name ?? "Неизвестный клиент",
            ClientPhone = order.Client?.Phone ?? "",
            Device = order.DeviceName,
            DeviceModel = order.DeviceModel,
            ProblemDescription = order.ProblemDescription,
            Status = order.Status,
            MasterName = order.Master?.Name,
            TotalAmount = order.TotalCost,
            CreatedDate = order.CreatedAt,
            EstimatedCompletionDate = order.EstimatedCompletionDate,
            IsUrgent = order.IsUrgent
        };
    }
}