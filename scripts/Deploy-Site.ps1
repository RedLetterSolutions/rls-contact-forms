# Azure Function App Site Configuration Script (PowerShell)
# This script automatically configures environment variables for new sites in your RLS Contact Form function

param(
    [Parameter(Mandatory=$true, HelpMessage="Azure Function App name")]
    [string]$FunctionAppName,
    
    [Parameter(Mandatory=$true, HelpMessage="Resource group name")]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true, HelpMessage="Azure subscription ID")]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true, HelpMessage="Site configuration JSON file path")]
    [string]$ConfigFile,
    
    [Parameter(HelpMessage="Create backup of current settings")]
    [switch]$Backup,
    
    [Parameter(HelpMessage="Show what would be changed without applying")]
    [switch]$DryRun,
    
    [Parameter(HelpMessage="Show help message")]
    [switch]$Help
)

# Function to write colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Function to show usage
function Show-Usage {
    @"
Azure Function App Site Configuration Script

DESCRIPTION:
    Configure a new site for RLS Contact Form Azure Function

PARAMETERS:
    -FunctionAppName    Azure Function App name (required)
    -ResourceGroupName  Resource group name (required)
    -SubscriptionId     Azure subscription ID (required)
    -ConfigFile         Site configuration JSON file (required)
    -Backup             Create backup of current settings
    -DryRun             Show what would be changed without applying
    -Help               Show this help message

EXAMPLES:
    # Configure from JSON file
    .\Deploy-Site.ps1 -FunctionAppName "my-function-app" -ResourceGroupName "my-rg" -SubscriptionId "sub-id" -ConfigFile "site-config.json"

    # Dry run to preview changes
    .\Deploy-Site.ps1 -FunctionAppName "my-function-app" -ResourceGroupName "my-rg" -SubscriptionId "sub-id" -ConfigFile "site-config.json" -DryRun

    # Create backup before applying changes
    .\Deploy-Site.ps1 -FunctionAppName "my-function-app" -ResourceGroupName "my-rg" -SubscriptionId "sub-id" -ConfigFile "site-config.json" -Backup

CONFIGURATION FILE FORMAT:
    {
        "siteId": "example-site",
        "toEmail": "contact@example.com",
        "fromEmail": "noreply@example.com",
        "redirectUrl": "https://example.com/thank-you",
        "allowOrigins": "https://example.com,https://www.example.com",
        "secret": "optional-secret-for-hmac"
    }
"@
}

# Function to validate email format
function Test-EmailFormat {
    param([string]$Email)
    
    $emailPattern = "^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$"
    return $Email -match $emailPattern
}

# Function to validate URL format
function Test-UrlFormat {
    param([string]$Url)
    
    try {
        $uri = [System.Uri]$Url
        return $uri.Scheme -in @("http", "https")
    }
    catch {
        return $false
    }
}

# Function to validate site configuration
function Test-SiteConfig {
    param([string]$ConfigFilePath)
    
    if (-not (Test-Path $ConfigFilePath)) {
        Write-Error "Configuration file not found: $ConfigFilePath"
        return $false
    }
    
    try {
        $config = Get-Content $ConfigFilePath -Raw | ConvertFrom-Json
    }
    catch {
        Write-Error "Invalid JSON format in configuration file: $_"
        return $false
    }
    
    # Check required fields
    $requiredFields = @("siteId", "toEmail", "fromEmail", "redirectUrl", "allowOrigins")
    foreach ($field in $requiredFields) {
        if (-not $config.$field) {
            Write-Error "Missing required field: $field"
            return $false
        }
    }
    
    # Validate field formats
    if (-not (Test-EmailFormat $config.toEmail)) {
        Write-Error "Invalid email format: $($config.toEmail)"
        return $false
    }
    
    if (-not (Test-EmailFormat $config.fromEmail)) {
        Write-Error "Invalid email format: $($config.fromEmail)"
        return $false
    }
    
    if (-not (Test-UrlFormat $config.redirectUrl)) {
        Write-Error "Invalid URL format: $($config.redirectUrl)"
        return $false
    }
    
    # Validate allow_origins URLs
    $origins = $config.allowOrigins -split ","
    foreach ($origin in $origins) {
        $origin = $origin.Trim()
        if (-not (Test-UrlFormat $origin)) {
            Write-Error "Invalid URL format in allowOrigins: $origin"
            return $false
        }
    }
    
    # Validate site_id format
    if ($config.siteId -notmatch "^[a-zA-Z0-9_-]+$") {
        Write-Error "Invalid siteId format: $($config.siteId) (only alphanumeric, hyphens, and underscores allowed)"
        return $false
    }
    
    Write-Success "Configuration validation passed"
    return $true
}

