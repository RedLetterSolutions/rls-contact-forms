# Create Submission API Endpoint

## Overview

The Create Submission API endpoint allows you to programmatically add contact form submissions to the database. This is particularly useful for:

- **WordPress Integration**: Track submissions from WordPress Contact Form 7, Gravity Forms, etc.
- **Third-Party Forms**: Integrate with external form builders
- **Centralized Management**: Consolidate submissions from multiple sources
- **Historical Import**: Import existing submissions from other systems
- **Custom Integrations**: Build your own form solutions that store data centrally

## Endpoint

```
POST /api/submissions
```

Optional query parameter:

```
?triggerWebhooks=true|false
```

## Request

### Headers
```
X-API-Key: your_api_key_here
Content-Type: application/json
```

### Request Body

```json
{
  "siteId": "string (required)",
  "name": "string (required)",
  "email": "string (required)",
  "message": "string (required)",
  "clientIp": "string (optional)",
  "submittedAt": "datetime (optional)",
  "metadata": {
    "key": "value"
  } (optional)
  ,
  "triggerWebhooks": "boolean (optional)"
}
```

### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `siteId` | string | Yes | Must match an existing site ID in the database |
| `name` | string | Yes | Name of the person submitting the form |
| `email` | string | Yes | Email address of the submitter |
| `message` | string | Yes | The message or inquiry content |
| `clientIp` | string | No | IP address of the client (defaults to "API" if not provided) |
| `submittedAt` | datetime | No | Timestamp of submission (defaults to current UTC time) |
| `metadata` | object | No | Additional fields as key-value pairs (phone, company, etc.) |
| `triggerWebhooks` | boolean | No | If true, triggers configured webhooks for the given site after saving (also supported via query string) |

### Metadata Fields

The `metadata` object supports any custom fields you want to track. Common examples:

- `phone` - Phone number
- `company` - Company name
- `budget_range` - Budget information
- `source` - Where the submission came from (e.g., "WordPress", "Custom Form")
- `referrer` - Referring page URL
- `user_agent` - Browser user agent
- Any other custom fields specific to your forms

## Response

### Success (201 Created)

```json
{
  "success": true,
  "message": "Submission created successfully",
  "webhooksRequested": false,
  "submission": {
    "id": 123,
    "siteId": "example-site",
    "name": "John Doe",
    "email": "john@example.com",
    "message": "Hello, I have a question...",
    "clientIp": "192.168.1.100",
    "submittedAt": "2025-10-20T12:00:00Z",
    "metadata": {
      "phone": "555-1234",
      "company": "Acme Corp",
      "source": "WordPress"
    },
    "createdAt": "2025-10-20T17:50:00Z"
  }
}
```

### Error Responses

#### 400 Bad Request - Missing Required Field

```json
{
  "success": false,
  "error": "siteId is required"
}
```

Possible validation errors:
- "siteId is required"
- "name is required"
- "email is required"
- "message is required"

#### 404 Not Found - Invalid Site

```json
{
  "success": false,
  "error": "Site 'unknown-site' not found"
}
```

#### 500 Internal Server Error

```json
{
  "success": false,
  "error": "Failed to create submission",
  "details": "Database connection error"
}
```

## Examples

### Example 1: Minimal Submission

**Request:**
```bash
curl -X POST https://your-api-domain.com/api/submissions \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "my-site",
    "name": "Jane Smith",
    "email": "jane@example.com",
    "message": "I would like more information about your services."
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Submission created successfully",
  "webhooksRequested": true,
  "submission": {
    "id": 124,
    "siteId": "my-site",
    "name": "Jane Smith",
    "email": "jane@example.com",
    "message": "I would like more information about your services.",
    "clientIp": "API",
    "submittedAt": "2025-10-20T17:50:00Z",
    "metadata": {},
    "createdAt": "2025-10-20T17:50:00Z"
  }
}
```

### Example 2: Full Submission with Metadata

**Request:**
```bash
curl -X POST https://your-api-domain.com/api/submissions \
  -H "Content-Type: application/json" \
  -d '{
    "siteId": "my-site",
    "name": "Bob Johnson",
    "email": "bob@techsolutions.com",
    "message": "We need a custom web application for our business.",
    "clientIp": "203.0.113.45",
    "submittedAt": "2025-10-20T15:30:00Z",
    "metadata": {
      "phone": "555-9876",
      "company": "Tech Solutions Inc",
      "budget_range": "$50k-$100k",
      "project_type": "Web Application",
      "timeline": "3-6 months",
      "source": "WordPress Contact Form"
    }
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Submission created successfully",
  "submission": {
    "id": 125,
    "siteId": "my-site",
    "name": "Bob Johnson",
    "email": "bob@techsolutions.com",
    "message": "We need a custom web application for our business.",
    "clientIp": "203.0.113.45",
    "submittedAt": "2025-10-20T15:30:00Z",
    "metadata": {
      "phone": "555-9876",
      "company": "Tech Solutions Inc",
      "budget_range": "$50k-$100k",
      "project_type": "Web Application",
      "timeline": "3-6 months",
      "source": "WordPress Contact Form"
    },
    "createdAt": "2025-10-20T17:50:00Z"
  }
}
```

