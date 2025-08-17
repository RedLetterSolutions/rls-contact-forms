# Multi-Tenant Contact Form Service

A **production-ready** .NET Azure Functions service that provides secure, multi-tenant contact form handling for any website. Submit forms via simple HTML POST or JSON fetch, with automatic email delivery via SendGrid and seamless 303-redirects back to your site.

## Architecture Overview

```
┌─────────────────┐    POST     ┌──────────────────┐    Email    ┌─────────────┐
│   Your Website  │────────────▶│  Azure Function  │────────────▶│  SendGrid   │
│                 │             │                  │             │             │
│ ┌─────────────┐ │◄────────────│ ┌──────────────┐ │             │             │
│ │    Form     │ │  303 Redir  │ │ Rate Limiter │ │             │             │
│ │             │ │             │ │ HMAC Verify  │ │             │             │
│ │ Honeypot    │ │             │ │ Origin Check │ │             │             │
│ └─────────────┘ │             │ └──────────────┘ │             │             │
└─────────────────┘             └──────────────────┘             └─────────────┘
                                          │
                                          ▼
                                ┌──────────────────┐
                                │ Azure Table      │
                                │ (Rate Limiting)  │
                                └──────────────────┘
```

## Features

- ✅ **Multi-tenant**: Support unlimited sites with individual configuration
- ✅ **Dual input**: HTML form POST + JSON fetch support
- ✅ **Security**: Honeypot, CORS, HMAC signatures, rate limiting
- ✅ **Email delivery**: SendGrid integration with HTML/text formats
- ✅ **Seamless UX**: 303-redirects keep users on their own domain
- ✅ **Production-ready**: Comprehensive error handling and logging

## Quick Start

### 1. Deploy to Azure

```bash
# Clone and navigate
git clone <your-repo>
cd RLS_Contact_Forms

# Deploy to Azure Functions
func azure functionapp publish your-function-app-name
```

### 2. Configure Environment Variables

Set these in your Azure Function App settings:

```bash
# Required
SENDGRID_API_KEY=SG.your-sendgrid-api-key
DEFAULT_FROM_EMAIL=no-reply@youragency.com
AzureWebJobsStorage=your-storage-connection-string

# Per-site configuration (example for "acme" site)
sites:acme:to_email=contact@acmecorp.com
sites:acme:from_email=noreply@acmecorp.com
sites:acme:redirect_url=https://acmecorp.com/thanks
sites:acme:allow_origins=https://acmecorp.com,https://www.acmecorp.com
sites:acme:secret=your-optional-hmac-secret
```

### 3. Add Form to Your Website

```html
<form action="https://your-function-app.azurewebsites.net/v1/contact/acme" method="POST">
    <input name="name" placeholder="Your Name" required>
    <input type="email" name="email" placeholder="Email" required>
    <textarea name="message" placeholder="Message" required></textarea>
    <input type="text" name="_hp" style="display:none">
    <button type="submit">Send</button>
</form>
```

## Local Development

### Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools v4
- Azure Storage Emulator (or Azurite)

### Setup

```bash
# Install dependencies
dotnet restore

# Copy settings template
cp local.settings.json.example local.settings.json

# Edit local.settings.json with your values
# At minimum, set SENDGRID_API_KEY and site configurations

# Start local development
func start
```

Your function will be available at: `http://localhost:7071/v1/contact/{siteId}`

## Adding a New Site (60-Second Checklist)

To onboard a new website called "newsite":

1. **Add environment variables** (Azure Portal → Function App → Configuration):
   ```
   sites:newsite:to_email=contact@newsite.com
   sites:newsite:from_email=noreply@newsite.com  
   sites:newsite:redirect_url=https://newsite.com/thank-you
   sites:newsite:allow_origins=https://newsite.com,https://www.newsite.com
   sites:newsite:secret=generate-random-secret-here  # Optional
   ```

2. **Update their form action**:
   ```html
   <form action="https://your-function-app.azurewebsites.net/v1/contact/newsite" method="POST">
   ```

3. **Test the integration**:
   - Submit a test form
   - Verify email delivery
   - Confirm redirect works

**That's it!** No code changes or redeployment needed.

## Configuration Reference

### Global Settings

| Variable | Required | Description |
|----------|----------|-------------|
| `SENDGRID_API_KEY` | ✅ | SendGrid API key for email delivery |
| `DEFAULT_FROM_EMAIL` | ✅ | Fallback sender email address |
| `AzureWebJobsStorage` | ✅ | Azure Storage connection (for rate limiting) |

### Per-Site Settings

Replace `{siteId}` with your site identifier (e.g., "acme", "world1"):

