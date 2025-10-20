# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Local Development
```bash
# Setup local environment
dotnet restore
cp local.settings.json.example local.settings.json
# Edit local.settings.json with your SendGrid API key, PostgreSQL connection string, and site configurations

# Apply database migrations (optional - will auto-apply on first submission)
dotnet ef database update

# Run locally (requires Azure Functions Core Tools v4)
func start
# Function available at: http://localhost:7071/v1/contact/{siteId}
```

### Build and Deploy
```bash
# Build project
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
- Check Azure Storage for rate limiting table entries and contact submissions
- Initialize/seed database using: `curl -X POST "http://localhost:7071/v1/database/init?seed=true&code=your-function-key"`

## Architecture Overview

This is a **multi-tenant Azure Functions service** that handles contact form submissions for multiple websites through a single endpoint. The core architecture consists of:

### Single Function Entry Point
- **Route**: `POST /v1/contact/{siteId}` 
- **File**: `Contact.cs` - Contains all business logic in the `Contact` class
- **Multi-tenant**: Each `{siteId}` represents a different client website with isolated configuration
- **Architecture**: Uses ASP.NET Core integration model for Azure Functions v4

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
3. Extract core fields (name, email, message) and metadata fields (any additional fields)
4. Send email via SendGrid with both HTML and text formats including all metadata
5. Store submission in database (failures logged but don't block user flow)
6. Return **303 See Other** redirect to keep users on their domain

### Dynamic Metadata Support
The service automatically detects and includes any form fields beyond the core set:
- **Core fields**: `name`, `email`, `message`, `_hp`, `_ts`, `_sig`
- **Metadata fields**: Any other field (e.g., `phone_number`, `company`, `budget_range`)
- **Email formatting**: Metadata appears in a formatted table in HTML emails and structured list in text emails
- **Field name formatting**: `phone_number` becomes "Phone Number", `budget-range` becomes "Budget Range"

### Database Storage System

All contact form submissions are automatically stored in **PostgreSQL** for record-keeping and analysis:

**Database Structure**:
- **Table Name**: `contact_submissions`
- **Primary Key**: `id` (auto-incrementing BIGINT)
- **Tenant Isolation**: `site_id` column with index for efficient per-site queries

**Schema**:
```sql
CREATE TABLE contact_submissions (
    id BIGSERIAL PRIMARY KEY,
    site_id VARCHAR(100) NOT NULL,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    client_ip VARCHAR(50),
    submitted_at TIMESTAMP NOT NULL,
    metadata_json JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX ix_contact_submissions_site_id ON contact_submissions(site_id);
CREATE INDEX ix_contact_submissions_submitted_at ON contact_submissions(submitted_at);
CREATE INDEX ix_contact_submissions_site_id_submitted_at ON contact_submissions(site_id, submitted_at);
```

**Stored Fields**:
- **Core Fields**: site_id, name, email, message, client_ip, submitted_at
- **Dynamic Metadata**: All additional form fields stored as JSONB in `metadata_json` column
- **Automatic Timestamps**: created_at auto-populated on insert

**Multi-Tenant Isolation**:
- Each site's submissions filtered by `site_id`
- Optimized index on `site_id` for fast queries
- Composite index on `(site_id, submitted_at)` for time-range queries
- Query: `SELECT * FROM contact_submissions WHERE site_id = 'world1' ORDER BY submitted_at DESC`

**Dynamic Schema Support (JSONB)**:
- Handles any form structure without schema changes
- Additional fields (phone, company, budget, etc.) stored as JSONB
- Native PostgreSQL JSON querying support
- `ContactSubmission.GetMetadata()` deserializes metadata back to dictionary

**Database Operations** (via `SubmissionRepository` with Entity Framework Core):
- `SaveSubmissionAsync()` - Store new submission
- `GetSubmissionsBySiteAsync(siteId, maxResults)` - Retrieve submissions for a site
- `GetSubmissionAsync(id)` - Get specific submission by ID
- `GetSubmissionAsync(siteId, id)` - Get submission with tenant isolation
- `DeleteSubmissionAsync(id)` - Delete submission
- `DeleteSubmissionAsync(siteId, id)` - Delete with tenant isolation
- `GetSubmissionCountAsync(siteId)` - Count submissions per site
- `GetSubmissionsByDateRangeAsync(siteId, start, end)` - Query by date range

**Database Initialization & Migrations**:
- **Route**: `GET/POST /v1/database/init`
- **Authorization**: Function-level (requires function key)
- **Initialize only**: `GET /v1/database/init` (runs EF Core migrations)
- **Initialize + seed**: `POST /v1/database/init?seed=true` (adds 4 sample submissions)
- Migrations applied automatically on first request or via init endpoint
- Migration files located in `Data/Migrations/`

**Error Handling**:
- Database failures are logged as warnings
- Submissions are stored AFTER successful email delivery
- Database errors don't block user flow (email + redirect still work)
- This ensures maximum reliability for end users

**File Structure**:
- `Models/ContactSubmission.cs` - EF Core entity with dynamic metadata support
- `Data/ApplicationDbContext.cs` - EF Core DbContext with PostgreSQL configuration
- `Data/Migrations/` - EF Core migration files
- `Services/SubmissionRepository.cs` - Database operations repository
- `DatabaseInit.cs` - Initialization and seeding Azure Function
- `Program.cs` - Registers DbContext with dependency injection
- `Contact.cs` - Main function integrates repository for storage

### Key Implementation Details

**Rate Limiting**: Uses Azure Table Storage with PartitionKey=`siteId`, RowKey=`{ip}:{yyyyMMddHHmm}`. Creates entity if not exists, returns 429 if already exists.

**HMAC Security**: String to sign format: `{siteId}|{timestamp}|{email}|{name}|{first_200_chars_of_message}|{metadata_fields}`. Metadata fields are sorted by key and formatted as `key:value|key:value`. Uses HMAC-SHA256 with constant-time comparison.

**Email Format**: 
- Subject: `New contact ({siteId}) from {name}`
- Dual format: plain text and HTML with proper escaping
- Includes core fields and all metadata fields with formatted names
- HTML version uses a styled table for metadata display

**Error Handling**: All errors return user-friendly messages while detailed errors are logged. SendGrid failures are logged but don't block the user flow.

## Adding New Sites

Adding a new client requires **zero code changes**:
1. Add configuration variables in Azure Portal with the `sites:{newSiteId}:*` pattern
2. Update client's form action URL to include the new `{siteId}`
3. Test submission and email delivery

## Configuration Management

**Local**: Copy `local.settings.json.example` to `local.settings.json` and configure
**Production**: Set environment variables in Azure Function App Configuration

Required global settings:
- `SENDGRID_API_KEY` - SendGrid API key for email delivery
- `DEFAULT_FROM_EMAIL` - Default from email address
- `AzureWebJobsStorage` - Azure Storage connection string for rate limiting
- `PostgresConnectionString` - PostgreSQL connection string for contact submissions

Required per-site:
- `sites:{siteId}:to_email` - Email recipient for this site
- `sites:{siteId}:redirect_url` - Redirect URL after successful submission

## Dependencies

- **.NET 8.0** - Azure Functions v4 isolated worker model
- **Npgsql.EntityFrameworkCore.PostgreSQL** - PostgreSQL provider for Entity Framework Core
- **Microsoft.EntityFrameworkCore.Design** - EF Core migration tools
- **Azure.Data.Tables** - Rate limiting storage (Azure Table Storage)
- **SendGrid** - Email delivery
- **Microsoft.Azure.Functions.Worker** - Function runtime

## Monitoring and Troubleshooting

Key log messages to watch for:
- `Contact form submitted successfully` - Normal operation
- `Email sent successfully` - Email delivered via SendGrid
- `Contact submission saved successfully for site {SiteId}, ID: {Id}` - Submission stored in PostgreSQL
- `Database migrations applied successfully` - EF Core migrations completed
- `Rate limit exceeded` - Rate limiting triggered
- `Honeypot triggered` - Bot/spam attempt
- `HMAC validation failed` - Invalid security signature
- `Unknown site` - Missing site configuration
- `Failed to save submission to database` - Database storage failed (doesn't block user)
- `Failed to apply database migrations` - Migration error

Rate limiting issues often indicate Azure Storage connectivity problems. Email delivery failures and database storage failures are logged as warnings but don't block user flow, ensuring maximum reliability for end users. Database migration failures will prevent the application from starting properly.

## REST API Endpoints

The ContactFormsAdmin application provides a comprehensive REST API for managing sites, webhooks, and submissions. All API endpoints are located in the `ContactFormsAdmin/Controllers/Api/` directory.

### Sites API (`/api/sites`)

**Location**: `ContactFormsAdmin/Controllers/Api/SitesApiController.cs`

Manage site configurations for the multi-tenant contact form service.

- `GET /api/sites` - List all sites
  - Query params: `includeInactive` (bool, default: false)
  - Returns: Array of site objects with configuration details

- `GET /api/sites/{id}` - Get site by database ID
  - Returns: Single site object

- `GET /api/sites/by-slug/{siteId}` - Get site by SiteId (slug)
  - Returns: Single site object

- `POST /api/sites` - Create new site
  - Body: `{ siteId, name, description?, toEmail, fromEmail, redirectUrl?, allowedOrigins?, secret?, isActive? }`
  - Returns: Created site object (201)
  - Validation: SiteId must be unique

- `PUT /api/sites/{id}` - Update existing site
  - Body: `{ name?, description?, toEmail?, fromEmail?, redirectUrl?, allowedOrigins?, secret?, isActive? }`
  - Returns: Updated site object

- `DELETE /api/sites/{id}` - Delete site
  - Returns: Success message

- `PATCH /api/sites/{id}/toggle-active` - Toggle site active status
  - Returns: New active status

**Response Format**:
```json
{
  "success": true,
  "site": {
    "id": 1,
    "siteId": "example-site",
    "name": "Example Site",
    "description": "My example website",
    "toEmail": "admin@example.com",
    "fromEmail": "noreply@example.com",
    "redirectUrl": "/thank-you",
    "allowedOrigins": ["https://example.com"],
    "hasSecret": true,
    "isActive": true,
    "createdAt": "2025-01-15T10:30:00Z",
    "updatedAt": "2025-01-15T10:30:00Z"
  }
}
```

### Webhooks API (`/api/webhooks`)

**Location**: `ContactFormsAdmin/Controllers/Api/WebhooksApiController.cs`

Manage webhooks that trigger on contact form submissions.

- `GET /api/webhooks` - List all webhooks
  - Query params: `siteId` (string, optional), `includeInactive` (bool, default: false)
  - Returns: Array of webhook objects

- `GET /api/webhooks/{id}` - Get webhook by ID
  - Returns: Single webhook object

- `POST /api/webhooks` - Create new webhook
  - Body: `{ siteId, url, description?, events?, secret?, isActive? }`
  - Returns: Created webhook object (201)
  - Validation: URL must be valid, site must exist

- `PUT /api/webhooks/{id}` - Update existing webhook
  - Body: `{ url?, description?, events?, secret?, isActive? }`
  - Returns: Updated webhook object

- `DELETE /api/webhooks/{id}` - Delete webhook
  - Returns: Success message

- `PATCH /api/webhooks/{id}/toggle-active` - Toggle webhook active status
  - Returns: New active status

- `POST /api/webhooks/{id}/test` - Test webhook with sample payload
  - Returns: Test result with success/error details

**Response Format**:
```json
{
  "success": true,
  "webhook": {
    "id": 1,
    "siteId": "example-site",
    "url": "https://example.com/webhook",
    "description": "Production webhook",
    "events": "contact.submitted",
    "hasSecret": true,
    "isActive": true,
    "createdAt": "2025-01-15T10:30:00Z",
    "lastTriggeredAt": "2025-01-15T12:45:00Z",
    "lastSuccess": true,
    "lastError": null
  }
}
```

### Submissions API (`/api/submissions`)

**Location**: `ContactFormsAdmin/Controllers/ApiController.cs`

Query and manage contact form submissions.

- `GET /api/submissions` - List all submissions (across all sites)
  - Query params: `siteId` (string, optional), `limit` (int, max 1000), `offset` (int)
  - Returns: Paginated array of submissions

- `GET /api/submissions/{siteId}` - List submissions for a specific site
  - Query params: `limit` (int, max 1000), `offset` (int)
  - Returns: Paginated array of submissions

- `GET /api/submissions/{siteId}/{id}` - Get specific submission
  - Returns: Single submission object

- `GET /api/submissions/{siteId}/by-date` - Query submissions by date range
  - Query params: `startDate` (DateTime?), `endDate` (DateTime?), `limit` (int)
  - Returns: Array of submissions within date range

- `POST /api/submissions` - Create new submission
  - Body: `{ siteId, name, email, message, clientIp?, submittedAt?, metadata? }`
  - Returns: Created submission object (201)
  - Validation: siteId must exist, name/email/message required
  - Use case: Import submissions from external sources (WordPress, other forms)

- `DELETE /api/submissions/{id}` - Delete submission by ID
  - Returns: Success message

- `DELETE /api/submissions/{siteId}/{id}` - Delete submission with site verification
  - Returns: Success message

- `GET /api/stats/{siteId}` - Get submission statistics
  - Returns: Stats object with total, last 24h, 7d, 30d counts

**Response Format**:
```json
{
  "success": true,
  "totalCount": 150,
  "limit": 100,
  "offset": 0,
  "count": 100,
  "siteId": "example-site",
  "submissions": [
    {
      "id": 1,
      "siteId": "example-site",
      "name": "John Doe",
      "email": "john@example.com",
      "message": "Hello, I have a question...",
      "clientIp": "192.168.1.1",
      "submittedAt": "2025-01-15T10:30:00Z",
      "metadata": {
        "phone": "555-1234",
        "company": "Acme Corp"
      },
      "createdAt": "2025-01-15T10:30:00Z"
    }
  ]
}
```

### API Authentication

Currently, the API endpoints do not enforce authentication. For production use, consider:

1. Adding API key authentication via `ApiKeyMiddleware` (already implemented in ContactFormsAdmin)
2. Using JWT tokens for user-specific access
3. Implementing role-based access control (RBAC)
4. Adding rate limiting to prevent abuse

**API Key Setup** (if using ApiKeyMiddleware):
- Create API keys via the admin dashboard
- Include `X-API-Key` header in all requests
- Keys are hashed and stored securely in the database

### Error Response Format

All API endpoints return errors in a consistent format:

```json
{
  "success": false,
  "error": "Error message here",
  "details": "Additional error details (optional)"
}
```

Common HTTP status codes:
- `200 OK` - Success
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request parameters
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource already exists (e.g., duplicate SiteId)
- `500 Internal Server Error` - Server error

### Testing API Endpoints

**Using cURL**:
```bash
# List all sites
curl http://localhost:5282/api/sites

# Create a new site
curl -X POST http://localhost:5282/api/sites \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "test-site",
    "name": "Test Site",
    "toEmail": "admin@test.com",
    "fromEmail": "noreply@test.com",
    "redirectUrl": "/thanks",
    "allowedOrigins": ["https://test.com"]
  }'

