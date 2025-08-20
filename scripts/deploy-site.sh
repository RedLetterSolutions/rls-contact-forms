#!/bin/bash

# Azure Function App Site Configuration Script
# This script automatically configures environment variables for new sites in your RLS Contact Form function

set -e

# Configuration
FUNCTION_APP_NAME=""
RESOURCE_GROUP=""
SUBSCRIPTION_ID=""

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Configure a new site for RLS Contact Form Azure Function

OPTIONS:
    -a, --app-name          Azure Function App name (required)
    -g, --resource-group    Resource group name (required)
    -s, --subscription      Azure subscription ID (required)
    -c, --config-file       Site configuration JSON file (required)
    -b, --backup            Create backup of current settings
    -d, --dry-run           Show what would be changed without applying
    -h, --help              Show this help message

EXAMPLES:
    # Configure from JSON file
    $0 -a my-function-app -g my-rg -s sub-id -c site-config.json

    # Dry run to preview changes
    $0 -a my-function-app -g my-rg -s sub-id -c site-config.json --dry-run

    # Create backup before applying changes
    $0 -a my-function-app -g my-rg -s sub-id -c site-config.json --backup

CONFIGURATION FILE FORMAT:
    {
        "siteId": "example-site",
        "toEmail": "contact@example.com",
        "fromEmail": "noreply@example.com",
        "redirectUrl": "https://example.com/thank-you",
        "allowOrigins": "https://example.com,https://www.example.com",
        "secret": "optional-secret-for-hmac"
    }
EOF
}

# Function to validate email format
validate_email() {
    local email=$1
    if [[ ! $email =~ ^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]]; then
        print_error "Invalid email format: $email"
        return 1
    fi
    return 0
}