# Function to check Azure PowerShell login
function Test-AzureLogin {
    try {
        $context = Get-AzContext
        if (-not $context) {
            Write-Error "Not logged in to Azure PowerShell. Please run 'Connect-AzAccount' first."
            return $false
        }
        return $true
    }
    catch {
        Write-Error "Not logged in to Azure PowerShell. Please run 'Connect-AzAccount' first."
        return $false
    }
}

# Function to backup current settings
function Backup-Settings {
    param(
        [string]$FunctionAppName,
        [string]$ResourceGroupName
    )
    
    $backupFile = "function-app-settings-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    
    Write-Status "Creating backup of current settings..."
    
    try {
        $settings = Get-AzFunctionAppSetting -Name $FunctionAppName -ResourceGroupName $ResourceGroupName
        $settings | ConvertTo-Json -Depth 10 | Out-File $backupFile -Encoding UTF8
        
        Write-Success "Backup created: $backupFile"
        return $true
    }
    catch {
        Write-Error "Failed to create backup: $_"
        return $false
    }
}

# Function to configure site settings
function Set-SiteConfiguration {
    param(
        [string]$ConfigFilePath,
        [string]$FunctionAppName,
        [string]$ResourceGroupName,
        [bool]$DryRunMode = $false
    )
    
    $config = Get-Content $ConfigFilePath -Raw | ConvertFrom-Json
    
    # Convert site_id to use double underscores for Azure
    $azureSiteId = $config.siteId -replace ":", "__" -replace "-", "_"
    
    # Prepare settings hashtable
    $settings = @{
        "sites__$($azureSiteId)__to_email" = $config.toEmail
        "sites__$($azureSiteId)__from_email" = $config.fromEmail
        "sites__$($azureSiteId)__redirect_url" = $config.redirectUrl
        "sites__$($azureSiteId)__allow_origins" = $config.allowOrigins
    }
    
    # Add secret if provided
    if ($config.secret) {
        $settings["sites__$($azureSiteId)__secret"] = $config.secret
    }
    
    if ($DryRunMode) {
        Write-Status "DRY RUN - Would configure the following settings:"
        $settings.GetEnumerator() | ForEach-Object {
            Write-Host "  $($_.Key) = $($_.Value)"
        }
        return $true
    }
    
    Write-Status "Configuring site settings for: $($config.siteId)"
    
    try {
        # Apply settings to Azure Function App
        foreach ($setting in $settings.GetEnumerator()) {
            Write-Status "Setting: $($setting.Key)"
        }
        
        Update-AzFunctionAppSetting -Name $FunctionAppName -ResourceGroupName $ResourceGroupName -AppSetting $settings
        
        Write-Success "Site configuration completed successfully for: $($config.siteId)"
        Write-Status "Site URL: https://$FunctionAppName.azurewebsites.net/v1/contact/$($config.siteId)"
        return $true
    }
    catch {
        Write-Error "Failed to configure site settings: $_"
        return $false
    }
}

# Main execution
if ($Help) {
    Show-Usage
    exit 0
}

# Check if required modules are installed
$requiredModules = @("Az.Functions", "Az.Accounts")
foreach ($module in $requiredModules) {
    if (-not (Get-Module -ListAvailable -Name $module)) {
        Write-Error "Required PowerShell module '$module' is not installed. Please install it with: Install-Module $module"
        exit 1
    }
}

# Import required modules
try {
    Import-Module Az.Functions -Force
    Import-Module Az.Accounts -Force
}
catch {
    Write-Error "Failed to import required modules: $_"
    exit 1
}

# Check prerequisites
if (-not (Test-AzureLogin)) {
    exit 1
}

# Set subscription context
try {
    Set-AzContext -SubscriptionId $SubscriptionId | Out-Null
    Write-Status "Using subscription: $SubscriptionId"
}
catch {
    Write-Error "Failed to set subscription context: $_"
    exit 1
}

# Validate configuration
if (-not (Test-SiteConfig $ConfigFile)) {
    exit 1
}

# Create backup if requested
if ($Backup) {
    if (-not (Backup-Settings $FunctionAppName $ResourceGroupName)) {
        exit 1
    }
}

# Configure the site
if (-not (Set-SiteConfiguration $ConfigFile $FunctionAppName $ResourceGroupName $DryRun)) {
    exit 1
}

Write-Success "Script completed successfully!"
