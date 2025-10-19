using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactFormsAdmin.Models;

[Table("webhooks")]
public class Webhook
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("site_id")]
    public string SiteId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("description")]
    public string? Description { get; set; }

    [MaxLength(200)]
    [Column("events")]
    public string? Events { get; set; }

    [MaxLength(100)]
    [Column("secret")]
    public string? Secret { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

    [Column("last_triggered_at")]
    public DateTime? LastTriggeredAt { get; set; }

    [Column("last_success")]
    public bool? LastSuccess { get; set; }

    [Column("last_error")]
    public string? LastError { get; set; }
}
