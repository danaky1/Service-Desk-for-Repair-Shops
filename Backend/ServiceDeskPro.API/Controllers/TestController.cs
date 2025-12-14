using Microsoft.AspNetCore.Mvc;

namespace ServiceDeskPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new 
        { 
            message = "ServiceDesk Pro API работает!",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            endpoints = new[]
            {
                new { method = "GET", path = "/api/test", description = "Тест API" },
                new { method = "GET", path = "/api/orders", description = "Получить все заказы" },
                new { method = "POST", path = "/api/orders", description = "Создать заказ" },
                new { method = "GET", path = "/api/orders/{id}", description = "Получить заказ по ID" },
                new { method = "PATCH", path = "/api/orders/{id}/status", description = "Обновить статус заказа" }
            }
        });
    }
}