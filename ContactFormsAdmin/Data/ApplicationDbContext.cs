using Microsoft.EntityFrameworkCore;
using ContactFormsAdmin.Models;

namespace ContactFormsAdmin.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContactSubmission> ContactSubmissions { get; set; } = null!;
    public DbSet<Webhook> Webhooks { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<Site> Sites { get; set; } = null!;
    public DbSet<AdminUser> AdminUsers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContactSubmission>(entity =>
        {
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.SubmittedAt);
            entity.HasIndex(e => new { e.SiteId, e.SubmittedAt });
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.HasIndex(e => e.SiteId);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasIndex(e => e.SiteId).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100);
        });
    }

    public override int SaveChanges()
    {
        FixDateTimeKinds();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        FixDateTimeKinds();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void FixDateTimeKinds()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime && dateTime.Kind == DateTimeKind.Unspecified)
                {
                    property.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
            }
        }
    }
}
