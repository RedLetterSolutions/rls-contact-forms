# Contact Forms Admin - Complete Deployment Guide

## 🚀 Quick Start

This admin application provides:
1. **Dashboard** - View all contact form submissions in a table
2. **Webhooks** - Configure webhooks per site with test functionality
3. **API** - Query submissions via REST API with API key authentication
4. **API Keys** - Generate and manage API keys

## 📋 Prerequisites

- PostgreSQL database (Railway)
- Docker (for deployment)
- .NET 8 SDK (for local development)

## 🔧 Setup Steps

### 1. Create Missing View Files

The application needs view files. Create these in the `Views` folder:

####  Views/_ViewStart.cshtml
```cshtml
@{
    Layout = "_Layout";
}
```

#### Views/Shared/_Layout.cshtml
```cshtml
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Contact Forms Admin</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-dark bg-dark border-bottom box-shadow mb-3">
            <div class="container-fluid">
                <a class="navbar-brand" asp-controller="Dashboard" asp-action="Index">Contact Forms Admin</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                    @if (User.Identity?.IsAuthenticated == true)
                    {
                        <ul class="navbar-nav flex-grow-1">
                            <li class="nav-item">
                                <a class="nav-link" asp-controller="Dashboard" asp-action="Index">Submissions</a>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link" asp-controller="Webhooks" asp-action="Index">Webhooks</a>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link" asp-controller="ApiKeys" asp-action="Index">API Keys</a>
                            </li>
                        </ul>
                        <ul class="navbar-nav">
                            <li class="nav-item">
                                <span class="navbar-text me-3">@User.Identity.Name</span>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link" asp-controller="Auth" asp-action="Logout">Logout</a>
                            </li>
                        </ul>
                    }
                </div>
            </div>
        </nav>
    </header>
    <div class="container">
        @if (TempData["Success"] != null)
        {
            <div class="alert alert-success alert-dismissible fade show">
                @TempData["Success"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        @if (TempData["Error"] != null)
        {
            <div class="alert alert-danger alert-dismissible fade show">
                @TempData["Error"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        @if (TempData["NewApiKey"] != null)
        {
            <div class="alert alert-warning alert-dismissible fade show">
                <strong>New API Key:</strong> <code>@TempData["NewApiKey"]</code>
                <br/><small>SAVE THIS KEY NOW - it will not be shown again!</small>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        <main role="main" class="pb-3">
            @RenderBody()
        </main>
    </div>
    <footer class="border-top footer text-muted">
        <div class="container text-center py-3">
            &copy; 2025 - Contact Forms Admin
        </div>
    </footer>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

#### Views/Auth/Login.cshtml
```cshtml
@{
    ViewData["Title"] = "Login";
    Layout = null;
}

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Login - Contact Forms Admin</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" />
</head>
<body class="bg-light">
    <div class="container">
        <div class="row justify-content-center align-items-center min-vh-100">
            <div class="col-md-4">
                <div class="card shadow">
                    <div class="card-body p-5">
                        <h3 class="card-title text-center mb-4">Contact Forms Admin</h3>

                        @if (ViewBag.Error != null)
                        {
                            <div class="alert alert-danger">@ViewBag.Error</div>
                        }

                        <form method="post" asp-action="Login">
                            <input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />

                            <div class="mb-3">
                                <label for="username" class="form-label">Username</label>
                                <input type="text" class="form-control" id="username" name="username" required autofocus />
                            </div>

                            <div class="mb-3">
                                <label for="password" class="form-label">Password</label>
                                <input type="password" class="form-control" id="password" name="password" required />
                            </div>

                            <button type="submit" class="btn btn-primary w-100">Login</button>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>
