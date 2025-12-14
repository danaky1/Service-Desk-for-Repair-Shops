using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClientsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients()
    {
        try
        {
            var clients = await _context.Clients
                .Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Email = c.Email,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    OrdersCount = c.Orders.Count
                })
                .ToListAsync();

            return Ok(clients);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDto>> GetClient(int id)
    {
        try
        {
            var client = await _context.Clients
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return NotFound(new { error = "Клиент не найден" });

            var clientDto = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                Phone = client.Phone,
                Email = client.Email,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                OrdersCount = client.Orders.Count
            };

            return Ok(clientDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientDto createDto)
    {
        try
        {
            var client = new Core.Entities.Client
            {
                Name = createDto.Name,
                Phone = createDto.Phone,
                Email = createDto.Email,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            var clientDto = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                Phone = client.Phone,
                Email = client.Email,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                OrdersCount = 0
            };

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, clientDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClientDto>> UpdateClient(int id, [FromBody] UpdateClientDto updateDto)
    {
        try
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound(new { error = "Клиент не найден" });

            client.Name = updateDto.Name;
            client.Phone = updateDto.Phone;
            client.Email = updateDto.Email;
            client.IsActive = updateDto.IsActive;

            await _context.SaveChangesAsync();

            var clientDto = new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                Phone = client.Phone,
                Email = client.Email,
                IsActive = client.IsActive,
                CreatedAt = client.CreatedAt,
                OrdersCount = await _context.Orders.CountAsync(o => o.ClientId == client.Id)
            };

            return Ok(clientDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        try
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound(new { error = "Клиент не найден" });

            // Проверяем, есть ли связанные заказы
            var hasOrders = await _context.Orders.AnyAsync(o => o.ClientId == id);
            if (hasOrders)
            {
                return BadRequest(new { error = "Нельзя удалить клиента, у которого есть заказы" });
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Клиент удален" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrdersCount { get; set; }
}

public class CreateClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class UpdateClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}