### Example 3: JavaScript/Fetch

```javascript
async function submitToAPI(formData) {
  try {
    const response = await fetch('https://your-api-domain.com/api/submissions?triggerWebhooks=true', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        siteId: 'my-site',
        name: formData.name,
        email: formData.email,
        message: formData.message,
        clientIp: formData.clientIp,
        metadata: {
          phone: formData.phone,
          company: formData.company,
          source: 'Custom Web Form'
        }
      ,
      triggerWebhooks: true
      })
    });
    
    const data = await response.json();
    
    if (data.success) {
      console.log('Submission created:', data.submission);
      return data.submission;
    } else {
      console.error('Error:', data.error);
      throw new Error(data.error);
    }
  } catch (error) {
    console.error('Failed to submit:', error);
    throw error;
  }
}
```

### Example 4: Python

```python
import requests
import json
from datetime import datetime

def create_submission(site_id, name, email, message, metadata=None):
  url = 'https://your-api-domain.com/api/submissions?triggerWebhooks=true'
    
    payload = {
        'siteId': site_id,
        'name': name,
        'email': email,
        'message': message,
        'submittedAt': datetime.utcnow().isoformat() + 'Z',
    'metadata': metadata or {},
    'triggerWebhooks': True
    }
    
    headers = {'Content-Type': 'application/json'}
    
    try:
        response = requests.post(url, json=payload, headers=headers)
        response.raise_for_status()
        
        data = response.json()
        if data['success']:
            print(f"Submission created: ID {data['submission']['id']}")
            return data['submission']
        else:
            print(f"Error: {data['error']}")
            return None
    except Exception as e:
        print(f"Failed to create submission: {e}")
        return None

# Usage
submission = create_submission(
    site_id='my-site',
    name='Alice Williams',
    email='alice@example.com',
    message='Question about pricing',
    metadata={
        'phone': '555-4321',
        'company': 'Example Corp',
        'source': 'Python Script'
    }
)
```

## WordPress Integration

### Contact Form 7

Add this to your theme's `functions.php` or a custom plugin:

```php
add_action('wpcf7_mail_sent', 'send_to_contact_forms_api');

function send_to_contact_forms_api($contact_form) {
    $submission = WPCF7_Submission::get_instance();
    
    if (!$submission) {
        return;
    }
    
    $posted_data = $submission->get_posted_data();
    
    // Prepare data for API
    $api_data = array(
        'siteId' => 'wordpress-site-1', // Configure your site ID
        'name' => isset($posted_data['your-name']) ? $posted_data['your-name'] : '',
        'email' => isset($posted_data['your-email']) ? $posted_data['your-email'] : '',
        'message' => isset($posted_data['your-message']) ? $posted_data['your-message'] : '',
        'clientIp' => $_SERVER['REMOTE_ADDR'],
        'submittedAt' => gmdate('Y-m-d\TH:i:s\Z'),
        'metadata' => array(
            'phone' => isset($posted_data['your-phone']) ? $posted_data['your-phone'] : '',
            'subject' => isset($posted_data['your-subject']) ? $posted_data['your-subject'] : '',
            'source' => 'WordPress Contact Form 7',
            'form_id' => $contact_form->id(),
            'form_title' => $contact_form->title(),
            'page_url' => home_url($_SERVER['REQUEST_URI']),
        )
    );
    
    // Remove empty metadata fields
    $api_data['metadata'] = array_filter($api_data['metadata']);
    
    // Send to API
  $response = wp_remote_post('https://your-api-domain.com/api/submissions?triggerWebhooks=true', array(
        'headers' => array('Content-Type' => 'application/json'),
        'body' => json_encode($api_data),
        'timeout' => 15
    ));
    
    // Log errors for debugging
    if (is_wp_error($response)) {
        error_log('Contact Forms API Error: ' . $response->get_error_message());
    }
}
```

### Gravity Forms
### WPForms (Webhooks Addon)

You can send nested metadata to the API using WPForms' Webhooks addon. There are two reliable approaches:

1) Raw JSON body (recommended)
- URL: https://your-api-domain.com/api/submissions?triggerWebhooks=true
- Method: POST
- Headers:
  - Content-Type: application/json
  - X-API-Key: your_api_key_here
- Body (Raw JSON):

```
{
  "siteId": "wordpress-site-1",
  "name": "{field_id_for_name}",
  "email": "{field_id_for_email}",
  "message": "{field_id_for_message}",
  "clientIp": "{user_ip}",
  "submittedAt": "{date format="Y-m-d\\TH:i:s\\Z"}",
  "metadata": {
    "phone": "{field_id_for_phone}",
    "company": "{field_id_for_company}",
    "source": "WPForms",
    "form_id": "{form_id}",
    "form_title": "{form_name}",
    "entry_id": "{entry_id}",
    "page_url": "{url_current}",
    "user_agent": "{user_agent}"
  },
  "triggerWebhooks": true
}
```

