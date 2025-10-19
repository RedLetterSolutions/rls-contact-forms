using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace RLS_Contact_Forms.Models;

/// <summary>
/// Represents a contact form submission stored in PostgreSQL.
/// Supports multi-tenant isolation via SiteId and dynamic metadata fields.
/// </summary>
[Table("contact_submissions")]
public class ContactSubmission
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Site identifier for multi-tenant isolation (e.g., "world1", "test-site")
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("site_id")]
    public string SiteId { get; set; } = string.Empty;

    /// <summary>
    /// Name from contact form
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email address from contact form
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Message from contact form
    /// </summary>
    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Client IP address
    /// </summary>
    [MaxLength(50)]
    [Column("client_ip")]
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the submission was received
    /// </summary>
    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Dynamic metadata fields stored as JSON
    /// Contains any additional form fields (e.g., phone, company, budget_range)
    /// </summary>
    [Column("metadata_json", TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Timestamp when the record was created (set by database)
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates a new ContactSubmission from form data
    /// </summary>
    public static ContactSubmission Create(
        string siteId,
        Dictionary<string, string> formData,
        string clientIp)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        // Extract core fields
        var name = formData.GetValueOrDefault("name", "Anonymous");
        var email = formData.GetValueOrDefault("email", "");
        var message = formData.GetValueOrDefault("message", "");

        // Extract metadata fields (anything not core or internal)
        var coreFields = new HashSet<string> { "name", "email", "message", "_hp", "_ts", "_sig" };
        var metadata = formData
            .Where(kvp => !coreFields.Contains(kvp.Key.ToLowerInvariant()) && !string.IsNullOrWhiteSpace(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var metadataJson = metadata.Any()
            ? JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = false })
            : null;

        return new ContactSubmission
        {
            SiteId = siteId,
            Name = name,
            Email = email,
            Message = message,
            ClientIp = clientIp,
            SubmittedAt = now,
            MetadataJson = metadataJson,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Deserializes the metadata JSON into a dictionary
    /// </summary>
    public Dictionary<string, string> GetMetadata()
    {
        if (string.IsNullOrWhiteSpace(MetadataJson))
        {
            return new Dictionary<string, string>();
        }

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
