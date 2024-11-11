using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        // #1 - The ConcurrencyToken configuration can be applied individually to each entity
        builder.Entity<ExampleEntity>()
               .Property(x => x.Version)
               .IsConcurrencyToken();

        // #2 - Through IEntityTypeConfiguration
        // builder.ApplyConfiguration(new ExampleEntityTypeConfiguration());

        // #3 - Or by iterating over the entities with a method call
        // setConcurrencyTokenForEntityBase(builder);

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

    private static void setConcurrencyTokenForEntityBase(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            // if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))

            IMutableProperty? versionProperty = entityType.FindProperty(nameof(EntityBase.Version));

            if (versionProperty is null) continue;

            versionProperty.IsConcurrencyToken = true;
        }
    }
}

public sealed class ExampleEntityTypeConfiguration : EntityBaseTypeConfiguration<ExampleEntity>
{
    public override void Configure(EntityTypeBuilder<ExampleEntity> builder)
    {
        // Add configurations related to the entity

        base.Configure(builder);
    }
}

public abstract class EntityBaseTypeConfiguration<TEntityBase> : IEntityTypeConfiguration<TEntityBase> where TEntityBase : EntityBase
{
    public virtual void Configure(EntityTypeBuilder<TEntityBase> builder)
    {
        builder.Property(p => p.Version)
               .IsConcurrencyToken();
    }
}
