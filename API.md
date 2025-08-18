# RLS Contact Form JavaScript API

A lightweight JavaScript library for integrating with the RLS Contact Form service. Makes it easy to add contact forms to any website with just a few lines of code.

## Quick Start

### 1. Include the Script

```html
<script src="https://your-cdn.com/rls-contact-api.min.js"></script>
```

### 2. Initialize with Your Site ID

```html
<script>
RLSContact.init({
    siteId: 'your-site-id'
});
</script>
```

### 3. Add a Contact Form

```html
<form data-rls-contact="your-site-id">
    <!-- Honeypot field (required for spam protection) -->
    <input type="text" name="_hp" style="display:none;" tabindex="-1" autocomplete="off">
    
    <div>
        <label>Name</label>
        <input type="text" name="name" required>
    </div>
    
    <div>
        <label>Email</label>
        <input type="email" name="email" required>
    </div>
    
    <div>
        <label>Message</label>
        <textarea name="message" required></textarea>
    </div>
    
    <button type="submit">Send Message</button>
</form>
```

That's it! The form will automatically submit to your RLS Contact service.

## Configuration Options

### RLSContact.init(options)

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `siteId` | string | **required** | Your site identifier |
| `apiUrl` | string | `https://rls-contact-form-d3bbb0f6avhtgxb5.eastus-01.azurewebsites.net` | API endpoint URL |
| `useRedirect` | boolean | `true` | Whether to follow server redirects |
| `debug` | boolean | `false` | Enable console logging |

### Example with all options:

```javascript
RLSContact.init({
    siteId: 'your-site-id',
    apiUrl: 'https://your-custom-api.com',
    useRedirect: true,
    debug: true
});
```

## Form Data Attributes

Add these attributes to your `<form>` element for customization:

| Attribute | Description | Example |
|-----------|-------------|---------|
| `data-rls-contact` | Site ID for this form | `data-rls-contact="your-site-id"` |
| `data-rls-redirect` | Disable redirect (use "false") | `data-rls-redirect="false"` |
| `data-rls-on-success` | Custom success function name | `data-rls-on-success="mySuccessHandler"` |
| `data-rls-on-error` | Custom error function name | `data-rls-on-error="myErrorHandler"` |

## Custom Success/Error Handling

### Using Data Attributes

```html
<form data-rls-contact="your-site-id" 
      data-rls-on-success="handleSuccess" 
      data-rls-on-error="handleError">
    <!-- form fields -->
</form>

<script>
function handleSuccess(result) {
    alert('Message sent successfully!');
}

function handleError(error) {
    alert('Error: ' + error.message);
}
</script>
```

### Programmatic Usage

```javascript
// Submit data directly
RLSContact.submit({
    name: 'John Doe',
    email: 'john@example.com',
    message: 'Hello from the API!',
    company: 'Acme Corp'
}, {
    useRedirect: false
}).then(result => {
    console.log('Success:', result);
}).catch(error => {
    console.log('Error:', error);
});

// Submit a form element
const form = document.getElementById('my-form');
RLSContact.submitForm(form, { useRedirect: false })
    .then(result => console.log('Success'))
    .catch(error => console.log('Error'));
```

## Form Fields

### Required Fields
- `email` - Recipient email address
- `message` - Message content

### Optional Fields
- `name` - Sender name
- `_hp` - Honeypot field (include but keep hidden)
- Any other fields (will be included as metadata)

### Custom Metadata
You can include any additional fields and they'll be included in the email:

```html
<input type="text" name="company" placeholder="Company Name">
<input type="tel" name="phone_number" placeholder="Phone">
<select name="budget_range">
    <option value="Under $5,000">Under $5,000</option>
    <option value="$5,000+">$5,000+</option>
</select>
```

## Advanced Examples

### Multiple Forms on One Page

```html
<!-- Contact form -->
<form data-rls-contact="your-site-id" data-rls-on-success="contactSuccess">
    <!-- fields -->
</form>

<!-- Quote request form -->
<form data-rls-contact="your-site-id" data-rls-on-success="quoteSuccess">
    <input type="hidden" name="form_type" value="quote">
    <!-- fields -->
</form>
```

### Custom Loading States

```html
<form data-rls-contact="your-site-id" data-rls-on-success="customSuccess">
    <!-- fields -->
    <button type="submit" id="submit-btn">
        <span class="btn-text">Send Message</span>
        <span class="btn-loading" style="display:none;">Sending...</span>
    </button>
</form>

<script>
function customSuccess(result) {
    // Show custom success message
    document.getElementById('success-banner').style.display = 'block';
}
</script>
```

### Validation Before Submit

```javascript
// Attach manually with custom validation
const form = document.getElementById('my-form');

form.addEventListener('submit', (e) => {
    e.preventDefault();
    
    const formData = RLSContact.extractFormData(form);
    const validation = RLSContact.validate(formData);
    
    if (!validation.isValid) {
        alert('Please fix: ' + validation.errors.join(', '));
        return;
    }
    
    RLSContact.submitForm(form);
});
```

## API Methods

### RLSContact.submit(formData, options)
Submit form data programmatically.

### RLSContact.submitForm(formElement, options)
Submit a form element.

### RLSContact.extractFormData(formElement)
Extract data from form element as object.

### RLSContact.validate(formData)
Validate form data (returns `{isValid: boolean, errors: string[]}`).

### RLSContact.attachToForm(formElement, options)
Manually attach event listener to form.

## Browser Support
- Modern browsers (Chrome, Firefox, Safari, Edge)
- IE11+ (with fetch polyfill)

## File Sizes
- Full version: ~8KB
- Minified version: ~3KB
- Gzipped: ~1.2KB

## Security Features
- Honeypot spam protection (include `_hp` field)
- CORS protection
- Rate limiting
- Input validation
- HMAC signature support (when configured)

## Troubleshooting

### Form not submitting
1. Check that `siteId` is configured correctly
2. Ensure the honeypot field `_hp` is included
3. Verify required fields (`email`, `message`) are present
4. Check browser console for errors

### CORS errors
Contact your administrator to add your domain to the allowed origins list.

### Custom styling
The API adds CSS classes for styling messages:
- `.rls-contact-message` - Base message class
- `.rls-contact-success` - Success message
- `.rls-contact-error` - Error message

```css
.rls-contact-message {
    padding: 10px;
    margin: 10px 0;
    border-radius: 4px;
}

.rls-contact-success {
    background-color: #d4edda;
    color: #155724;
    border: 1px solid #c3e6cb;
}

.rls-contact-error {
    background-color: #f8d7da;
    color: #721c24;
    border: 1px solid #f5c6cb;
}
```