# Function to validate URL format
validate_url() {
    local url=$1
    if [[ ! $url =~ ^https?:// ]]; then
        print_error "Invalid URL format: $url (must start with http:// or https://)"
        return 1
    fi
    return 0
}

# Function to validate site configuration
validate_config() {
    local config_file=$1
    
    if [[ ! -f "$config_file" ]]; then
        print_error "Configuration file not found: $config_file"
        return 1
    fi
    
    # Check if file is valid JSON
    if ! jq empty "$config_file" 2>/dev/null; then
        print_error "Invalid JSON format in configuration file"
        return 1
    fi
    
    # Extract and validate required fields
    local site_id=$(jq -r '.siteId // empty' "$config_file")
    local to_email=$(jq -r '.toEmail // empty' "$config_file")
    local from_email=$(jq -r '.fromEmail // empty' "$config_file")
    local redirect_url=$(jq -r '.redirectUrl // empty' "$config_file")
    local allow_origins=$(jq -r '.allowOrigins // empty' "$config_file")
    
    # Check required fields
    if [[ -z "$site_id" ]]; then
        print_error "Missing required field: siteId"
        return 1
    fi
    
    if [[ -z "$to_email" ]]; then
        print_error "Missing required field: toEmail"
        return 1
    fi
    
    if [[ -z "$from_email" ]]; then
        print_error "Missing required field: fromEmail"
        return 1
    fi
    
    if [[ -z "$redirect_url" ]]; then
        print_error "Missing required field: redirectUrl"
        return 1
    fi
    
    if [[ -z "$allow_origins" ]]; then
        print_error "Missing required field: allowOrigins"
        return 1
    fi
    
    # Validate field formats
    validate_email "$to_email" || return 1
    validate_email "$from_email" || return 1
    validate_url "$redirect_url" || return 1
    
    # Validate allow_origins URLs
    IFS=',' read -ra ORIGINS <<< "$allow_origins"
    for origin in "${ORIGINS[@]}"; do
        validate_url "$origin" || return 1
    done
    
    # Validate site_id format (alphanumeric, hyphens, underscores only)
    if [[ ! $site_id =~ ^[a-zA-Z0-9_-]+$ ]]; then
        print_error "Invalid siteId format: $site_id (only alphanumeric, hyphens, and underscores allowed)"
        return 1
    fi
    
    print_success "Configuration validation passed"
    return 0
}

# Function to check Azure CLI login
check_azure_login() {
    if ! az account show >/dev/null 2>&1; then
        print_error "Not logged in to Azure CLI. Please run 'az login' first."
        return 1
    fi
    return 0
}

# Function to backup current settings
backup_settings() {
    local backup_file="function-app-settings-backup-$(date +%Y%m%d-%H%M%S).json"
    
    print_status "Creating backup of current settings..."
    
    az functionapp config appsettings list \
        --name "$FUNCTION_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --subscription "$SUBSCRIPTION_ID" \
        --output json > "$backup_file"
    
    if [[ $? -eq 0 ]]; then
        print_success "Backup created: $backup_file"
    else
        print_error "Failed to create backup"
        return 1
    fi
}

# Function to configure site settings
configure_site() {
    local config_file=$1
    local dry_run=${2:-false}
    
    # Extract configuration values
    local site_id=$(jq -r '.siteId' "$config_file")
    local to_email=$(jq -r '.toEmail' "$config_file")
    local from_email=$(jq -r '.fromEmail' "$config_file")
    local redirect_url=$(jq -r '.redirectUrl' "$config_file")
    local allow_origins=$(jq -r '.allowOrigins' "$config_file")
    local secret=$(jq -r '.secret // empty' "$config_file")
    
    # Convert site_id to use double underscores for Azure
    local azure_site_id="${site_id//:/__}"
    azure_site_id="${azure_site_id//-/_}"
    
    # Prepare settings array
    local settings=(
        "sites__${azure_site_id}__to_email=$to_email"
        "sites__${azure_site_id}__from_email=$from_email"
        "sites__${azure_site_id}__redirect_url=$redirect_url"
        "sites__${azure_site_id}__allow_origins=$allow_origins"
    )
    
    # Add secret if provided
    if [[ -n "$secret" ]]; then
        settings+=("sites__${azure_site_id}__secret=$secret")
    fi
    
    if [[ "$dry_run" == "true" ]]; then
        print_status "DRY RUN - Would configure the following settings:"
        printf "%s\n" "${settings[@]}"
        return 0
    fi
    
    print_status "Configuring site settings for: $site_id"
    
    # Apply settings to Azure Function App
    for setting in "${settings[@]}"; do
        print_status "Setting: $setting"
    done
    
    az functionapp config appsettings set \
        --name "$FUNCTION_APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --subscription "$SUBSCRIPTION_ID" \
        --settings "${settings[@]}"
    
    if [[ $? -eq 0 ]]; then
        print_success "Site configuration completed successfully for: $site_id"
        print_status "Site URL: https://$FUNCTION_APP_NAME.azurewebsites.net/v1/contact/$site_id"
    else
        print_error "Failed to configure site settings"
        return 1
    fi
}

# Main function
main() {
    local config_file=""
    local create_backup=false
    local dry_run=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -a|--app-name)
                FUNCTION_APP_NAME="$2"
                shift 2
                ;;
            -g|--resource-group)
                RESOURCE_GROUP="$2"
                shift 2
                ;;
            -s|--subscription)
                SUBSCRIPTION_ID="$2"
                shift 2
                ;;
            -c|--config-file)
                config_file="$2"
                shift 2
                ;;
            -b|--backup)
                create_backup=true
                shift
                ;;
            -d|--dry-run)
                dry_run=true
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            *)
                print_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Validate required parameters
    if [[ -z "$FUNCTION_APP_NAME" ]] || [[ -z "$RESOURCE_GROUP" ]] || [[ -z "$SUBSCRIPTION_ID" ]] || [[ -z "$config_file" ]]; then
        print_error "Missing required parameters"
        show_usage
        exit 1
    fi
    
    # Check prerequisites
    if ! command -v az &> /dev/null; then
        print_error "Azure CLI is not installed. Please install it first."
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        print_error "jq is not installed. Please install it first."
        exit 1
    fi
    
    check_azure_login || exit 1
    
    # Validate configuration
    validate_config "$config_file" || exit 1
    
    # Create backup if requested
    if [[ "$create_backup" == "true" ]]; then
        backup_settings || exit 1
    fi
    
    # Configure the site
    configure_site "$config_file" "$dry_run"
}

# Run main function with all arguments
main "$@"
