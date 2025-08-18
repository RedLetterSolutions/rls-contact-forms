/**
 * RLS Contact Form API
 * A simple JavaScript library for integrating with the RLS Contact Form service
 * 
 * Usage:
 * <script src="https://your-cdn.com/rls-contact-api.js"></script>
 * <script>
 *   RLSContact.init({
 *     siteId: 'your-site-id',
 *     apiUrl: 'https://rls-contact-form-d3bbb0f6avhtgxb5.eastus-01.azurewebsites.net'
 *   });
 * </script>
 */

(function(window) {
    'use strict';

    const RLSContact = {
        config: {
            apiUrl: 'https://rls-contact-form-d3bbb0f6avhtgxb5.eastus-01.azurewebsites.net',
            siteId: null,
            useRedirect: true,
            debug: false
        },

        /**
         * Initialize the RLS Contact API
         * @param {Object} options - Configuration options
         * @param {string} options.siteId - Your site ID
         * @param {string} [options.apiUrl] - API base URL (optional)
         * @param {boolean} [options.useRedirect=true] - Whether to use redirect flow or return promise
         * @param {boolean} [options.debug=false] - Enable debug logging
         */
        init: function(options) {
            if (!options || !options.siteId) {
                throw new Error('RLSContact: siteId is required');
            }

            this.config = Object.assign(this.config, options);
            this.log('Initialized with config:', this.config);

            // Auto-attach to forms with data-rls-contact attribute
            this.autoAttach();
        },

        /**
         * Submit a contact form
         * @param {Object} formData - Form data object
         * @param {Object} [options] - Submission options
         * @returns {Promise} - Promise that resolves with submission result
         */
        submit: function(formData, options = {}) {
            const config = Object.assign({}, this.config, options);
            
            if (!config.siteId) {
                throw new Error('RLSContact: siteId not configured');
            }

            this.log('Submitting form data:', formData);

            const url = `${config.apiUrl}/api/v1/contact/${config.siteId}`;
            const urlEncodedData = new URLSearchParams();

            // Convert form data to URL-encoded format
            for (const [key, value] of Object.entries(formData)) {
                if (value !== null && value !== undefined) {
                    urlEncodedData.append(key, value);
                }
            }

            // Add honeypot field (empty by default)
            if (!formData._hp) {
                urlEncodedData.append('_hp', '');
            }

            return fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: urlEncodedData.toString()
            }).then(response => {
                this.log('Response received:', response);

                if (response.ok) {
                    // Handle redirect response
                    if (response.status === 303 && config.useRedirect) {
                        const location = response.headers.get('Location');
                        if (location) {
                            window.location.href = location;
                            return { success: true, redirected: true, location: location };
                        }
                    }
                    return { success: true, status: response.status };
                } else {
                    return response.text().then(errorText => {
                        throw new Error(`Server error: ${response.status} - ${errorText}`);
                    });
                }
            });
        },

        /**
         * Submit a form element
         * @param {HTMLFormElement} form - Form element to submit
         * @param {Object} [options] - Submission options
         * @returns {Promise} - Promise that resolves with submission result
         */
        submitForm: function(form, options = {}) {
            const formData = this.extractFormData(form);
            return this.submit(formData, options);
        },

        /**
         * Extract data from a form element
         * @param {HTMLFormElement} form - Form element
         * @returns {Object} - Form data object
         */
        extractFormData: function(form) {
            const formData = new FormData(form);
            const data = {};

            for (const [key, value] of formData.entries()) {
                data[key] = value;
            }

            return data;
        },

        /**
         * Automatically attach to forms with data-rls-contact attribute
         */
        autoAttach: function() {
            const forms = document.querySelectorAll('form[data-rls-contact]');
            
            forms.forEach(form => {
                this.log('Auto-attaching to form:', form);
                this.attachToForm(form);
            });
        },

        /**
         * Attach event listener to a form
         * @param {HTMLFormElement} form - Form element
         * @param {Object} [options] - Options for this specific form
         */
        attachToForm: function(form, options = {}) {
            const config = Object.assign({}, this.config, options);
            
            // Get options from data attributes
            const siteId = form.dataset.rlsContact || config.siteId;
            const useRedirect = form.dataset.rlsRedirect !== 'false';
            const onSuccess = form.dataset.rlsOnSuccess;
            const onError = form.dataset.rlsOnError;

            form.addEventListener('submit', (e) => {
                e.preventDefault();

                // Show loading state if there's a submit button
                const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
                const originalText = submitBtn ? submitBtn.textContent : '';
                if (submitBtn) {
                    submitBtn.disabled = true;
                    submitBtn.textContent = 'Sending...';
                }

                this.submitForm(form, { 
                    siteId: siteId, 
                    useRedirect: useRedirect 
                })
                .then(result => {
                    this.log('Form submitted successfully:', result);
                    
                    if (result.redirected) {
                        // Redirect happened, no need to do anything else
                        return;
                    }

                    // Reset form and show success
                    form.reset();
                    
                    if (onSuccess) {
                        // Call custom success function
                        if (typeof window[onSuccess] === 'function') {
                            window[onSuccess](result);
                        }
                    } else {
                        // Default success behavior
                        this.showMessage(form, 'Message sent successfully!', 'success');
                    }
                })
                .catch(error => {
                    this.log('Form submission failed:', error);
                    
                    if (onError) {
                        // Call custom error function
                        if (typeof window[onError] === 'function') {
                            window[onError](error);
                        }
                    } else {
                        // Default error behavior
                        this.showMessage(form, error.message, 'error');
                    }
                })
                .finally(() => {
                    // Restore submit button
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.textContent = originalText;
                    }
                });
            });
        },

        /**
         * Show a message near the form
         * @param {HTMLFormElement} form - Form element
         * @param {string} message - Message to show
         * @param {string} type - Message type ('success' or 'error')
         */
        showMessage: function(form, message, type) {
            // Remove existing message
            const existingMessage = form.querySelector('.rls-contact-message');
            if (existingMessage) {
                existingMessage.remove();
            }

            // Create new message
            const messageEl = document.createElement('div');
            messageEl.className = `rls-contact-message rls-contact-${type}`;
            messageEl.textContent = message;
            messageEl.style.cssText = `
                padding: 10px;
                margin: 10px 0;
                border-radius: 4px;
                border: 1px solid;
                ${type === 'success' ? 
                    'background-color: #d4edda; border-color: #c3e6cb; color: #155724;' :
                    'background-color: #f8d7da; border-color: #f5c6cb; color: #721c24;'
                }
            `;

            // Insert after form
            form.parentNode.insertBefore(messageEl, form.nextSibling);

            // Auto-remove after 5 seconds
            setTimeout(() => {
                if (messageEl.parentNode) {
                    messageEl.remove();
                }
            }, 5000);
        },

        /**
         * Validate required form fields
         * @param {Object} formData - Form data to validate
         * @returns {Object} - Validation result
         */
        validate: function(formData) {
            const errors = [];

            if (!formData.email || !formData.email.trim()) {
                errors.push('Email is required');
            } else if (!this.isValidEmail(formData.email)) {
                errors.push('Please enter a valid email address');
            }

            if (!formData.message || !formData.message.trim()) {
                errors.push('Message is required');
            }

            return {
                isValid: errors.length === 0,
                errors: errors
            };
        },

        /**
         * Simple email validation
         * @param {string} email - Email to validate
         * @returns {boolean} - Whether email is valid
         */
        isValidEmail: function(email) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            return emailRegex.test(email);
        },

        /**
         * Debug logging
         * @param {...any} args - Arguments to log
         */
        log: function(...args) {
            if (this.config.debug) {
                console.log('[RLSContact]', ...args);
            }
        }
    };

    // Expose to global scope
    window.RLSContact = RLSContact;

    // Auto-initialize if config is present
    document.addEventListener('DOMContentLoaded', function() {
        if (window.RLSContactConfig) {
            RLSContact.init(window.RLSContactConfig);
        }
    });

})(window);