using Microsoft.EntityFrameworkCore;
using RLS_Contact_Forms.Models;

namespace RLS_Contact_Forms.Data;

/// <summary>
/// Entity Framework Core DbContext for the contact forms database.
/// Configured for PostgreSQL with support for multi-tenant contact submissions.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContactSubmission> ContactSubmissions { get; set; } = null!;
    public DbSet<Site> Sites { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContactSubmission>(entity =>
        {
            // Create index on SiteId for efficient tenant-specific queries
            entity.HasIndex(e => e.SiteId)
                .HasDatabaseName("ix_contact_submissions_site_id");

            // Create index on SubmittedAt for time-based queries
            entity.HasIndex(e => e.SubmittedAt)
                .HasDatabaseName("ix_contact_submissions_submitted_at");

            // Composite index for site + time queries (most common pattern)
            entity.HasIndex(e => new { e.SiteId, e.SubmittedAt })
                .HasDatabaseName("ix_contact_submissions_site_id_submitted_at");

            // Set default value for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Site>(entity =>
        {
            // Configure unique index on SiteId
            entity.HasIndex(s => s.SiteId)
                .IsUnique()
                .HasDatabaseName("IX_sites_site_id");

            // Configure name index
            entity.HasIndex(s => s.Name)
                .HasDatabaseName("IX_sites_name");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        FixDateTimeKinds();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        FixDateTimeKinds();
        return base.SaveChanges();
    }

    private void FixDateTimeKinds()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().GetProperties()
                .Any(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)));

        foreach (var entry in entries)
        {
            var properties = entry.Entity.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

            foreach (var property in properties)
            {
                var value = property.GetValue(entry.Entity);
                if (value is DateTime dt && dt.Kind == DateTimeKind.Unspecified)
                {
                    property.SetValue(entry.Entity, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                }
            }
        }
    }
}
