# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Local Development
```bash
# Setup local environment
cd api
dotnet restore
cp local.settings.json.example local.settings.json
# Edit local.settings.json with your SendGrid API key and site configurations

# Run locally (requires Azure Functions Core Tools v4)
func start
# Function available at: http://localhost:7071/v1/contact/{siteId}
```

### Build and Deploy
```bash
# Build project
cd api
dotnet build --configuration Release

# Publish for deployment
dotnet publish --configuration Release --output ./output

# Deploy to Azure (replace with your function app name)
func azure functionapp publish your-function-app-name
```

### Testing
- No automated tests are currently implemented
- Test manually by submitting forms to the local endpoint
- Verify email delivery through SendGrid logs
- Check Azure Storage for rate limiting table entries

## Architecture Overview

This is a **multi-tenant Azure Functions service** that handles contact form submissions for multiple websites through a single endpoint. The core architecture consists of:

### Single Function Entry Point
- **Route**: `POST /v1/contact/{siteId}` 
- **File**: `api/Contact.cs` - Contains all business logic in the `ContactFunction` class
- **Multi-tenant**: Each `{siteId}` represents a different client website with isolated configuration

### Configuration-Driven Multi-Tenancy
Configuration is loaded per-site using the pattern `sites:{siteId}:*`:
- `sites:{siteId}:to_email` - Where emails are sent
- `sites:{siteId}:redirect_url` - Where users are redirected after submission
- `sites:{siteId}:from_email` - Sender email (optional)
- `sites:{siteId}:allow_origins` - CORS origins (optional)
- `sites:{siteId}:secret` - HMAC secret for signature verification (optional)

### Security Pipeline
The function processes requests through multiple security layers in sequence:
1. **Rate Limiting** - 1 request per 10 seconds per (siteId, IP) using Azure Table Storage
2. **Honeypot Detection** - Hidden `_hp` field to catch bots
3. **Origin Validation** - CORS checking against configured allowed origins
4. **HMAC Verification** - Optional cryptographic signatures with timestamp validation
5. **Input Validation** - Required fields (email, message)

### Request Processing Flow
1. Parse both `application/x-www-form-urlencoded` and `application/json` content types
2. Apply security pipeline (rate limit → honeypot → origin → HMAC → validation)
3. Send email via SendGrid with both HTML and text formats
4. Return **303 See Other** redirect to keep users on their domain

### Key Implementation Details

**Rate Limiting**: Uses Azure Table Storage with PartitionKey=`siteId`, RowKey=`{ip}:{yyyyMMddHHmm}`. Creates entity if not exists, returns 429 if already exists.

**HMAC Security**: String to sign format: `{siteId}|{timestamp}|{email}|{name}|{first_200_chars_of_message}`. Uses HMAC-SHA256 with constant-time comparison.

**Email Format**: 
- Subject: `New contact ({siteId}) from {name}`
- Dual format: plain text and HTML with proper escaping

**Error Handling**: All errors return user-friendly messages while detailed errors are logged. SendGrid failures are logged but don't block the user flow.

## Adding New Sites

Adding a new client requires **zero code changes**:
1. Add configuration variables in Azure Portal with the `sites:{newSiteId}:*` pattern
2. Update client's form action URL to include the new `{siteId}`
3. Test submission and email delivery

## Configuration Management

**Local**: Copy `api/local.settings.json.example` to `api/local.settings.json` and configure
**Production**: Set environment variables in Azure Function App Configuration

Required global settings: `SENDGRID_API_KEY`, `DEFAULT_FROM_EMAIL`, `AzureWebJobsStorage`
Required per-site: `sites:{siteId}:to_email`, `sites:{siteId}:redirect_url`

## Dependencies

- **.NET 8.0** - Azure Functions v4 isolated worker model
- **Azure.Data.Tables** - Rate limiting storage
- **SendGrid** - Email delivery
- **Microsoft.Azure.Functions.Worker** - Function runtime

## Monitoring and Troubleshooting

Key log messages to watch for:
- `Contact form submitted successfully` - Normal operation
- `Rate limit exceeded` - Rate limiting triggered  
- `Honeypot triggered` - Bot/spam attempt
- `HMAC validation failed` - Invalid security signature
- `Unknown site` - Missing site configuration

Rate limiting issues often indicate Azure Storage connectivity problems. Email delivery failures are logged as warnings but don't block user flow.