```

#### Views/Dashboard/Index.cshtml
```cshtml
@model IEnumerable<ContactFormsAdmin.Models.ContactSubmission>
@{
    ViewData["Title"] = "Contact Submissions";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h1>Contact Submissions</h1>
    <span class="badge bg-secondary fs-6">Total: @ViewBag.TotalCount</span>
</div>

<div class="card mb-3">
    <div class="card-body">
        <form method="get" class="row g-3">
            <div class="col-auto">
                <label for="siteId" class="form-label">Filter by Site:</label>
            </div>
            <div class="col-auto">
                <select name="siteId" id="siteId" class="form-select" onchange="this.form.submit()">
                    <option value="">All Sites</option>
                    @foreach (var site in ViewBag.Sites)
                    {
                        <option value="@site" selected="@(site == ViewBag.CurrentSiteId)">@site</option>
                    }
                </select>
            </div>
        </form>
    </div>
</div>

<div class="table-responsive">
    <table class="table table-hover table-striped">
        <thead class="table-dark">
            <tr>
                <th>ID</th>
                <th>Site</th>
                <th>Name</th>
                <th>Email</th>
                <th>Message</th>
                <th>Submitted</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model)
            {
                <tr>
                    <td>@item.Id</td>
                    <td><span class="badge bg-info">@item.SiteId</span></td>
                    <td>@item.Name</td>
                    <td>@item.Email</td>
                    <td>@(item.Message.Length > 50 ? item.Message.Substring(0, 50) + "..." : item.Message)</td>
                    <td>@item.SubmittedAt.ToString("yyyy-MM-dd HH:mm")</td>
                    <td>
                        <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-primary">View</a>
                        <form asp-action="Delete" asp-route-id="@item.Id" method="post" class="d-inline" onsubmit="return confirm('Delete this submission?');">
                            <button type="submit" class="btn btn-sm btn-danger">Delete</button>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@if (ViewBag.TotalPages > 1)
{
    <nav>
        <ul class="pagination">
            @for (int i = 1; i <= ViewBag.TotalPages; i++)
            {
                <li class="page-item @(i == ViewBag.Page ? "active" : "")">
                    <a class="page-link" href="?siteId=@ViewBag.CurrentSiteId&page=@i">@i</a>
                </li>
            }
        </ul>
    </nav>
}
```

#### Views/Dashboard/Details.cshtml
```cshtml
@model ContactFormsAdmin.Models.ContactSubmission
@{
    ViewData["Title"] = "Submission Details";
}

<h1>Submission Details</h1>

<div class="card">
    <div class="card-header">
        <strong>Submission #@Model.Id</strong> - <span class="badge bg-info">@Model.SiteId</span>
    </div>
    <div class="card-body">
        <dl class="row">
            <dt class="col-sm-3">Name</dt>
            <dd class="col-sm-9">@Model.Name</dd>

            <dt class="col-sm-3">Email</dt>
            <dd class="col-sm-9">@Model.Email</dd>

            <dt class="col-sm-3">Message</dt>
            <dd class="col-sm-9">@Model.Message</dd>

            <dt class="col-sm-3">Client IP</dt>
            <dd class="col-sm-9">@Model.ClientIp</dd>

            <dt class="col-sm-3">Submitted At</dt>
            <dd class="col-sm-9">@Model.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss")</dd>

            <dt class="col-sm-3">Created At</dt>
            <dd class="col-sm-9">@Model.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")</dd>

            @if (!string.IsNullOrEmpty(Model.MetadataJson))
            {
                var metadata = Model.GetMetadata();
                if (metadata.Any())
                {
                    <dt class="col-sm-3">Additional Fields</dt>
                    <dd class="col-sm-9">
                        <table class="table table-sm table-bordered">
                            @foreach (var field in metadata)
                            {
                                <tr>
                                    <th style="width: 30%">@field.Key</th>
                                    <td>@field.Value</td>
                                </tr>
                            }
                        </table>
                    </dd>
                }
            }
        </dl>

        <a asp-action="Index" class="btn btn-secondary">Back to List</a>
        <form asp-action="Delete" asp-route-id="@Model.Id" method="post" class="d-inline" onsubmit="return confirm('Delete this submission?');">
            <button type="submit" class="btn btn-danger">Delete</button>
        </form>
    </div>
</div>
```

#### Views/Webhooks/Index.cshtml
```cshtml
@model IEnumerable<ContactFormsAdmin.Models.Webhook>
@{
    ViewData["Title"] = "Webhooks";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h1>Webhooks</h1>
    <a asp-action="Create" class="btn btn-primary">Add Webhook</a>
</div>

<div class="table-responsive">
    <table class="table table-hover">
        <thead class="table-dark">
            <tr>
                <th>Site ID</th>
                <th>URL</th>
                <th>Description</th>
                <th>Status</th>
                <th>Last Triggered</th>
                <th>Last Result</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var webhook in Model)
            {
                <tr>
                    <td><span class="badge bg-info">@webhook.SiteId</span></td>
                    <td><small>@webhook.Url</small></td>
                    <td>@webhook.Description</td>
                    <td>
                        @if (webhook.IsActive)
                        {
                            <span class="badge bg-success">Active</span>
                        }
                        else
                        {
                            <span class="badge bg-secondary">Inactive</span>
                        }
                    </td>
                    <td>
                        @if (webhook.LastTriggeredAt.HasValue)
                        {
                            @webhook.LastTriggeredAt.Value.ToString("yyyy-MM-dd HH:mm")
                        }
                        else
                        {
                            <span class="text-muted">Never</span>
                        }
                    </td>
                    <td>
                        @if (webhook.LastSuccess.HasValue)
                        {
                            if (webhook.LastSuccess.Value)
                            {
                                <span class="badge bg-success">Success</span>
                            }
                            else
                            {
                                <span class="badge bg-danger" title="@webhook.LastError">Failed</span>
                            }
                        }
                    </td>
                    <td>
                        <form asp-action="Test" asp-route-id="@webhook.Id" method="post" class="d-inline">
                            <button type="submit" class="btn btn-sm btn-info">Test</button>
                        </form>
                        <form asp-action="ToggleActive" asp-route-id="@webhook.Id" method="post" class="d-inline">
                            <button type="submit" class="btn btn-sm btn-warning">
                                @(webhook.IsActive ? "Deactivate" : "Activate")
                            </button>
                        </form>
                        <a asp-action="Edit" asp-route-id="@webhook.Id" class="btn btn-sm btn-primary">Edit</a>
                        <form asp-action="Delete" asp-route-id="@webhook.Id" method="post" class="d-inline" onsubmit="return confirm('Delete this webhook?');">
                            <button type="submit" class="btn btn-sm btn-danger">Delete</button>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@if (!Model.Any())
{
    <div class="alert alert-info">
        No webhooks configured yet. Click "Add Webhook" to create one.
    </div>
}
```

#### Views/Webhooks/Create.cshtml & Edit.cshtml
```cshtml
@model ContactFormsAdmin.Models.Webhook
@{
    ViewData["Title"] = Model.Id == 0 ? "Create Webhook" : "Edit Webhook";
}

<h1>@ViewData["Title"]</h1>

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <div class="mb-3">
                <label asp-for="SiteId" class="form-label"></label>
                <input asp-for="SiteId" class="form-control" required />
                <small class="text-muted">The site ID this webhook is for (e.g., world1, test-site)</small>
            </div>
            <div class="mb-3">
                <label asp-for="Url" class="form-label"></label>
                <input asp-for="Url" class="form-control" type="url" required />
                <small class="text-muted">Full URL to POST webhook data to</small>
            </div>
            <div class="mb-3">
                <label asp-for="Description" class="form-label"></label>
                <input asp-for="Description" class="form-control" />
            </div>
            <div class="mb-3 form-check">
                <input asp-for="IsActive" class="form-check-input" />
                <label asp-for="IsActive" class="form-check-label"></label>
            </div>
            <button type="submit" class="btn btn-primary">Save</button>
            <a asp-action="Index" class="btn btn-secondary">Cancel</a>
        </form>
    </div>
</div>
```

#### Views/ApiKeys/Index.cshtml
```cshtml
@model IEnumerable<ContactFormsAdmin.Models.ApiKey>
@{
    ViewData["Title"] = "API Keys";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h1>API Keys</h1>
    <a asp-action="Create" class="btn btn-primary">Generate New API Key</a>
</div>

<div class="table-responsive">
    <table class="table table-hover">
        <thead class="table-dark">
            <tr>
                <th>Name</th>
                <th>Created</th>
                <th>Last Used</th>
                <th>Expires</th>
                <th>Status</th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var key in Model)
            {
                <tr>
                    <td>@key.Name</td>
                    <td>@key.CreatedAt.ToString("yyyy-MM-dd HH:mm")</td>
                    <td>
                        @if (key.LastUsedAt.HasValue)
                        {
                            @key.LastUsedAt.Value.ToString("yyyy-MM-dd HH:mm")
                        }
                        else
                        {
                            <span class="text-muted">Never</span>
                        }
                    </td>
                    <td>
                        @if (key.ExpiresAt.HasValue)
                        {
                            @key.ExpiresAt.Value.ToString("yyyy-MM-dd")
                        }
                        else
                        {
                            <span class="text-muted">Never</span>
                        }
                    </td>
                    <td>
                        @if (key.IsActive)
                        {
                            <span class="badge bg-success">Active</span>
                        }
                        else
                        {
                            <span class="badge bg-secondary">Inactive</span>
                        }
                    </td>
                    <td>
                        <form asp-action="ToggleActive" asp-route-id="@key.Id" method="post" class="d-inline">
                            <button type="submit" class="btn btn-sm btn-warning">
                                @(key.IsActive ? "Deactivate" : "Activate")
                            </button>
                        </form>
                        <form asp-action="Delete" asp-route-id="@key.Id" method="post" class="d-inline" onsubmit="return confirm('Delete this API key?');">
                            <button type="submit" class="btn btn-sm btn-danger">Delete</button>
                        </form>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

#### Views/ApiKeys/Create.cshtml
```cshtml
@{
    ViewData["Title"] = "Generate API Key";
}

<h1>Generate New API Key</h1>

<div class="row">
    <div class="col-md-6">
        <form method="post">
            <div class="mb-3">
                <label for="name" class="form-label">Key Name</label>
                <input type="text" class="form-control" id="name" name="name" required />
                <small class="text-muted">A friendly name for this API key</small>
            </div>
            <div class="mb-3">
                <label for="expiresAt" class="form-label">Expiration Date (Optional)</label>
                <input type="datetime-local" class="form-control" id="expiresAt" name="expiresAt" />
                <small class="text-muted">Leave empty for no expiration</small>
            </div>
            <button type="submit" class="btn btn-primary">Generate API Key</button>
            <a asp-action="Index" class="btn btn-secondary">Cancel</a>
        </form>
    </div>
</div>
```

### 2. Build the Docker Image

```bash
cd ContactFormsAdmin
docker build -t contact-forms-admin .
```

### 3. Deploy to Railway

1. **Install Railway CLI:**
   ```bash
   npm install -g @railway/cli
   railway login
   ```

2. **Create new project:**
   ```bash
   railway init
   ```

3. **Add environment variables in Railway dashboard:**
   ```
   ConnectionStrings__DefaultConnection=Host=caboose.proxy.rlwy.net;Port=46817;Username=postgres;Password=TEldbxeBObbohehNCkfjJpadtmyUNPRC;Database=railway;SSL Mode=Require;Trust Server Certificate=true;
   AdminUsername=admin
   AdminPassword=YourSecurePassword123!
   ASPNETCORE_ENVIRONMENT=Production
   ```

4. **Deploy:**
   ```bash
   railway up
   ```

### 4. Update Azure Function to Trigger Webhooks

Add this NuGet package to your Azure Function project:
```bash
cd ../  # Back to main project
dotnet add package Newtonsoft.Json
```

Then update `Contact.cs` to include webhook triggering. Add at line 123 (after email send):

```csharp
// Trigger webhooks (fire and forget)
_ = Task.Run(async () =>
{
    try
    {
        using var httpClient = new HttpClient();
        var webhookUrl = $"{Environment.GetEnvironmentVariable("ADMIN_URL") ?? "http://localhost:8080"}/api/trigger-webhook";

        var webhookPayload = new
        {
            siteId,
            data = new
            {
                name = formData.GetValueOrDefault("name", ""),
                email = formData.GetValueOrDefault("email", ""),
                message = formData.GetValueOrDefault("message", ""),
                metadata = GetMetadataFields(formData),
                clientIp,
                submittedAt = DateTime.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(webhookPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Call admin app to trigger webhooks
        await httpClient.PostAsync(webhookUrl, content);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to trigger webhooks for site {SiteId}", siteId);
    }
});
```

## 📡 API Usage

### Authentication
Include API key in header:
```bash
curl -H "X-API-Key: cfadmin_yourApiKeyHere" \
  https://your-app.railway.app/api/submissions/world1
```

### Endpoints

**Get submissions for a site:**
```
GET /api/submissions/{siteId}?limit=100&offset=0
```

**Get specific submission:**
```
GET /api/submissions/{siteId}/{id}
```

**Get statistics:**
```
GET /api/stats/{siteId}
```

**List all sites:**
```
GET /api/sites
```

## 🔐 Default Login

- **Username:** `admin`
- **Password:** `ChangeMe123!` (Change this in appsettings.json or environment variables!)

## ✅ Testing

1. **Login** at https://your-app.railway.app
2. **View submissions** - should show the test data we seeded earlier
3. **Create a webhook** pointing to https://webhook.site or https://requestbin.com
4. **Test the webhook** using the Test button
5. **Generate an API key** and test the API endpoints

## 🎉 You're Done!

Your complete admin application is now deployed with:
- ✅ Dashboard with filterable submissions table
- ✅ Webhook management with test functionality
- ✅ API endpoints with API key authentication
- ✅ Multi-tenant support
- ✅ Docker deployment ready for Railway

