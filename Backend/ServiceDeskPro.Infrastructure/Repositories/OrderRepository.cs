using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Core.Entities;
using ServiceDeskPro.Core.Interfaces;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Master)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Master)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(string status)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Master)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByClientAsync(int clientId)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Master)
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order> CreateAsync(Order order)
    {
        order.OrderNumber = GenerateOrderNumber();
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateOrderNumber()
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{date}-{random}";
    }
}