# Webhook Integration Setup Guide

## What Changed

The Azure Functions Contact handler now automatically notifies the admin app to trigger configured webhooks when a contact form is submitted. Previously, the admin webhook system existed but was never called from actual form submissions.

### Code Changes Made
- ✅ Added `NotifyAdminAppAsync()` method to `Contact.cs`
- ✅ Function now POSTs to admin app `/api/trigger-webhook` after saving submissions
- ✅ Fire-and-forget approach so user responses aren't delayed
- ✅ Admin app endpoint is already secured (exempt from API key auth for internal use)

---

## What You Need To Do

### 1. Configure Local Development

Add the admin app URL to your **local.settings.json**:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PostgresConnectionString": "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;",
    "SENDGRID_API_KEY": "your-sendgrid-key",
    "ADMIN_APP_URL": "http://localhost:5282"
  }
}
```

**Key:** `ADMIN_APP_URL` (or `ADMIN_URL` as fallback)  
**Local Value:** `http://localhost:5282`  
**Production Value:** Your deployed admin app URL (e.g., `https://your-admin-app.railway.app`)

### 2. Configure Azure (Production)

Add the same environment variable in Azure Portal:

1. Go to your Azure Functions App
2. Navigate to **Configuration** → **Application settings**
3. Click **+ New application setting**
4. Name: `ADMIN_APP_URL`
5. Value: `https://your-admin-app.railway.app` (or wherever your admin app is deployed)
6. Click **OK** and **Save**

---

## How It Works

```
┌─────────────────────────────────────────────────────────────────┐
│                     Contact Form Submission                      │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Functions (Contact.cs)                   │
│  1. Validate form data                                           │
│  2. Send email via SendGrid                                      │
│  3. Save to PostgreSQL database                                  │
│  4. POST to Admin App (fire-and-forget) ──────────┐            │
└───────────────────────────────────────────────────┼─────────────┘
                                                     │
                           ┌─────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│        Admin App (WebhookTriggerController.cs)                   │
│  POST /api/trigger-webhook                                       │
│  Receives: { siteId, data }                                      │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│              WebhookService.TriggerWebhooksAsync()               │
│  1. Query database for active webhooks for siteId               │
│  2. POST to each webhook URL with form data                     │
│  3. Update webhook last_triggered_at, last_success, last_error  │
└─────────────────────────────────────────────────────────────────┘
                           │
                           ▼
                  ┌────────┴────────┐
                  │                 │
                  ▼                 ▼
         [Webhook URL 1]    [Webhook URL 2]
         (Your Zapier,      (Your n8n,
          Make.com, etc.)    custom API, etc.)
```

---

## Testing

### Local Testing

1. **Start Admin App:**
   ```powershell
   cd ContactFormsAdmin
   dotnet run
   # Should start at http://localhost:5282
   ```

2. **Start Azure Functions (requires Azure Functions Core Tools):**
   ```powershell
   cd ..
   func start
   # or: func host start
   # Should start at http://localhost:7071
   ```

3. **Create a test webhook in admin portal:**
   - Go to http://localhost:5282/Sites
   - Verify your sites are seeded (guitar_repair_of_tampa_bay, logos, test)
   - Go to http://localhost:5282/Webhooks
   - Click "Create New Webhook"
   - Fill in:
     - Site ID: `guitar_repair_of_tampa_bay`
     - URL: `https://webhook.site/your-unique-url` (or your test endpoint)
     - Events: `form.submitted`
     - Active: ✅ checked

