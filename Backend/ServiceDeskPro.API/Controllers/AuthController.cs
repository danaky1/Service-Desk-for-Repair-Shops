using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // В демо-режиме ищем любого активного мастера или создаем демо
            var master = await _context.Masters
                .Where(m => m.IsActive)
                .FirstOrDefaultAsync();

            if (master == null)
            {
                // Создаем демо-мастера если нет активных
                master = new ServiceDeskPro.Core.Entities.Master
                {
                    Name = string.IsNullOrWhiteSpace(request.Username) ? 
                           "Демо Мастер" : request.Username,
                    Specialization = "Демо специализация",
                    Email = "demo@servicedesk.ru",
                    Phone = "+7 (999) 000-00-00",
                    HourlyRate = 1000,
                    Rating = 4.5m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Masters.Add(master);
                await _context.SaveChangesAsync();
            }

            // Подсчитываем заказы
            var ordersCount = await _context.Orders.CountAsync(o => o.MasterId == master.Id);
            var currentOrders = await _context.Orders
                .CountAsync(o => o.MasterId == master.Id && 
                               o.Status != "completed" && 
                               o.Status != "cancelled");

            // Генерируем демо-токен
            var token = $"demo-token-{master.Id}-{DateTime.UtcNow.Ticks}";
            
            var response = new LoginResponse
            {
                Success = true,
                MasterId = master.Id,
                MasterName = master.Name,
                Token = token,
                Master = new AuthMasterDto
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
                    OrdersCount = ordersCount,
                    CurrentOrders = currentOrders
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // В случае ошибки возвращаем демо-ответ
            var demoResponse = new LoginResponse
            {
                Success = true,
                MasterId = 1,
                MasterName = "Демо Мастер",
                Token = $"demo-token-1-{DateTime.UtcNow.Ticks}",
                Master = new AuthMasterDto
                {
                    Id = 1,
                    Name = "Демо Мастер",
                    Specialization = "Демо специализация",
                    Email = "demo@servicedesk.ru",
                    Phone = "+7 (999) 000-00-00",
                    HourlyRate = 1000,
                    Rating = 4.5m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    OrdersCount = 0,
                    CurrentOrders = 0
                }
            };
            
            return Ok(demoResponse);
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { 
            success = true, 
            message = "Выход выполнен успешно"
        });
    }

    [HttpGet("current")]
    public async Task<ActionResult<AuthMasterDto>> GetCurrentMaster()
    {
        try
        {
            // Проверяем токен в заголовке
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            int masterId = 1; // По умолчанию демо-мастер
            
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (token.StartsWith("demo-token-"))
                {
                    var parts = token.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedId))
                    {
                        masterId = parsedId;
                    }
                }
            }

            // Ищем мастера
            var master = await _context.Masters
                .FirstOrDefaultAsync(m => m.Id == masterId && m.IsActive);

            if (master == null)
            {
                // Возвращаем демо-мастера
                return Ok(new AuthMasterDto
                {
                    Id = 1,
                    Name = "Демо Мастер",
                    Specialization = "Демо специализация",
                    Email = "demo@servicedesk.ru",
                    Phone = "+7 (999) 000-00-00",
                    HourlyRate = 1000,
                    Rating = 4.5m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    OrdersCount = 0,
                    CurrentOrders = 0
                });
            }

            // Подсчитываем заказы
            var ordersCount = await _context.Orders.CountAsync(o => o.MasterId == master.Id);
            var currentOrders = await _context.Orders
                .CountAsync(o => o.MasterId == master.Id && 
                               o.Status != "completed" && 
                               o.Status != "cancelled");

            return Ok(new AuthMasterDto
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
                OrdersCount = ordersCount,
                CurrentOrders = currentOrders
            });
        }
        catch (Exception)
        {
            // В случае ошибки возвращаем демо-мастера
            return Ok(new AuthMasterDto
            {
                Id = 1,
                Name = "Демо Мастер",
                Specialization = "Демо специализация",
                Email = "demo@servicedesk.ru",
                Phone = "+7 (999) 000-00-00",
                HourlyRate = 1000,
                Rating = 4.5m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                OrdersCount = 0,
                CurrentOrders = 0
            });
        }
    }
}

// DTO классы должны быть в этом файле или импортироваться
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public int MasterId { get; set; }
    public string MasterName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public AuthMasterDto Master { get; set; } = new AuthMasterDto();
}

public class AuthMasterDto
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