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
    }
}
