using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RLS_Contact_Forms.Data;
using RLS_Contact_Forms.Models;

namespace RLS_Contact_Forms.Services;

/// <summary>
/// Repository for storing and retrieving contact form submissions from PostgreSQL.
/// Provides multi-tenant isolation through filtering by SiteId.
/// </summary>
public class SubmissionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger _logger;

    public SubmissionRepository(ApplicationDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database by running pending migrations.
    /// This method is idempotent and safe to call multiple times.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply database migrations");
            throw;
        }
    }

    /// <summary>
    /// Stores a contact form submission in the database.
    /// Returns true if successful, false otherwise.
    /// </summary>
    public async Task<bool> SaveSubmissionAsync(ContactSubmission submission)
    {
        try
        {
            _context.ContactSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Contact submission saved successfully for site {SiteId}, ID: {Id}",
                submission.SiteId,
                submission.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save contact submission for site {SiteId}",
                submission.SiteId);
            return false;
        }
    }

    /// <summary>
    /// Retrieves all submissions for a specific site.
    /// Results are ordered by SubmittedAt descending (newest first).
    /// </summary>
    public async Task<List<ContactSubmission>> GetSubmissionsBySiteAsync(string siteId, int maxResults = 100)
    {
        try
        {
            var submissions = await _context.ContactSubmissions
                .Where(s => s.SiteId == siteId)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(maxResults)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} submissions for site {SiteId}",
                submissions.Count,
                siteId);

            return submissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve submissions for site {SiteId}", siteId);
            return new List<ContactSubmission>();
        }
    }

    /// <summary>
    /// Retrieves a specific submission by ID.
    /// </summary>
    public async Task<ContactSubmission?> GetSubmissionAsync(long id)
    {
        try
        {
            return await _context.ContactSubmissions.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve submission with ID: {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Retrieves a specific submission by ID for a specific site (ensures tenant isolation).
    /// </summary>
    public async Task<ContactSubmission?> GetSubmissionAsync(string siteId, long id)
    {
        try
        {
            return await _context.ContactSubmissions
                .FirstOrDefaultAsync(s => s.Id == id && s.SiteId == siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve submission for site {SiteId}, ID: {Id}", siteId, id);
            return null;
        }
    }

    /// <summary>
    /// Deletes a specific submission by ID.
    /// Returns true if successful, false otherwise.
    /// </summary>
    public async Task<bool> DeleteSubmissionAsync(long id)
    {
        try
        {
            var submission = await _context.ContactSubmissions.FindAsync(id);
            if (submission == null)
            {
                _logger.LogWarning("Submission not found for deletion: ID {Id}", id);
                return false;
            }

            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Submission deleted successfully: ID {Id}, Site {SiteId}",
                id,
                submission.SiteId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete submission with ID: {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// Deletes a specific submission by ID for a specific site (ensures tenant isolation).
    /// Returns true if successful, false otherwise.
    /// </summary>
    public async Task<bool> DeleteSubmissionAsync(string siteId, long id)
    {
        try
        {
            var submission = await _context.ContactSubmissions
                .FirstOrDefaultAsync(s => s.Id == id && s.SiteId == siteId);

            if (submission == null)
            {
                _logger.LogWarning("Submission not found for deletion: site {SiteId}, ID {Id}", siteId, id);
                return false;
            }

            _context.ContactSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Submission deleted successfully for site {SiteId}, ID: {Id}",
                siteId,
                id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete submission for site {SiteId}, ID: {Id}", siteId, id);
            return false;
        }
    }

    /// <summary>
    /// Gets the count of submissions for a specific site.
    /// </summary>
    public async Task<int> GetSubmissionCountAsync(string siteId)
    {
        try
        {
            return await _context.ContactSubmissions
                .Where(s => s.SiteId == siteId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get submission count for site {SiteId}", siteId);
            return 0;
        }
    }

    /// <summary>
    /// Gets submissions for a specific site within a date range.
    /// </summary>
    public async Task<List<ContactSubmission>> GetSubmissionsByDateRangeAsync(
        string siteId,
        DateTime startDate,
        DateTime endDate,
        int maxResults = 100)
    {
        try
        {
            var submissions = await _context.ContactSubmissions
                .Where(s => s.SiteId == siteId &&
                           s.SubmittedAt >= startDate &&
                           s.SubmittedAt <= endDate)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(maxResults)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} submissions for site {SiteId} between {StartDate} and {EndDate}",
                submissions.Count,
                siteId,
                startDate,
                endDate);

            return submissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve submissions for site {SiteId} in date range",
                siteId);
            return new List<ContactSubmission>();
        }
    }
}
