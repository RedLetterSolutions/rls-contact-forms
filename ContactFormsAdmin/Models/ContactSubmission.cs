using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ContactFormsAdmin.Models;

[Table("contact_submissions")]
public class ContactSubmission
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

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("client_ip")]
    public string ClientIp { get; set; } = string.Empty;

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; }

    [Column("metadata_json", TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Dictionary<string, string> GetMetadata()
    {
        if (string.IsNullOrWhiteSpace(MetadataJson))
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
