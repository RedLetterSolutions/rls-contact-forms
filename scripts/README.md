# Site Deployment Scripts Documentation

This directory contains scripts to automatically configure new websites for the RLS Contact Form service without manually editing Azure Function App settings.

## Quick Start

### Prerequisites

**For Bash Script (`deploy-site.sh`):**
- Azure CLI installed and configured
- `jq` command-line JSON processor
- Bash shell (macOS/Linux/WSL)

**For PowerShell Script (`Deploy-Site.ps1`):**
- Azure PowerShell modules (`Az.Functions`, `Az.Accounts`)
- PowerShell 5.1+ or PowerShell Core 6+
- Windows, macOS, or Linux

### Installation

1. **Install Azure CLI** (for bash script):
   ```bash
   # macOS
   brew install azure-cli
   
   # Linux
   curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
   
   # Windows
   # Download from https://aka.ms/installazurecliwindows
   ```

2. **Install jq** (for bash script):
   ```bash
   # macOS
   brew install jq
   
   # Linux
   sudo apt-get install jq
   ```

3. **Install Azure PowerShell** (for PowerShell script):
   ```powershell
   Install-Module -Name Az.Functions -Force
   Install-Module -Name Az.Accounts -Force
   ```

4. **Login to Azure**:
   ```bash
   # For Azure CLI
   az login
   
   # For PowerShell
   Connect-AzAccount
   ```

## Usage

### Basic Site Configuration

1. **Create a site configuration file** (e.g., `my-site.json`):
   ```json
   {
     "siteId": "my-awesome-site",
     "toEmail": "contact@mysite.com",
     "fromEmail": "noreply@mysite.com",
     "redirectUrl": "https://mysite.com/thank-you",
     "allowOrigins": "https://mysite.com,https://www.mysite.com"
   }
   ```

2. **Run the deployment script**:

   **Bash:**
   ```bash
   chmod +x deploy-site.sh
   ./deploy-site.sh \
     --app-name "rls-contact-form" \
     --resource-group "rls-contact-form_group" \
     --subscription "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" \
     --config-file "test-site-config.json"
   ```

   **PowerShell:**
   ```powershell
   .\Deploy-Site.ps1 \
     -FunctionAppName "rls-contact-form" \
     -ResourceGroupName "rls-contact-form_group" \
     -SubscriptionId "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" \
     -ConfigFile "test-site-config.json"
   ```

3. **Your site is now configured!** Use this URL in your forms:
   ```
   https://rls-contact-form-d3bbb0f6avhtgxb5.eastus-01.azurewebsites.net/v1/contact/my-awesome-site
   ```

### Advanced Usage

#### Dry Run (Preview Changes)
See what would be configured without making changes:

```bash
# Bash
./deploy-site.sh --app-name "rls-contact-form-d3bbb0f6avhtgxb5" --resource-group "rls-contact-form-group" --subscription "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" --config-file "my-site.json" --dry-run

# PowerShell
.\Deploy-Site.ps1 -FunctionAppName "rls-contact-form-d3bbb0f6avhtgxb5" -ResourceGroupName "rls-contact-form-group" -SubscriptionId "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" -ConfigFile "my-site.json" -DryRun
```

#### Backup Current Settings
Create a backup before making changes:

```bash
# Bash
./deploy-site.sh --app-name "rls-contact-form-d3bbb0f6avhtgxb5" --resource-group "rls-contact-form-group" --subscription "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" --config-file "my-site.json" --backup

# PowerShell
.\Deploy-Site.ps1 -FunctionAppName "rls-contact-form-d3bbb0f6avhtgxb5" -ResourceGroupName "rls-contact-form-group" -SubscriptionId "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" -ConfigFile "my-site.json" -Backup
```

#### Site with HMAC Security
For enhanced security, add a secret for HMAC validation:

```json
{
  "siteId": "secure-site",
  "toEmail": "contact@securesite.com",
  "fromEmail": "noreply@securesite.com",
  "redirectUrl": "https://securesite.com/thank-you",
  "allowOrigins": "https://securesite.com",
  "secret": "my-super-secure-secret-key-minimum-16-chars"
}
```

## Configuration File Reference

### Required Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `siteId` | string | Unique site identifier (alphanumeric, hyphens, underscores only) | `"my-website"` |
| `toEmail` | string | Email where form submissions are sent | `"contact@example.com"` |
| `fromEmail` | string | Email used as sender for notifications | `"noreply@example.com"` |
| `redirectUrl` | string | URL to redirect after successful submission | `"https://example.com/thanks"` |
| `allowOrigins` | string | Comma-separated list of allowed origins for CORS | `"https://example.com,https://www.example.com"` |

### Optional Fields

| Field | Type | Description | Example |
|-------|------|-------------|---------|
| `secret` | string | Secret key for HMAC signature validation (min 16 chars) | `"my-secret-key-12345"` |

### Field Validation Rules

- **siteId**: Only letters, numbers, hyphens, and underscores
- **toEmail/fromEmail**: Valid email format
- **redirectUrl**: Valid HTTP/HTTPS URL
- **allowOrigins**: Comma-separated valid HTTP/HTTPS URLs (no spaces after commas)
- **secret**: Minimum 16 characters (recommended for security)

## Examples

