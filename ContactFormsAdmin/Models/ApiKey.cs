using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContactFormsAdmin.Models;

[Table("api_keys")]
public class ApiKey
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    [Column("key_hash")]
    public string KeyHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Column("key_prefix")]
    public string KeyPrefix { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
}