Notes:
- Replace {field_id_for_*} with the actual smart tags for your fields (e.g., {field_id="3"} or {field_id="3" first} depending on your setup).
- WPForms smart tag names can vary by version; use the tag selector in the Webhook UI to insert the correct tags.

2) Key/value pairs (form-encoded)
- If you prefer key/value mode instead of raw JSON, you can still build nested metadata by using bracket or dot notation in keys:
  - Key: metadata[phone]   Value: {field_id_for_phone}
  - Key: metadata[company] Value: {field_id_for_company}
  - Key: metadata[source]  Value: WPForms
  - Key: siteId            Value: wordpress-site-1
  - Key: name              Value: {field_id_for_name}
  - Key: email             Value: {field_id_for_email}
  - Key: message           Value: {field_id_for_message}
  - Key: clientIp          Value: {user_ip}
  - Key: triggerWebhooks   Value: true

Either metadata[phone] or metadata.phone will correctly bind to the server's nested metadata object. Ensure the Content-Type is set appropriately:
- application/x-www-form-urlencoded for key/value pairs
- application/json for raw JSON

Also add the X-API-Key header in the Webhook's Request Headers section.


```php
add_action('gform_after_submission', 'send_gravity_form_to_api', 10, 2);

function send_gravity_form_to_api($entry, $form) {
    // Get field values (adjust field IDs based on your form)
    $name = rgar($entry, '1'); // Name field
    $email = rgar($entry, '2'); // Email field
    $message = rgar($entry, '3'); // Message field
    $phone = rgar($entry, '4'); // Phone field (optional)
    
    $api_data = array(
        'siteId' => 'wordpress-site-1',
        'name' => $name,
        'email' => $email,
        'message' => $message,
        'clientIp' => $entry['ip'],
        'submittedAt' => gmdate('Y-m-d\TH:i:s\Z', strtotime($entry['date_created'])),
        'metadata' => array(
            'phone' => $phone,
            'source' => 'WordPress Gravity Forms',
            'form_id' => $form['id'],
            'form_title' => $form['title'],
            'entry_id' => $entry['id'],
        )
    );
    
    $api_data['metadata'] = array_filter($api_data['metadata']);
    
  wp_remote_post('https://your-api-domain.com/api/submissions?triggerWebhooks=true', array(
        'headers' => array('Content-Type' => 'application/json'),
        'body' => json_encode($api_data),
        'timeout' => 15
    ));
}
```

## Important Notes

1. **No Email Sent**: Unlike the standard form submission endpoint (`POST /v1/contact/{siteId}`), this API endpoint does NOT send email notifications. It only stores the submission in the database.

2. **Webhooks Support**: You can trigger configured webhooks by setting `triggerWebhooks` to `true` in the JSON body or by adding `?triggerWebhooks=true` to the URL.

3. **Site Validation**: The `siteId` must exist in the `sites` table. If it doesn't exist, you'll receive a 404 error.

4. **UTC Timestamps**: All timestamps should be in UTC format (ISO 8601). The `submittedAt` field defaults to current UTC time if not provided.

5. **Dynamic Metadata**: The metadata field is stored as JSONB in PostgreSQL, allowing for flexible querying and indexing of custom fields.

6. **Client IP**: If you don't provide a `clientIp`, it defaults to "API" to indicate the submission came through the API rather than directly from a user.

## Use Cases

### 1. WordPress Multi-Site Management
Track submissions from multiple WordPress sites in one centralized admin dashboard:
- Site A: `siteId: "client-a-wordpress"`
- Site B: `siteId: "client-b-wordpress"`
- Site C: `siteId: "client-c-wordpress"`

### 2. Historical Data Import
Import existing submissions from a CSV or database export:
```python
import csv
from datetime import datetime

with open('old_submissions.csv', 'r') as file:
    reader = csv.DictReader(file)
    for row in reader:
        create_submission(
            site_id='imported-data',
            name=row['name'],
            email=row['email'],
            message=row['message'],
            metadata={
                'original_date': row['date'],
                'source': 'Legacy System Import'
            }
        )
```

### 3. Third-Party Form Integration
Connect forms from services like Typeform, Jotform, or Google Forms using webhooks or Zapier.

### 4. Custom Application Forms
Build custom applications that need to store form submissions centrally without implementing their own database logic.

## Security Considerations

1. **API Authentication**: This endpoint requires an API key. Include `X-API-Key: your_api_key_here` in the headers to prevent unauthorized submissions.
2. **Rate Limiting**: Implement rate limiting if exposing publicly
3. **Input Validation**: All inputs are validated server-side, but consider adding client-side validation
4. **HTTPS Only**: Always use HTTPS in production to protect sensitive data
5. **Site Isolation**: Each site's submissions are isolated by `siteId` for multi-tenant security

## Future Enhancements

Potential future features for this endpoint:

- [ ] API key authentication
- [ ] Webhook triggering on submission creation
- [ ] Email notification support (optional flag)
- [ ] Bulk submission creation
- [ ] Attachment/file upload support
- [ ] Spam detection integration
- [ ] Custom validation rules per site
