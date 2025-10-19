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
    }
}
