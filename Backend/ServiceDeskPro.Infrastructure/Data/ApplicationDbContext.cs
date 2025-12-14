using Microsoft.EntityFrameworkCore;
using ServiceDeskPro.Core.Entities;

namespace ServiceDeskPro.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) { }

    public DbSet<Client> Clients { get; set; }
    public DbSet<Master> Masters { get; set; }
    public DbSet<SparePart> SpareParts { get; set; }
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();
    }
}