### Example 1: Simple Business Website
```json
{
  "siteId": "acme-corp",
  "toEmail": "info@acmecorp.com",
  "fromEmail": "website@acmecorp.com",
  "redirectUrl": "https://acmecorp.com/contact-success",
  "allowOrigins": "https://acmecorp.com,https://www.acmecorp.com"
}
```

### Example 2: Client Website with Security
```json
{
  "siteId": "client-secure-site",
  "toEmail": "contact@clientsite.org",
  "fromEmail": "forms@youragency.com",
  "redirectUrl": "https://clientsite.org/thank-you",
  "allowOrigins": "https://clientsite.org",
  "secret": "ultra-secure-client-key-2024"
}
```

### Example 3: Development/Staging Site
```json
{
  "siteId": "staging-site",
  "toEmail": "dev@youragency.com",
  "fromEmail": "staging@youragency.com",
  "redirectUrl": "https://staging.clientsite.com/success",
  "allowOrigins": "https://staging.clientsite.com,https://dev.clientsite.com"
}
```

## Script Options Reference

### Bash Script (`deploy-site.sh`)

| Option | Short | Required | Description |
|--------|-------|----------|-------------|
| `--app-name` | `-a` | Yes | Azure Function App name |
| `--resource-group` | `-g` | Yes | Resource group name |
| `--subscription` | `-s` | Yes | Azure subscription ID |
| `--config-file` | `-c` | Yes | Site configuration JSON file |
| `--backup` | `-b` | No | Create backup of current settings |
| `--dry-run` | `-d` | No | Show changes without applying |
| `--help` | `-h` | No | Show help message |

### PowerShell Script (`Deploy-Site.ps1`)

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-FunctionAppName` | Yes | Azure Function App name |
| `-ResourceGroupName` | Yes | Resource group name |
| `-SubscriptionId` | Yes | Azure subscription ID |
| `-ConfigFile` | Yes | Site configuration JSON file |
| `-Backup` | No | Create backup of current settings |
| `-DryRun` | No | Show changes without applying |
| `-Help` | No | Show help message |

## Team Workflow

### For Team Leaders
1. Create configuration templates for common site types
2. Share the scripts and examples with team members
3. Set up a repository of configuration files for all client sites
4. Use version control to track site configuration changes

### For Developers
1. Create a site configuration file for each new project
2. Test with `--dry-run` first to verify settings
3. Use `--backup` for production deployments
4. Keep configuration files with project documentation

### For Agencies Managing Multiple Clients
1. Use consistent naming conventions for `siteId`:
   - `client-website-name` (e.g., `acme-main-site`)
   - `client-environment` (e.g., `acme-staging`)
2. Organize configuration files by client in folders
3. Document which sites use HMAC secrets
4. Maintain backup files for configuration rollbacks

## Troubleshooting

### Common Issues

**Authentication Errors:**
```
Error: Not logged in to Azure CLI
```
**Solution:** Run `az login` (for bash) or `Connect-AzAccount` (for PowerShell)

**Permission Errors:**
```
Error: Insufficient privileges to complete the operation
```
**Solution:** Ensure your Azure account has Contributor or Owner role on the Function App

**Invalid JSON:**
```
Error: Invalid JSON format in configuration file
```
**Solution:** Validate your JSON file with an online JSON validator

**Invalid Email Format:**
```
Error: Invalid email format: not-an-email
```
**Solution:** Ensure all email fields contain valid email addresses

**CORS Issues After Deployment:**
```
Browser console: CORS error when submitting form
```
**Solution:** Verify `allowOrigins` includes the exact domain where your form is hosted

### Getting Help

1. **Validate your configuration** with the JSON schema:
   ```bash
   # Using online JSON schema validator
   # Schema: site-config.schema.json
   # Data: your-site-config.json
   ```

2. **Use dry-run mode** to preview changes:
   ```bash
   ./deploy-site.sh ... --dry-run
   ```

3. **Check Azure Function logs** in the Azure Portal if forms aren't working after deployment

4. **Verify environment variables** in Azure Portal:
   - Go to Function App > Settings > Configuration
   - Look for `sites__your-site-id__*` variables

### Rollback Procedure

If something goes wrong, you can restore from backup:

1. **Locate your backup file** (created with `--backup` option):
   ```
   function-app-settings-backup-YYYYMMDD-HHMMSS.json
   ```

2. **Restore settings manually** in Azure Portal:
   - Go to Function App > Settings > Configuration
   - Delete problematic settings
   - Add back settings from backup file

3. **Or use Azure CLI** to restore (bash example):
   ```bash
   # Extract settings from backup and restore
   jq -r '.[] | "\(.name)=\(.value)"' backup-file.json > settings.txt
   az functionapp config appsettings set \
     --name "rls-contact-form-d3bbb0f6avhtgxb5" \
     --resource-group "rls-contact-form-group" \
     --subscription "e69c8e00-54a6-4c76-92e6-19dc4fa0b4bf" \
     --settings @settings.txt
   ```

## Files in This Directory

- `deploy-site.sh` - Bash script for macOS/Linux/WSL
- `Deploy-Site.ps1` - PowerShell script for Windows/Cross-platform
- `site-config.schema.json` - JSON schema for validation
- `example-site-config.json` - Example configuration file
- `README.md` - This documentation file

## Support

For questions or issues:
1. Check this documentation first
2. Validate your configuration file against the schema
3. Test with `--dry-run` before making changes
4. Use `--backup` for important deployments
5. Contact your development team for assistance
