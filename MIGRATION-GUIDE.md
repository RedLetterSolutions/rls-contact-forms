# Migration from Environment Variables to Database

## Overview
Your contact form sites have been successfully migrated from Azure environment variables to a PostgreSQL database.

## Seeded Sites

### 1. Guitar Repair of Tampa Bay
**API Endpoint:** `POST /api/v1/contact/guitar_repair_of_tampa_bay`

**Configuration:**
- **Site ID:** `guitar_repair_of_tampa_bay`
- **Name:** Guitar Repair of Tampa Bay
- **To Email:** cody@redlettersolutions.io
- **From Email:** admin@redlettersolutions.io
- **Redirect URL:** http://localhost:5173/form-sent
- **Allowed Origins:**
  - http://localhost:5173
  - http://www.guitarrepairoftampabay.com
- **Status:** ✅ Active

---

### 2. Logos Helix Partners
**API Endpoint:** `POST /api/v1/contact/logos`

**Configuration:**
- **Site ID:** `logos`
- **Name:** Logos Helix Partners
- **To Email:** codyjg10@gmail.com
- **From Email:** admin@redlettersolutions.io
- **Redirect URL:** http://localhost:5173#form-complete
- **Allowed Origins:**
  - https://logoshelixpartners.com
  - http://localhost:5173
- **Status:** ✅ Active

---

### 3. Test Site
**API Endpoint:** `POST /api/v1/contact/test`

**Configuration:**
- **Site ID:** `test`
- **Name:** Test Site
- **To Email:** cody@redlettersolutions.io
- **From Email:** admin@redlettersolutions.io
- **Redirect URL:** http://127.0.0.1:5500/test-website.html#form-complete
- **Allowed Origins:**
  - https://codygordon.com
  - https://www.codygordon.com
  - http://127.0.0.1:5500
  - http://localhost:5500
  - file://
- **Status:** ✅ Active

---

## Environment Variables Mapping

### Before (Environment Variables)
```
sites__guitar_repair_of_tampa_bay__to_email=cody@redlettersolutions.io
sites__guitar_repair_of_tampa_bay__from_email=admin@redlettersolutions.io
sites__guitar_repair_of_tampa_bay__redirect_url=http://localhost:5173/form-sent
sites__guitar_repair_of_tampa_bay__allow_origins=http://localhost:5173,http://www.guitarrepairoftampabay.com

sites__logos__to_email=codyjg10@gmail.com
sites__logos__from_email=admin@redlettersolutions.io
sites__logos__redirect_url=http://localhost:5173#form-complete
sites__logos__allow_origins=https://logoshelixpartners.com,http://localhost:5173

sites__test__to_email=cody@redlettersolutions.io
sites__test__from_email=admin@redlettersolutions.io
sites__test__redirect_url=http://127.0.0.1:5500/test-website.html#form-complete
sites__test__allow_origins=https://codygordon.com,https://www.codygordon.com,http://127.0.0.1:5500,http://localhost:5500,file://
```

### After (Database)
All configuration is now stored in the `sites` table in PostgreSQL and can be managed through the admin portal at:
**http://localhost:5282/Sites**

---

## Admin Portal Access

### Site Management
- **URL:** http://localhost:5282/Sites
- **Features:**
  - View all configured sites
  - Create new sites
  - Edit existing site configurations
  - Activate/deactivate sites
  - Delete sites
  - No need to manage Azure environment variables

### Quick Actions
1. **View Sites:** Navigate to http://localhost:5282/Sites
2. **Add New Site:** Click "Add New Site" button
3. **Edit Site:** Click the edit icon next to any site
4. **Toggle Active/Inactive:** Click the play/pause icon
5. **Delete Site:** Click the trash icon (with confirmation)

---

## Azure Functions Configuration

### Required Connection String
Your Azure Functions app needs the `PostgresConnectionString` configuration:

```json
{
  "PostgresConnectionString": "Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;"
}
```

### Code Changes
The `Contact.cs` Azure Function now queries the database:
```csharp
private async Task<SiteConfiguration?> LoadSiteConfigurationAsync(string siteId)
{
    var site = await _dbContext.Sites
        .Where(s => s.SiteId == siteId && s.IsActive)
        .FirstOrDefaultAsync();
    
    // Returns null if site not found or inactive
    return site == null ? null : new SiteConfiguration { ... };
}
```

---

## Benefits of Database Approach

✅ **Centralized Management** - Single admin portal for all site configurations
✅ **No Azure Portal Access Needed** - Manage sites without Azure credentials
✅ **Instant Updates** - Changes take effect immediately (no redeployment)
✅ **Audit Trail** - Track when sites were created and updated
✅ **Easy Backup** - Database snapshots include site configurations
✅ **Version Control Not Required** - No need to commit config changes
✅ **Multi-User Support** - Multiple admins can manage sites safely

---

## Testing Your Sites

### Example API Call (Guitar Repair of Tampa Bay)
```javascript
fetch('https://your-functions-app.azurewebsites.net/api/v1/contact/guitar_repair_of_tampa_bay', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        name: 'Test User',
        email: 'test@example.com',
        message: 'This is a test message',
        _hp: '' // Honeypot field (must be empty)
    })
})
.then(r => r.json())
.then(data => {
    if (data.ok) {
        window.location.href = data.redirect; // Redirect on success
    }
});
```

### Expected Response
```json
{
    "ok": true,
    "redirect": "http://localhost:5173/form-sent"
}
```

---

## Cleanup (Optional)

Once you verify everything works with the database, you can safely remove these environment variables from Azure:
- `sites__guitar_repair_of_tampa_bay__*`
- `sites__logos__*`
- `sites__test__*`

Keep these essential variables:
- `PostgresConnectionString` ✅ (required)
- `SENDGRID_API_KEY` ✅ (required)
- `AzureWebJobsStorage` ✅ (required)
- `DEFAULT_FROM_EMAIL` ✅ (optional fallback)

---

## Next Steps

1. ✅ **Sites Seeded** - All 3 sites have been loaded into the database
2. 🔄 **Test Admin Portal** - Visit http://localhost:5282/Sites to verify
3. 🔄 **Test Azure Functions** - Submit test forms to each endpoint
4. 🔄 **Update Production** - Deploy Azure Functions with database support
5. 🔄 **Remove Old Env Vars** - Clean up Azure environment variables after verification

---

## Support

If you need to:
- **Add a new site:** Use the admin portal's "Add New Site" button
- **Update CORS origins:** Edit the site in the admin portal
- **Change email recipients:** Edit the site in the admin portal
- **Disable a site temporarily:** Click the pause button in the admin portal
- **Re-enable a site:** Click the play button in the admin portal

No Azure Portal access or code changes required! 🎉
