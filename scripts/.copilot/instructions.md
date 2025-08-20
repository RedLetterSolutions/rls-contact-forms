# Copilot Instructions for Scripts Directory

## Directory Purpose
This directory contains deployment automation scripts for configuring Azure Function App environment variables for the RLS Contact Form service. These scripts enable remote management of site-specific configurations without manual Azure portal editing.

## Script Architecture

### Core Scripts
- `deploy-site.sh` - Bash script for macOS/Linux/WSL environments
- `Deploy-Site.ps1` - PowerShell script for cross-platform deployment
- Both scripts provide identical functionality with platform-specific implementations

### Configuration Files
- `site-config.schema.json` - JSON schema for validating site configurations
- `example-site-config.json` - Template configuration file
- `test-site-config.json` - Testing configuration

## Environment Variable Patterns

### Site Configuration Pattern
All site-specific settings follow this naming convention:
```
sites__<siteId>__<property>
```

### Standard Properties
- `sites__<siteId>__toEmail` - Destination email for form submissions
- `sites__<siteId>__fromEmail` - Sender email for notifications
- `sites__<siteId>__redirectUrl` - Success redirect URL
- `sites__<siteId>__allowOrigins` - CORS allowed origins (comma-separated)
- `sites__<siteId>__secret` - Optional HMAC secret for enhanced security

## Coding Standards

### Bash Scripts (`deploy-site.sh`)
- Use `set -e` for strict error handling
- Implement colored output functions (print_status, print_success, print_warning, print_error)
- Include comprehensive argument parsing with long and short options
- Validate all required parameters before execution
- Support dry-run mode for safe testing
- Implement backup functionality for rollback capability
- Use proper exit codes (0 for success, non-zero for errors)
- Include comprehensive help documentation

### PowerShell Scripts (`Deploy-Site.ps1`)
- Use proper parameter validation with `[Parameter()]` attributes
- Implement colored output functions (Write-Status, Write-Success, Write-Warning, Write-Error)
- Support switch parameters for optional features (-DryRun, -Backup, -Help)
- Use try-catch blocks for error handling
- Validate Azure PowerShell module availability
- Include proper parameter help messages
- Support pipeline input where appropriate

### JSON Configuration
- Follow the JSON schema strictly (`site-config.schema.json`)
- Validate all required fields: siteId, toEmail, fromEmail, redirectUrl, allowOrigins
- Support optional security enhancements with secret field
- Use meaningful validation patterns (email format, URL format, alphanumeric siteId)
- Include helpful examples and descriptions

## Azure Integration Patterns

### Authentication
- Support Azure CLI authentication (`az login`) for bash scripts
- Support Azure PowerShell authentication (`Connect-AzAccount`) for PowerShell scripts
- Validate authentication before attempting operations
- Handle subscription context properly

### Function App Management
- Use Azure CLI `az functionapp config appsettings set` for bash
- Use Azure PowerShell `Update-AzFunctionAppSetting` for PowerShell
- Batch environment variable updates for efficiency
- Implement proper error handling for Azure API failures

### Backup and Recovery
- Create timestamped backup files before changes
- Include all current environment variables in backups
- Provide clear rollback instructions in documentation
- Support restoration from backup files

## Security Considerations

### Input Validation
- Validate siteId format (alphanumeric, hyphens, underscores only)
- Validate email formats for toEmail and fromEmail
- Validate URL formats for redirectUrl and allowOrigins
- Enforce minimum secret length (16 characters) when provided

### Secret Management
- Never log or display secret values
- Treat HMAC secrets as sensitive data
- Support optional secret configuration
- Document secret requirements clearly

## Error Handling Standards

### User-Friendly Messages
- Provide clear, actionable error messages
- Include suggested solutions for common issues
- Use consistent color coding for different message types
- Avoid exposing internal Azure errors to users

### Common Error Scenarios
- Authentication failures (not logged in)
- Permission errors (insufficient Azure permissions)
- Invalid JSON configuration files
- Missing required Azure resources
- Network connectivity issues

## Testing Patterns

### Dry-Run Mode
- Show exactly what changes would be made
- Display current vs. new environment variable values
- Validate configuration without applying changes
- Support comprehensive preview functionality

### Configuration Validation
- Validate JSON against schema before processing
- Check Azure resource existence before deployment
- Verify authentication and permissions
- Test configuration file accessibility

## Documentation Standards

### Inline Comments
- Document complex Azure CLI/PowerShell commands
- Explain environment variable naming patterns
- Include examples for configuration patterns
- Document any Azure-specific limitations

### Help Text
- Provide comprehensive usage examples
- Include all parameter descriptions
- Show common workflow scenarios
- Reference related documentation files

## Development Guidelines

### When Adding New Features
- Maintain parity between bash and PowerShell versions
- Update JSON schema for new configuration options
- Add validation for new fields
- Update documentation and examples
- Test on multiple platforms

### When Modifying Existing Code
- Preserve backward compatibility with existing site configurations
- Update both script versions simultaneously
- Test with existing configuration files
- Update help text and documentation

### Configuration File Management
- Use consistent JSON formatting (2-space indentation)
- Include comprehensive examples
- Validate against schema in CI/CD if available
- Document breaking changes clearly

## Integration Points

### With Azure Functions
- Environment variables must match function app expectations
- CORS origins must align with actual website domains
- Email configurations must work with configured email service
- HMAC secrets must be consistent with function app validation

### With Main Project
- Site configurations should align with function app implementation
- Environment variable patterns must match main application code
- Security patterns should be consistent across the project
- Documentation should reference main project setup