# Create a webhook
curl -X POST http://localhost:5282/api/webhooks \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "test-site",
    "url": "https://webhook.site/your-unique-url",
    "description": "Test webhook"
  }'

# Create a submission (e.g., from WordPress)
curl -X POST http://localhost:5282/api/submissions \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "test-site",
    "name": "John Doe",
    "email": "john@example.com",
    "message": "Hello from WordPress!",
    "metadata": {
      "phone": "555-1234",
      "source": "WordPress Contact Form 7"
    }
  }'

# List submissions for a site
curl http://localhost:5282/api/submissions/test-site?limit=10

# Delete a submission
curl -X DELETE http://localhost:5282/api/submissions/123
```

**Using JavaScript/Fetch**:
```javascript
// Create a new site
const response = await fetch('http://localhost:5282/api/sites', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    siteId: 'my-site',
    name: 'My Site',
    toEmail: 'admin@mysite.com',
    fromEmail: 'noreply@mysite.com'
  })
});
const data = await response.json();
console.log(data.site);

// Create a submission from external source
const submissionResponse = await fetch('http://localhost:5282/api/submissions', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    siteId: 'my-site',
    name: 'Jane Smith',
    email: 'jane@example.com',
    message: 'I would like more information',
    clientIp: '192.168.1.100',
    metadata: {
      phone: '555-9876',
      company: 'Tech Solutions',
      budget_range: '$10k-$50k'
    }
  })
});
const submissionData = await submissionResponse.json();
console.log(submissionData.submission);
```

### WordPress Integration Example

To integrate with WordPress and track submissions in this app:

**PHP Code for WordPress** (add to functions.php or a custom plugin):
```php
// Hook into Contact Form 7 submission
add_action('wpcf7_mail_sent', 'send_to_contact_forms_api');

function send_to_contact_forms_api($contact_form) {
    $submission = WPCF7_Submission::get_instance();
    
    if ($submission) {
        $posted_data = $submission->get_posted_data();
        
        // Prepare data for API
        $api_data = array(
            'siteId' => 'wordpress-site-1', // Your site ID
            'name' => $posted_data['your-name'] ?? '',
            'email' => $posted_data['your-email'] ?? '',
            'message' => $posted_data['your-message'] ?? '',
            'clientIp' => $_SERVER['REMOTE_ADDR'],
            'metadata' => array(
                'phone' => $posted_data['your-phone'] ?? '',
                'source' => 'WordPress Contact Form 7',
                'form_id' => $contact_form->id(),
            )
        );
        
        // Send to API
        wp_remote_post('https://your-api-domain.com/api/submissions', array(
            'headers' => array('Content-Type' => 'application/json'),
            'body' => json_encode($api_data),
            'timeout' => 15
        ));
    }
}
```

This allows you to:
1. Keep using your existing WordPress forms
2. Track all submissions in a centralized database
3. View all client submissions in one admin dashboard
4. Maintain historical data even if you migrate away from WordPress
5. Support multiple WordPress sites with different `siteId` values
```