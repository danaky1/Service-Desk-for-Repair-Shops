using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Core.Entities;
using ServiceDeskPro.Core.Interfaces;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.Infrastructure.Repositories;

public class SparePartRepository : ISparePartRepository
{
    private readonly ApplicationDbContext _context;

    public SparePartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SparePart?> GetByIdAsync(int id)
    {
        return await _context.SpareParts.FindAsync(id);
    }

    public async Task<IEnumerable<SparePart>> GetAllAsync()
    {
        return await _context.SpareParts.ToListAsync();
    }
}