using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SparePartsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SparePartsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SparePartDto>>> GetSpareParts()
    {
        try
        {
            var parts = await _context.SpareParts
                .Select(p => new SparePartDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Manufacturer = p.Manufacturer,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    Price = p.Price,
                    MinStockLevel = p.MinStockLevel,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(parts);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SparePartDto>> GetSparePart(int id)
    {
        try
        {
            var part = await _context.SpareParts.FindAsync(id);

            if (part == null)
                return NotFound(new { error = "Запчасть не найдена" });

            var partDto = new SparePartDto
            {
                Id = part.Id,
                Name = part.Name,
                Sku = part.Sku,
                Manufacturer = part.Manufacturer,
                Description = part.Description,
                Quantity = part.Quantity,
                Price = part.Price,
                MinStockLevel = part.MinStockLevel,
                CreatedAt = part.CreatedAt
            };

            return Ok(partDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<SparePartDto>> CreateSparePart([FromBody] CreateSparePartDto createDto)
    {
        try
        {
            var part = new Core.Entities.SparePart
            {
                Name = createDto.Name,
                Sku = createDto.Sku,
                Manufacturer = createDto.Manufacturer,
                Description = createDto.Description,
                Quantity = createDto.Quantity,
                Price = createDto.Price,
                MinStockLevel = createDto.MinStockLevel,
                CreatedAt = DateTime.UtcNow
            };

            _context.SpareParts.Add(part);
            await _context.SaveChangesAsync();

            var partDto = new SparePartDto
            {
                Id = part.Id,
                Name = part.Name,
                Sku = part.Sku,
                Manufacturer = part.Manufacturer,
                Description = part.Description,
                Quantity = part.Quantity,
                Price = part.Price,
                MinStockLevel = part.MinStockLevel,
                CreatedAt = part.CreatedAt
            };

            return CreatedAtAction(nameof(GetSparePart), new { id = part.Id }, partDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SparePartDto>> UpdateSparePart(int id, [FromBody] UpdateSparePartDto updateDto)
    {
        try
        {
            var part = await _context.SpareParts.FindAsync(id);
            if (part == null)
                return NotFound(new { error = "Запчасть не найдена" });

            part.Name = updateDto.Name;
            part.Sku = updateDto.Sku;
            part.Manufacturer = updateDto.Manufacturer;
            part.Description = updateDto.Description;
            part.Quantity = updateDto.Quantity;
            part.Price = updateDto.Price;
            part.MinStockLevel = updateDto.MinStockLevel;

            await _context.SaveChangesAsync();

            var partDto = new SparePartDto
            {
                Id = part.Id,
                Name = part.Name,
                Sku = part.Sku,
                Manufacturer = part.Manufacturer,
                Description = part.Description,
                Quantity = part.Quantity,
                Price = part.Price,
                MinStockLevel = part.MinStockLevel,
                CreatedAt = part.CreatedAt
            };

            return Ok(partDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPatch("{id}/use")]
    public async Task<ActionResult<SparePartDto>> UseSparePart(int id, [FromBody] UseSparePartDto useDto)
    {
        try
        {
            var part = await _context.SpareParts.FindAsync(id);
            if (part == null)
                return NotFound(new { error = "Запчасть не найдена" });

            if (part.Quantity < useDto.Quantity)
                return BadRequest(new { error = $"Недостаточно запчастей. В наличии: {part.Quantity}" });

            part.Quantity -= useDto.Quantity;

            await _context.SaveChangesAsync();

            var partDto = new SparePartDto
            {
                Id = part.Id,
                Name = part.Name,
                Sku = part.Sku,
                Manufacturer = part.Manufacturer,
                Description = part.Description,
                Quantity = part.Quantity,
                Price = part.Price,
                MinStockLevel = part.MinStockLevel,
                CreatedAt = part.CreatedAt
            };

            return Ok(partDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSparePart(int id)
    {
        try
        {
            var part = await _context.SpareParts.FindAsync(id);
            if (part == null)
                return NotFound(new { error = "Запчасть не найдена" });

            _context.SpareParts.Remove(part);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Запчасть удалена" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class SparePartDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int MinStockLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSparePartDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int MinStockLevel { get; set; }
}

public class UpdateSparePartDto
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int MinStockLevel { get; set; }
}

public class UseSparePartDto
{
    public int Quantity { get; set; }
}