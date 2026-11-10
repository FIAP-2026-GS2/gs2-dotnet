using Microsoft.EntityFrameworkCore;
using SampaCuida.Domain.Entities;

namespace SampaCuida.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Camera> Cameras => Set<Camera>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