4. **Submit a test form:**
   ```powershell
   $body = @{
       name = "Test User"
       email = "test@example.com"
       message = "Testing webhook integration"
   } | ConvertTo-Json

   Invoke-RestMethod -Method Post `
       -Uri "http://localhost:7071/api/v1/contact/guitar_repair_of_tampa_bay" `
       -Body $body `
       -ContentType "application/json"
   ```

5. **Verify:**
   - ✅ Functions logs show: `"Admin webhook trigger successful for site guitar_repair_of_tampa_bay"`
   - ✅ Admin logs show: `"Webhook {id} for site guitar_repair_of_tampa_bay triggered successfully"`
   - ✅ Your webhook endpoint (webhook.site) received the POST request
   - ✅ In admin portal, go to Webhooks → your webhook shows updated "Last Triggered" time

### Quick Test Without Functions Host

If you don't have Azure Functions Core Tools installed, you can test the admin webhook system directly:

```powershell
# Directly call the admin webhook trigger endpoint
$payload = @{
    siteId = "guitar_repair_of_tampa_bay"
    data = @{
        name = "Direct Test"
        email = "test@example.com"
        message = "Testing admin webhook trigger"
    }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Method Post `
    -Uri "http://localhost:5282/api/trigger-webhook" `
    -Body $payload `
    -ContentType "application/json"
```

This simulates what the Function does and should trigger all active webhooks for that site.

---

## Production Deployment Checklist

- [ ] **Azure Functions Configuration:**
  - [ ] Set `ADMIN_APP_URL` to your deployed admin app URL
  - [ ] Verify `PostgresConnectionString` points to production database
  - [ ] Verify `SENDGRID_API_KEY` is set

- [ ] **Admin App:**
  - [ ] Deploy admin app (Railway, Azure App Service, etc.)
  - [ ] Note the deployed URL (e.g., https://your-admin.railway.app)
  - [ ] Configure admin app database connection

- [ ] **Webhooks Configuration:**
  - [ ] Create webhooks in admin portal for each site
  - [ ] Use production webhook URLs (not webhook.site)
  - [ ] Test each webhook using "Test" button in admin portal

- [ ] **End-to-End Test:**
  - [ ] Submit a real form from your website
  - [ ] Verify email is sent
  - [ ] Verify submission is saved in database
  - [ ] Verify webhooks are triggered
  - [ ] Check webhook endpoint received the data

---

## Troubleshooting

### Webhook Not Triggered

**Symptom:** Form submission works, email sent, but webhook doesn't fire.

**Check:**
1. **ADMIN_APP_URL is set:** Functions logs should show attempt to notify admin
   - Look for: `"Admin webhook trigger successful"` or `"Failed to notify admin app"`
2. **Admin app is reachable:** From Functions host, can it reach the admin URL?
   - Test: `curl http://localhost:5282/api/trigger-webhook -v`
3. **Webhook is active:** In admin portal, verify webhook has Active ✅ checked
4. **Site ID matches:** Webhook `site_id` must match the form submission site ID

### Function Can't Reach Admin App

**Symptom:** Functions logs show: `"Failed to notify admin app"`

**Solutions:**
- **Local:** Admin app must be running before submitting forms
- **Production:** Verify `ADMIN_APP_URL` is correct and admin app is deployed
- **Network:** If admin app is internal-only, ensure Functions can reach it
- **Timeout:** Default is 5 seconds; increase if admin app is slow to respond

### Admin App Receives Request But Doesn't Trigger Webhooks

**Symptom:** Admin logs show webhook controller called, but no webhooks fire.

**Check:**
1. **Database connection:** Admin app must connect to same database as Functions
2. **Webhooks exist:** Query database: `SELECT * FROM webhooks WHERE site_id = 'your-site' AND is_active = true`
3. **Admin logs:** Look for `"Webhook {id} for site {siteId} triggered successfully"` or error messages

### Webhook URL Returns Error

**Symptom:** Webhook triggers but target URL returns 4xx/5xx error.

**Check:**
1. **URL is correct:** Test manually with curl/Postman
2. **Authentication:** Some webhook endpoints require headers/secrets
3. **Payload format:** Check webhook expects JSON in the format we send
4. **Admin portal:** Check webhook's "Last Error" field for details

---

## Configuration Reference

### Environment Variables

| Variable | Required | Local Value | Production Value | Notes |
|----------|----------|-------------|------------------|-------|
| `ADMIN_APP_URL` | Yes | `http://localhost:5282` | Your deployed admin URL | Primary config key |
| `ADMIN_URL` | No | `http://localhost:5282` | Your deployed admin URL | Fallback if ADMIN_APP_URL not set |
| `PostgresConnectionString` | Yes | Railway connection string | Same as admin app | Must be same database |
| `SENDGRID_API_KEY` | Yes | Your SendGrid key | Same | For email sending |
| `AzureWebJobsStorage` | Yes | `UseDevelopmentStorage=true` | Azure Storage connection | Functions runtime |

### Database Tables

**sites** - Site configurations (you've already seeded these)
- `site_id` - Used in API URLs like `/api/v1/contact/{siteId}`
- `is_active` - Must be true for webhooks to trigger

**webhooks** - Webhook configurations
- `site_id` - Foreign key to sites
- `url` - Target webhook endpoint
- `is_active` - Must be true to trigger
- `last_triggered_at` - Updated each time webhook fires
- `last_success` - Boolean indicating last result
- `last_error` - Error message if failed

**contact_submissions** - Form submissions
- Saved by Functions before triggering webhooks

---

## Next Steps

1. ✅ **Code is ready** - Webhook integration is implemented
2. 🔄 **Configure `ADMIN_APP_URL`** - Add to local.settings.json and Azure
3. 🔄 **Test locally** - Submit a form and watch webhooks fire
4. 🔄 **Deploy to production** - Update Azure config and test end-to-end
5. 🎉 **Done!** - Webhooks will now trigger automatically for every form submission

---

## Security Notes

- ✅ `/api/trigger-webhook` endpoint is exempt from API key authentication (by design)
- ✅ Only internal Functions should call this endpoint
- ⚠️ If your admin app is publicly accessible, consider:
  - Adding a shared secret header requirement
  - IP whitelisting Functions app
  - Moving admin app to internal network only

Would you like me to add shared secret authentication? Let me know!