| Variable | Required | Description |
|----------|----------|-------------|
| `sites:{siteId}:to_email` | ✅ | Where to send contact form emails |
| `sites:{siteId}:redirect_url` | ✅ | Where to redirect after successful submission |
| `sites:{siteId}:from_email` | ❌ | Sender email (defaults to `DEFAULT_FROM_EMAIL`) |
| `sites:{siteId}:allow_origins` | ❌ | Comma-separated allowed origins for CORS |
| `sites:{siteId}:secret` | ❌ | HMAC secret for signature verification |

## Security Features

### 1. Honeypot Protection

Include a hidden `_hp` field. If filled, the form appears to succeed but no email is sent:

```html
<input type="text" name="_hp" style="display:none" tabindex="-1">
```

### 2. Origin Allowlist

Requests with an `Origin` header are checked against `allow_origins`. Configure this for AJAX submissions:

```
sites:mysite:allow_origins=https://mysite.com,https://www.mysite.com
```

### 3. HMAC Signature Verification

For enhanced security, include timestamped signatures:

```html
<!-- Generated server-side -->
<input type="hidden" name="_ts" value="1640995200">
<input type="hidden" name="_sig" value="computed-hmac-sha256">
```

**Signature computation:**
```
String to sign: {siteId}|{timestamp}|{email}|{name}|{first_200_chars_of_message}
Signature: HMAC-SHA256-hex(string_to_sign, sites:{siteId}:secret)
```

### 4. Rate Limiting

- **Rate**: 1 request per 10 seconds per (siteId, IP address)
- **Storage**: Azure Table Storage (`ContactRate` table)
- **Response**: HTTP 429 on limit exceeded

## API Reference

### Endpoint

```
POST /v1/contact/{siteId}
```

### Content Types

- `application/x-www-form-urlencoded` (HTML forms)
- `application/json` (AJAX/fetch)

### Request Fields

| Field | Required | Description |
|-------|----------|-------------|
| `email` | ✅ | Sender's email address |
| `message` | ✅ | Message content |
| `name` | ❌ | Sender's name (defaults to "Anonymous") |
| `_hp` | ❌ | Honeypot field (should be empty) |
| `_ts` | ❌* | Unix timestamp (required if HMAC enabled) |
| `_sig` | ❌* | HMAC-SHA256 signature (required if HMAC enabled) |

*Required only if `sites:{siteId}:secret` is configured.

### Response Codes

| Code | Description |
|------|-------------|
| `303` | Success - redirects to `sites:{siteId}:redirect_url` |
| `400` | Bad request (missing fields, invalid signature, unknown site) |
| `429` | Rate limit exceeded |

## Email Format

### Subject
```
New contact ({siteId}) from {name}
```

### Text Body
```
From: {name} <{email}>

{message}
```

### HTML Body
```html
<p><b>From:</b> {name} &lt;{email}&gt;</p>
<p>{message}</p>
```

## Troubleshooting

### Common Issues

**"Unknown site" error**
- Verify `sites:{siteId}:to_email` and `sites:{siteId}:redirect_url` are set
- Check that {siteId} in URL matches configuration exactly

**CORS errors**
- Add your domain to `sites:{siteId}:allow_origins`
- Ensure origins include protocol (https://)

**Rate limiting issues**
- Check Azure Storage connection
- Verify `AzureWebJobsStorage` is configured

**Emails not sending**
- Verify `SENDGRID_API_KEY` is valid
- Check SendGrid account status and sending limits
- Review function logs for SendGrid API errors

### Monitoring

View logs in Azure Portal:
- Function App → Functions → Contact → Monitor
- Application Insights (if configured)

Key log messages:
- `Contact form submitted successfully` - Normal operation
- `Rate limit exceeded` - Rate limiting triggered
- `Honeypot triggered` - Spam attempt blocked
- `HMAC validation failed` - Security signature invalid

## Security Best Practices

### Email Security
- Configure **SPF** and **DKIM** records for your sending domains in SendGrid
- Use dedicated sending domains (not shared)
- Monitor SendGrid reputation and delivery metrics

### Function Security
- Use **Application Insights** for monitoring and alerting
- Set up **Azure Monitor** alerts for unusual traffic patterns
- Regularly rotate HMAC secrets
- Consider adding **Azure Front Door** or **CloudFlare** for DDoS protection

### Additional Protections
- **Turnstile/reCAPTCHA**: Add client-side verification before form submission
- **Content filtering**: Implement keyword filtering for spam detection
- **Geographic restrictions**: Block requests from specific countries if needed

## Deployment

### GitHub Actions (Optional)

See `azure-functions-deploy.yml` for automated CI/CD pipeline.

### Manual Deployment

```bash
# Build and publish
dotnet publish --configuration Release

# Deploy using Azure CLI
az functionapp deployment source config-zip \
  --resource-group your-resource-group \
  --name your-function-app \
  --src publish.zip
```

## Support

For issues and feature requests, please check:
1. This README for configuration guidance
2. Function logs in Azure Portal
3. SendGrid delivery logs
4. Azure Storage metrics for rate limiting

## License

This project is licensed under the MIT License.