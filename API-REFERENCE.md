# Contact Forms Admin API Reference

This document provides comprehensive CURL examples for all API endpoints in the Contact Forms Admin system.

**Base URL (Local):** `https://forms.redlettersolutions.io`  
**Base URL (Production):** `https://your-production-url.com`

**Authentication:** Most endpoints require an API key passed in the `X-API-Key` header.  
**Dev API Key:** `cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA`

---

## Table of Contents

1. [Submissions API](#submissions-api)
2. [Statistics API](#statistics-api)
3. [Sites API](#sites-api)
4. [Webhook Trigger API](#webhook-trigger-api)

---

## Submissions API

### 1. Get Submissions for a Site

Retrieve submissions for a specific site with pagination support.

**Endpoint:** `GET /api/submissions/{siteId}`

**Parameters:**
- `siteId` (path): Site identifier (e.g., `guitar_repair_of_tampa_bay`, `logos`, `test`)
- `limit` (query, optional): Maximum number of results (default: 100, max: 1000)
- `offset` (query, optional): Number of results to skip for pagination (default: 0)

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/submissions/guitar_repair_of_tampa_bay?limit=50&offset=0" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "siteId": "guitar_repair_of_tampa_bay",
  "totalCount": 150,
  "limit": 50,
  "offset": 0,
  "count": 50,
  "submissions": [
    {
      "id": 1,
      "siteId": "guitar_repair_of_tampa_bay",
      "name": "John Doe",
      "email": "john@example.com",
      "message": "Need a guitar setup",
      "clientIp": "192.168.1.1",
      "submittedAt": "2024-01-15T10:30:00Z",
      "metadata": {
        "phone": "+1234567890",
        "guitarType": "Electric"
      },
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

### 2. Get a Specific Submission

Retrieve a single submission by ID for a specific site.

**Endpoint:** `GET /api/submissions/{siteId}/{id}`

**Parameters:**
- `siteId` (path): Site identifier
- `id` (path): Submission ID

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/submissions/guitar_repair_of_tampa_bay/1" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "id": 1,
  "siteId": "guitar_repair_of_tampa_bay",
  "name": "John Doe",
  "email": "john@example.com",
  "message": "Need a guitar setup",
  "clientIp": "192.168.1.1",
  "submittedAt": "2024-01-15T10:30:00Z",
  "metadata": {
    "phone": "+1234567890",
    "guitarType": "Electric"
  },
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Error Response (404):**

```json
{
  "error": "Submission not found"
}
```

---

### 3. Get All Submissions (Across All Sites)

Retrieve submissions from all sites with optional filtering and pagination.

**Endpoint:** `GET /api/submissions`

**Parameters:**
- `limit` (query, optional): Maximum number of results (default: 100, max: 1000)
- `offset` (query, optional): Number of results to skip (default: 0)
- `siteId` (query, optional): Filter by specific site ID

**CURL Example (All Sites):**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/submissions?limit=100&offset=0" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**CURL Example (Filtered by Site):**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/submissions?siteId=logos&limit=50" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "success": true,
  "totalCount": 500,
  "limit": 100,
  "offset": 0,
  "count": 100,
  "siteId": null,
  "submissions": [
    {
      "id": 1,
      "siteId": "guitar_repair_of_tampa_bay",
      "name": "John Doe",
      "email": "john@example.com",
      "message": "Need a guitar setup",
      "clientIp": "192.168.1.1",
      "submittedAt": "2024-01-15T10:30:00Z",
      "metadata": {},
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

### 4. Get Submissions by Date Range

Retrieve submissions for a site within a specific date range.

**Endpoint:** `GET /api/submissions/{siteId}/by-date`

**Parameters:**
- `siteId` (path): Site identifier
- `startDate` (query, optional): Start date in ISO 8601 format (e.g., `2024-01-01T00:00:00Z`)
- `endDate` (query, optional): End date in ISO 8601 format
- `limit` (query, optional): Maximum number of results (default: 100, max: 1000)

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/submissions/guitar_repair_of_tampa_bay/by-date?startDate=2024-01-01T00:00:00Z&endDate=2024-01-31T23:59:59Z&limit=100" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "success": true,
  "siteId": "guitar_repair_of_tampa_bay",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-01-31T23:59:59Z",
  "count": 25,
  "submissions": [
    {
      "id": 1,
      "siteId": "guitar_repair_of_tampa_bay",
      "name": "John Doe",
      "email": "john@example.com",
      "message": "Need a guitar setup",
      "clientIp": "192.168.1.1",
      "submittedAt": "2024-01-15T10:30:00Z",
      "metadata": {},
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

### 5. Delete a Submission

Delete a specific submission by ID.

**Endpoint:** `DELETE /api/submissions/{id}`

**Parameters:**
- `id` (path): Submission ID to delete

**CURL Example:**

```bash
curl -X DELETE "https://forms.redlettersolutions.io/api/submissions/1" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example (Success):**

```json
{
  "success": true,
  "message": "Submission deleted successfully"
}
```

**Response Example (Not Found):**

```json
{
  "success": false,
  "error": "Submission not found"
}
```

---

### 6. Delete a Submission with Site Verification

Delete a submission by ID with additional site verification.

**Endpoint:** `DELETE /api/submissions/{siteId}/{id}`

**Parameters:**
- `siteId` (path): Site identifier
- `id` (path): Submission ID to delete

**CURL Example:**

```bash
curl -X DELETE "https://forms.redlettersolutions.io/api/submissions/guitar_repair_of_tampa_bay/1" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example (Success):**

```json
{
  "success": true,
  "message": "Submission deleted successfully"
}
```

---

## Statistics API

### 7. Get Submission Statistics

Retrieve submission statistics for a specific site including total count and counts for various time periods.

**Endpoint:** `GET /api/stats/{siteId}`

**Parameters:**
- `siteId` (path): Site identifier

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/stats/guitar_repair_of_tampa_bay" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "siteId": "guitar_repair_of_tampa_bay",
  "totalSubmissions": 1500,
  "last24Hours": 5,
  "last7Days": 42,
  "last30Days": 180
}
```

---

## Sites API

### 8. Get All Sites

Retrieve a list of all sites (complete site records, not just submission counts).

**Endpoint:** `GET /api/sites`

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/sites" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "success": true,
  "count": 3,
  "sites": [
    {
      "id": 1,
      "siteId": "guitar_repair_of_tampa_bay",
      "name": "Guitar Repair of Tampa Bay",
      "description": "Contact form for guitar repair services",
      "toEmail": "info@guitarrepairoftampabay.com",
      "fromEmail": "forms@redlettersolutions.io",
      "redirectUrl": "https://guitarrepairoftampabay.com/thank-you",
      "allowedOrigins": "https://guitarrepairoftampabay.com,https://www.guitarrepairoftampabay.com",
      "secret": "secret_key_here",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

### 9. Get a Specific Site by ID

Retrieve a single site by its numeric ID.

**Endpoint:** `GET /api/sites/{id}`

**Parameters:**
- `id` (path): Numeric site ID

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/sites/1" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "success": true,
  "site": {
    "id": 1,
    "siteId": "guitar_repair_of_tampa_bay",
    "name": "Guitar Repair of Tampa Bay",
    "description": "Contact form for guitar repair services",
    "toEmail": "info@guitarrepairoftampabay.com",
    "fromEmail": "forms@redlettersolutions.io",
    "redirectUrl": "https://guitarrepairoftampabay.com/thank-you",
    "allowedOrigins": "https://guitarrepairoftampabay.com,https://www.guitarrepairoftampabay.com",
    "secret": "secret_key_here",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

**Error Response (404):**

```json
{
  "success": false,
  "error": "Site not found"
}
```

---

### 10. Get a Site by Site ID (String Identifier)

Retrieve a single site by its string identifier (siteId).

**Endpoint:** `GET /api/sites/by-site-id/{siteId}`

**Parameters:**
- `siteId` (path): Site string identifier (e.g., `guitar_repair_of_tampa_bay`)

**CURL Example:**

```bash
curl -X GET "https://forms.redlettersolutions.io/api/sites/by-site-id/guitar_repair_of_tampa_bay" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example:**

```json
{
  "success": true,
  "site": {
    "id": 1,
    "siteId": "guitar_repair_of_tampa_bay",
    "name": "Guitar Repair of Tampa Bay",
    "description": "Contact form for guitar repair services",
    "toEmail": "info@guitarrepairoftampabay.com",
    "fromEmail": "forms@redlettersolutions.io",
    "redirectUrl": "https://guitarrepairoftampabay.com/thank-you",
    "allowedOrigins": "https://guitarrepairoftampabay.com,https://www.guitarrepairoftampabay.com",
    "secret": "secret_key_here",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

---

### 11. Create a New Site

Create a new site configuration.

**Endpoint:** `POST /api/sites`

**Request Body:**
```json
{
  "siteId": "new_site",
  "name": "New Site Name",
  "description": "Optional description",
  "toEmail": "contact@example.com",
  "fromEmail": "forms@redlettersolutions.io",
  "redirectUrl": "https://example.com/thank-you",
  "allowedOrigins": "https://example.com,https://www.example.com",
  "secret": "optional_secret_key",
  "isActive": true
}
```

**Required Fields:**
- `siteId`: Unique site identifier (alphanumeric, underscores allowed)
- `name`: Display name for the site
- `toEmail`: Email address where form submissions will be sent
- `fromEmail`: Email address used as sender

**Optional Fields:**
- `description`: Site description
- `redirectUrl`: URL to redirect after successful submission
- `allowedOrigins`: Comma-separated list of allowed CORS origins
- `secret`: Secret key for additional validation
- `isActive`: Whether the site is active (default: true)

**CURL Example:**

```bash
curl -X POST "https://forms.redlettersolutions.io/api/sites" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA" \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "new_site",
    "name": "New Site Name",
    "description": "Optional description",
    "toEmail": "contact@example.com",
    "fromEmail": "forms@redlettersolutions.io",
    "redirectUrl": "https://example.com/thank-you",
    "allowedOrigins": "https://example.com,https://www.example.com",
    "isActive": true
  }'
```

**Response Example (Success - 201 Created):**

```json
{
  "success": true,
  "message": "Site created successfully",
  "site": {
    "id": 4,
    "siteId": "new_site",
    "name": "New Site Name",
    "description": "Optional description",
    "toEmail": "contact@example.com",
    "fromEmail": "forms@redlettersolutions.io",
    "redirectUrl": "https://example.com/thank-you",
    "allowedOrigins": "https://example.com,https://www.example.com",
    "secret": null,
    "isActive": true,
    "createdAt": "2024-01-20T15:45:00Z",
    "updatedAt": "2024-01-20T15:45:00Z"
  }
}
```

**Error Response (400 - Missing Required Field):**

```json
{
  "success": false,
  "error": "toEmail is required"
}
```

**Error Response (409 - Duplicate siteId):**

```json
{
  "success": false,
  "error": "A site with this siteId already exists"
}
```

---

### 12. Update a Site

Update an existing site configuration. Only provided fields will be updated.

**Endpoint:** `PUT /api/sites/{id}`

**Parameters:**
- `id` (path): Numeric site ID

**Request Body (all fields optional):**
```json
{
  "name": "Updated Site Name",
  "description": "Updated description",
  "toEmail": "newemail@example.com",
  "fromEmail": "newforms@redlettersolutions.io",
  "redirectUrl": "https://example.com/new-thank-you",
  "allowedOrigins": "https://example.com",
  "secret": "new_secret_key",
  "isActive": false
}
```

**CURL Example:**

```bash
curl -X PUT "https://forms.redlettersolutions.io/api/sites/1" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Site Name",
    "toEmail": "newemail@example.com",
    "isActive": true
  }'
```

**Response Example (Success):**

```json
{
  "success": true,
  "message": "Site updated successfully",
  "site": {
    "id": 1,
    "siteId": "guitar_repair_of_tampa_bay",
    "name": "Updated Site Name",
    "description": "Contact form for guitar repair services",
    "toEmail": "newemail@example.com",
    "fromEmail": "forms@redlettersolutions.io",
    "redirectUrl": "https://guitarrepairoftampabay.com/thank-you",
    "allowedOrigins": "https://guitarrepairoftampabay.com",
    "secret": "secret_key_here",
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-20T16:00:00Z"
  }
}
```

**Error Response (404):**

```json
{
  "success": false,
  "error": "Site not found"
}
```

---

### 13. Delete a Site

Delete a site configuration. **Note:** Sites with existing submissions cannot be deleted.

**Endpoint:** `DELETE /api/sites/{id}`

**Parameters:**
- `id` (path): Numeric site ID

**CURL Example:**

```bash
curl -X DELETE "https://forms.redlettersolutions.io/api/sites/4" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example (Success):**

```json
{
  "success": true,
  "message": "Site deleted successfully"
}
```

**Error Response (400 - Has Submissions):**

```json
{
  "success": false,
  "error": "Cannot delete site with existing submissions. Consider deactivating instead."
}
```

**Error Response (404):**

```json
{
  "success": false,
  "error": "Site not found"
}
```

---

### 14. Toggle Site Active Status

Toggle a site's active/inactive status.

**Endpoint:** `PATCH /api/sites/{id}/toggle-active`

**Parameters:**
- `id` (path): Numeric site ID

**CURL Example:**

```bash
curl -X PATCH "https://forms.redlettersolutions.io/api/sites/1/toggle-active" \
  -H "X-API-Key: cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
```

**Response Example (Success):**

```json
{
  "success": true,
  "message": "Site deactivated successfully",
  "site": {
    "id": 1,
    "siteId": "guitar_repair_of_tampa_bay",
    "name": "Guitar Repair of Tampa Bay",
    "isActive": false,
    "updatedAt": "2024-01-20T16:15:00Z"
  }
}
```

---

## Webhook Trigger API

### 15. Trigger Webhooks (Internal Use)

**⚠️ Note:** This endpoint is for internal use by the Azure Functions app and does **NOT** require an API key. It should not be exposed publicly in production.

**Endpoint:** `POST /api/trigger-webhook`

**Request Body:**
```json
{
  "siteId": "guitar_repair_of_tampa_bay",
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "message": "Need a guitar setup",
    "submittedAt": "2024-01-15T10:30:00Z"
  }
}
```

**CURL Example:**

```bash
curl -X POST "https://forms.redlettersolutions.io/api/trigger-webhook" \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "guitar_repair_of_tampa_bay",
    "data": {
      "id": 1,
      "name": "John Doe",
      "email": "john@example.com",
      "message": "Need a guitar setup",
      "submittedAt": "2024-01-15T10:30:00Z"
    }
  }'
```

**Response Example (Success):**

```json
{
  "success": true,
  "message": "Webhooks triggered"
}
```

**Response Example (Error):**

```json
{
  "error": "siteId is required"
}
```

---

## Testing with PowerShell

If you prefer using PowerShell instead of CURL, here are equivalent examples:

### Get Submissions

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
}
Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/submissions/guitar_repair_of_tampa_bay?limit=50" -Method Get -Headers $headers
```

### Get Statistics

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
}
Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/stats/guitar_repair_of_tampa_bay" -Method Get -Headers $headers
```

### Get All Sites

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
}
Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/sites" -Method Get -Headers $headers
```

### Create a Site

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
    "Content-Type" = "application/json"
}
$body = @{
    siteId = "new_site"
    name = "New Site Name"
    toEmail = "contact@example.com"
    fromEmail = "forms@redlettersolutions.io"
    isActive = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/sites" -Method Post -Headers $headers -Body $body
```

### Update a Site

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
    "Content-Type" = "application/json"
}
$body = @{
    name = "Updated Site Name"
    toEmail = "newemail@example.com"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/sites/1" -Method Put -Headers $headers -Body $body
```

### Delete a Site

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
}
Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/sites/4" -Method Delete -Headers $headers
```

### Toggle Site Active Status

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
}
Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/sites/1/toggle-active" -Method Patch -Headers $headers
```

### Delete Submission

```powershell
$headers = @{
    "X-API-Key" = "cfadmin_xpfWC5wGq9VvlfowvdcoTL8k2KXgKERS666WqrnrSjA"
}
Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/submissions/1" -Method Delete -Headers $headers
```

### Trigger Webhook

```powershell
$body = @{
    siteId = "guitar_repair_of_tampa_bay"
    data = @{
        id = 1
        name = "John Doe"
        email = "john@example.com"
        message = "Need a guitar setup"
        submittedAt = "2024-01-15T10:30:00Z"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://forms.redlettersolutions.io/api/trigger-webhook" -Method Post -Body $body -ContentType "application/json"
```

---

## Error Responses

All endpoints may return the following error responses:

### 401 Unauthorized
```json
{
  "error": "Unauthorized"
}
```
**Cause:** Missing or invalid API key

### 404 Not Found
```json
{
  "success": false,
  "error": "Submission not found"
}
```
**Cause:** Requested resource doesn't exist

### 500 Internal Server Error
```json
{
  "success": false,
  "error": "Failed to retrieve submissions",
  "details": "Database connection error"
}
```
**Cause:** Server-side error

---

## Notes

1. **Authentication:** All API endpoints except `/api/trigger-webhook` require the `X-API-Key` header
2. **Rate Limiting:** Consider implementing rate limiting in production
3. **HTTPS:** Always use HTTPS in production environments
4. **Pagination:** Use `limit` and `offset` parameters for large datasets (max limit: 1000)
5. **Date Formats:** All dates should be in ISO 8601 format with UTC timezone
6. **Site IDs:** Available site IDs: `guitar_repair_of_tampa_bay`, `logos`, `test`
7. **Site Management:** Sites with existing submissions cannot be deleted - deactivate them instead
8. **Partial Updates:** When updating sites (PUT), only provided fields will be updated
9. **CORS Origins:** Multiple origins should be comma-separated (e.g., `https://example.com,https://www.example.com`)

---

## Additional Resources

- [Webhook Setup Guide](./WEBHOOK-SETUP.md)
- [Azure Functions Documentation](./API.md)
- [README](./README.md)
