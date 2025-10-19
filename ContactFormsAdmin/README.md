# Contact Forms Admin Portal

A complete admin web application for managing contact form submissions with webhooks and API access.

## Features

✅ **Dashboard** - View all contact submissions in a filterable table
✅ **Webhook Management** - Configure webhooks per site with test functionality
✅ **API Endpoints** - REST API for querying submissions with API key authentication
✅ **API Key Management** - Generate and manage API keys
✅ **Multi-Tenant** - Isolated data per site ID
✅ **Docker Ready** - Deploy anywhere with Docker

## Quick Start

### Prerequisites
- .NET 8 SDK
- PostgreSQL database (Railway recommended)
- Docker (for deployment)

### Local Development

1. **Restore packages:**
   ```bash
   dotnet restore
   ```

2. **Update connection string in `appsettings.json`**

3. **Run migrations:**
   ```bash
   dotnet ef database update
   ```

4. **Run the application:**
   ```bash
   dotnet run
   ```

5. **Access at:** `https://localhost:5001`

### Deployment

See **DEPLOY.md** for complete deployment instructions including:
- All view file templates
- Docker build steps
- Railway deployment guide
- Azure Function integration

## Default Credentials

- **Username:** admin
- **Password:** ChangeMe123!

**⚠️ IMPORTANT:** Change these in production via environment variables!

## API Usage

Generate an API key in the admin panel, then use it:

```bash
curl -H "X-API-Key: your_api_key_here" \
  https://your-app.railway.app/api/submissions/world1
```

## Tech Stack

- ASP.NET Core 8 MVC
- Entity Framework Core
- PostgreSQL
- Bootstrap 5
- Cookie Authentication
- API Key Authentication

## License

MIT
