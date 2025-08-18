using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RLS_Contact_Forms;

public class Contact
{
    private readonly ILogger<Contact> _logger;
    private readonly IConfiguration _configuration;
    private readonly TableServiceClient _tableServiceClient;
    private readonly ISendGridClient _sendGridClient;

    public Contact(ILogger<Contact> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var storageConnectionString = _configuration["AzureWebJobsStorage"];
        _tableServiceClient = new TableServiceClient(storageConnectionString);
        
        var sendGridApiKey = _configuration["SENDGRID_API_KEY"];
        _sendGridClient = new SendGridClient(sendGridApiKey);
    }

    [Function("Contact")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "v1/contact/{siteId}")] HttpRequestData req,
        string siteId)
    {
        // Handle preflight OPTIONS requests
        if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            var optionsResponse = req.CreateResponse(HttpStatusCode.OK);
            AddCorsHeaders(optionsResponse);
            return optionsResponse;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var clientIp = GetClientIpAddress(req);
        
        try
        {
            var siteConfig = LoadSiteConfiguration(siteId);
            if (siteConfig == null)
            {
                _logger.LogWarning("Unknown site: {SiteId}", siteId);
                return CreateBadRequestResponse(req, "Unknown site");
            }

            if (!await CheckRateLimit(siteId, clientIp))
            {
                _logger.LogWarning("Rate limit exceeded for site {SiteId} from IP {ClientIp}", siteId, clientIp);
                return CreateRateLimitResponse(req);
            }

            var formData = await ParseRequestData(req);
            
            if (IsHoneypotTriggered(formData))
            {
                _logger.LogInformation("Honeypot triggered for site {SiteId} from IP {ClientIp}", siteId, clientIp);
                return CreateRedirectResponse(req, siteConfig.RedirectUrl);
            }

            if (!ValidateOrigin(req, siteConfig))
            {
                _logger.LogWarning("Origin validation failed for site {SiteId} from IP {ClientIp}", siteId, clientIp);
                return CreateBadRequestResponse(req, "Origin not allowed");
            }

            if (!ValidateRequiredFields(formData, out var validationError))
            {
                _logger.LogWarning("Validation failed for site {SiteId}: {Error}", siteId, validationError);
                return CreateBadRequestResponse(req, validationError);
            }

            if (!ValidateHmacSignature(formData, siteConfig, siteId))
            {
                _logger.LogWarning("HMAC validation failed for site {SiteId} from IP {ClientIp}", siteId, clientIp);
                return CreateBadRequestResponse(req, "Invalid signature");
            }

            await SendEmail(formData, siteConfig, siteId);
            
            _logger.LogInformation("Contact form submitted successfully for site {SiteId} from IP {ClientIp} in {ElapsedMs}ms", 
                siteId, clientIp, stopwatch.ElapsedMilliseconds);
            
            return CreateRedirectResponse(req, siteConfig.RedirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contact form for site {SiteId} from IP {ClientIp}", siteId, clientIp);
            return CreateBadRequestResponse(req, "An error occurred processing your request");
        }
    }

    private SiteConfiguration? LoadSiteConfiguration(string siteId)
    {
        var prefix = $"sites:{siteId}:";
        
        var toEmail = _configuration[prefix + "to_email"];
        var redirectUrl = _configuration[prefix + "redirect_url"];
        
        if (string.IsNullOrEmpty(toEmail) || string.IsNullOrEmpty(redirectUrl))
        {
            return null;
        }

        return new SiteConfiguration
        {
            ToEmail = toEmail,
            FromEmail = _configuration[prefix + "from_email"] ?? _configuration["DEFAULT_FROM_EMAIL"] ?? "",
            RedirectUrl = redirectUrl,
            AllowOrigins = _configuration[prefix + "allow_origins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
            Secret = _configuration[prefix + "secret"]
        };
    }

    private async Task<Dictionary<string, string>> ParseRequestData(HttpRequestData req)
    {
        var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault() ?? "";
        var body = await new StreamReader(req.Body).ReadToEndAsync();

        if (contentType.Contains("application/json"))
        {
            var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(body);
            return jsonData?.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value?.ToString() ?? ""
            ) ?? new Dictionary<string, string>();
        }
        else
        {
            var formData = System.Web.HttpUtility.ParseQueryString(body);
            return formData.AllKeys.Where(key => key != null)
                .ToDictionary(key => key!, key => formData[key] ?? "");
        }
    }

    private static bool IsHoneypotTriggered(Dictionary<string, string> formData)
    {
        return formData.TryGetValue("_hp", out var honeypot) && !string.IsNullOrEmpty(honeypot);
    }

    private bool ValidateOrigin(HttpRequestData req, SiteConfiguration siteConfig)
    {
        if (!req.Headers.TryGetValues("Origin", out var origins) || !origins.Any())
        {
            return true;
        }

        if (siteConfig.AllowOrigins.Length == 0)
        {
            return true;
        }

        var origin = origins.First();
        return siteConfig.AllowOrigins.Any(allowed => 
            string.Equals(allowed.Trim(), origin, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ValidateRequiredFields(Dictionary<string, string> formData, out string validationError)
    {
        validationError = "";

        if (!formData.TryGetValue("email", out var email) || string.IsNullOrWhiteSpace(email))
        {
            validationError = "Email is required";
            return false;
        }

        if (!formData.TryGetValue("message", out var message) || string.IsNullOrWhiteSpace(message))
        {
            validationError = "Message is required";
            return false;
        }

        return true;
    }

    private bool ValidateHmacSignature(Dictionary<string, string> formData, SiteConfiguration siteConfig, string siteId)
    {
        if (string.IsNullOrEmpty(siteConfig.Secret))
        {
            return true;
        }

        if (!formData.TryGetValue("_ts", out var timestampStr) || 
            !formData.TryGetValue("_sig", out var signature))
        {
            return false;
        }

        if (!long.TryParse(timestampStr, out var timestamp))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > 600) // 10 minutes
        {
            return false;
        }

        var email = formData.GetValueOrDefault("email", "");
        var name = formData.GetValueOrDefault("name", "");
        var message = formData.GetValueOrDefault("message", "");
        var messagePrefix = message.Length > 200 ? message.Substring(0, 200) : message;

        // Include metadata fields in signature for enhanced security
        var metadata = GetMetadataFields(formData);
        var metadataString = string.Join("|", metadata.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));

        var stringToSign = $"{siteId}|{timestamp}|{email}|{name}|{messagePrefix}|{metadataString}";
        var expectedSignature = ComputeHmacSha256(stringToSign, siteConfig.Secret);

        return ConstantTimeEquals(signature, expectedSignature);
    }

    private static string ComputeHmacSha256(string message, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    private async Task<bool> CheckRateLimit(string siteId, string clientIp)
    {
        try
        {
            var tableClient = _tableServiceClient.GetTableClient("ContactRate");
            await tableClient.CreateIfNotExistsAsync();

            var now = DateTime.UtcNow;
            var minute = now.ToString("yyyyMMddHHmm");
            var partitionKey = siteId;
            var rowKey = $"{clientIp}:{minute}";

            var entity = new TableEntity(partitionKey, rowKey)
            {
                ["Timestamp"] = now,
                ["Count"] = 1
            };

            try
            {
                await tableClient.AddEntityAsync(entity);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate limiting check failed, allowing request");
            return true;
        }
    }

    private async Task SendEmail(Dictionary<string, string> formData, SiteConfiguration siteConfig, string siteId)
    {
        try
        {
            var name = formData.GetValueOrDefault("name", "Anonymous");
            var email = formData.GetValueOrDefault("email", "");
            var message = formData.GetValueOrDefault("message", "");

            var subject = $"New contact ({siteId}) from {name}";
            
            // Build email content with core fields and metadata
            var textContent = BuildTextEmailContent(formData);
            var htmlContent = BuildHtmlEmailContent(formData);

            var from = new EmailAddress(siteConfig.FromEmail, "Website Contact");
            var to = new EmailAddress(siteConfig.ToEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, textContent, htmlContent);

            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (IsSuccessStatusCode(response.StatusCode))
            {
                _logger.LogInformation("Email sent successfully for site {SiteId} to {ToEmail}", siteId, siteConfig.ToEmail);
            }
            else
            {
                _logger.LogWarning("SendGrid returned non-success status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email for site {SiteId}", siteId);
        }
    }

    private string BuildTextEmailContent(Dictionary<string, string> formData)
    {
        var content = new StringBuilder();
        var name = formData.GetValueOrDefault("name", "Anonymous");
        var email = formData.GetValueOrDefault("email", "");
        var message = formData.GetValueOrDefault("message", "");

        content.AppendLine($"From: {name} <{email}>");
        content.AppendLine();
        content.AppendLine($"Message: {message}");

        // Add metadata fields
        var metadata = GetMetadataFields(formData);
        if (metadata.Any())
        {
            content.AppendLine();
            content.AppendLine("Additional Information:");
            content.AppendLine("------------------------");
            foreach (var field in metadata)
            {
                var fieldName = FormatFieldName(field.Key);
                content.AppendLine($"{fieldName}: {field.Value}");
            }
        }

        return content.ToString();
    }

    private string BuildHtmlEmailContent(Dictionary<string, string> formData)
    {
        var content = new StringBuilder();
        var name = formData.GetValueOrDefault("name", "Anonymous");
        var email = formData.GetValueOrDefault("email", "");
        var message = formData.GetValueOrDefault("message", "");

        content.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px;'>");
        content.AppendLine($"<p><b>From:</b> {HttpUtility.HtmlEncode(name)} &lt;{HttpUtility.HtmlEncode(email)}&gt;</p>");
        content.AppendLine($"<p><b>Message:</b></p>");
        content.AppendLine($"<p>{HttpUtility.HtmlEncode(message).Replace("\n", "<br>")}</p>");

        // Add metadata fields
        var metadata = GetMetadataFields(formData);
        if (metadata.Any())
        {
            content.AppendLine("<hr style='margin: 20px 0; border: none; border-top: 1px solid #ddd;'>");
            content.AppendLine("<h3 style='color: #333; font-size: 16px; margin-bottom: 10px;'>Additional Information</h3>");
            content.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
            
            foreach (var field in metadata)
            {
                var fieldName = FormatFieldName(field.Key);
                content.AppendLine("<tr>");
                content.AppendLine($"<td style='padding: 8px 0; border-bottom: 1px solid #eee; font-weight: bold; width: 30%;'>{HttpUtility.HtmlEncode(fieldName)}:</td>");
                content.AppendLine($"<td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{HttpUtility.HtmlEncode(field.Value)}</td>");
                content.AppendLine("</tr>");
            }
            
            content.AppendLine("</table>");
        }

        content.AppendLine("</div>");
        return content.ToString();
    }

    private Dictionary<string, string> GetMetadataFields(Dictionary<string, string> formData)
    {
        // Core fields that are not metadata
        var coreFields = new HashSet<string> { "name", "email", "message", "_hp", "_ts", "_sig" };
        
        return formData
            .Where(kvp => !coreFields.Contains(kvp.Key.ToLowerInvariant()) && !string.IsNullOrWhiteSpace(kvp.Value))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private string FormatFieldName(string fieldName)
    {
        // Convert field names like "phone_number" to "Phone Number"
        return string.Join(" ", fieldName.Split('_', '-'))
            .Split(' ')
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant())
            .Aggregate((a, b) => $"{a} {b}");
    }

    private static bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Accepted;
    }

    private static string GetClientIpAddress(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.First().Split(',')[0].Trim();
        }

        if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
        {
            return realIp.First();
        }

        return "unknown";
    }

    private static HttpResponseData CreateRedirectResponse(HttpRequestData req, string location)
    {
        var response = req.CreateResponse(HttpStatusCode.SeeOther);
        response.Headers.Add("Location", location);
        AddCorsHeaders(response);
        return response;
    }

    private static HttpResponseData CreateBadRequestResponse(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        response.WriteString(message);
        AddCorsHeaders(response);
        return response;
    }

    private static HttpResponseData CreateRateLimitResponse(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.TooManyRequests);
        response.WriteString("Rate limit exceeded. Please try again later.");
        AddCorsHeaders(response);
        return response;
    }

    private static void AddCorsHeaders(HttpResponseData response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
    }
}

public class SiteConfiguration
{
    public string ToEmail { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string RedirectUrl { get; set; } = "";
    public string[] AllowOrigins { get; set; } = Array.Empty<string>();
    public string? Secret { get; set; }
}