using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RLS_Contact_Forms.Models;

[Table("sites")]
public class Site
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("site_id")]
    public string SiteId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("to_email")]
    public string ToEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("from_email")]
    public string FromEmail { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("redirect_url")]
    public string? RedirectUrl { get; set; }

    [MaxLength(1000)]
    [Column("allowed_origins")]
    public string? AllowedOrigins { get; set; }

    [MaxLength(100)]
    [Column("secret")]
    public string? Secret { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

    // Helper properties for Azure Functions
    [NotMapped]
    public string[] AllowOriginsArray 
    { 
        get => string.IsNullOrEmpty(AllowedOrigins) 
            ? Array.Empty<string>() 
            : AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToArray();
    }
}