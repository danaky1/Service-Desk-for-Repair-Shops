using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MastersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MastersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MasterDto>>> GetMasters()
    {
        try
        {
            var masters = await _context.Masters
                .Include(m => m.Orders)
                .Select(m => new MasterDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Specialization = m.Specialization,
                    Email = m.Email,
                    Phone = m.Phone,
                    HourlyRate = m.HourlyRate,
                    Rating = m.Rating,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt,
                    OrdersCount = m.Orders.Count,
                    CurrentOrders = m.Orders.Count(o => o.Status != "completed" && o.Status != "cancelled")
                })
                .ToListAsync();

            return Ok(masters);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MasterDto>> GetMaster(int id)
    {
        try
        {
            var master = await _context.Masters
                .Include(m => m.Orders)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (master == null)
                return NotFound(new { error = "Мастер не найден" });

            var masterDto = new MasterDto
            {
                Id = master.Id,
                Name = master.Name,
                Specialization = master.Specialization,
                Email = master.Email,
                Phone = master.Phone,
                HourlyRate = master.HourlyRate,
                Rating = master.Rating,
                IsActive = master.IsActive,
                CreatedAt = master.CreatedAt,
                OrdersCount = master.Orders.Count,
                CurrentOrders = master.Orders.Count(o => o.Status != "completed" && o.Status != "cancelled")
            };

            return Ok(masterDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class MasterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Rating { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrdersCount { get; set; }
    public int CurrentOrders { get; set; }
}