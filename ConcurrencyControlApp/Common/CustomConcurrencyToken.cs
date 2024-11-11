using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ConcurrencyControlApp.Common;

// The following example demonstrates how to create a unified solution for SQL Server and Postgres to manage and handle the concurrency token
// Handling concurrency conflicts with EF: https://youtu.be/cEQxX1yM-Zc
public abstract class EntityBase
{
    public Guid Id { get; set; }
    public Guid Version { get; set; }
}

public sealed class ExampleEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
}

public sealed class ExampleDbContext(DbContextOptions _options) : DbContext(_options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ExampleEntity>()
               .Property(x => x.Version)
               .IsConcurrencyToken();

        base.OnModelCreating(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        EntityEntry<ExampleEntity>[] entityEntries = ChangeTracker
            .Entries<ExampleEntity>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified).ToArray();

        foreach (EntityEntry<ExampleEntity> entityEntry in entityEntries)
        {
            entityEntry.Entity.Version = Guid.NewGuid();
        }

        return base.SaveChangesAsync(ct);
    }
}
