using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Core.Entities;
using ServiceDeskPro.Core.Interfaces;
using ServiceDeskPro.Infrastructure.Data;

namespace ServiceDeskPro.Infrastructure.Repositories;

public class MasterRepository : IMasterRepository
{
    private readonly ApplicationDbContext _context;

    public MasterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Master?> GetByIdAsync(int id)
    {
        return await _context.Masters.FindAsync(id);
    }

    public async Task<IEnumerable<Master>> GetAllAsync()
    {
        return await _context.Masters.ToListAsync();
    }
}