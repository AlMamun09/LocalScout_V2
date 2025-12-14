/**
 * AI Description Generator Module
 * Reusable module for generating AI-powered descriptions from form data.
 */
const AIGenerator = (function () {
    const API_URL = '/AI/GenerateDescription';

    /**
     * Generate description from form fields
     * @param {Object} options Configuration options
     * @param {Object} options.fields - Field mappings { displayName: inputSelector }
     * @param {string} options.targetTextarea - Selector for target textarea
     * @param {string} options.buttonSelector - Selector for the generate button
     * @param {string} options.type - Type of description: 'provider' or 'service'
     */
    function init(options) {
        const button = document.querySelector(options.buttonSelector);
        if (!button) return;

        button.addEventListener('click', () => generate(options));
    }

    /**
     * Generate description
     */
    async function generate(options) {
        const button = document.querySelector(options.buttonSelector);
        const textarea = document.querySelector(options.targetTextarea);
        const icon = button.querySelector('i');
        const textSpan = button.querySelector('span') || button;

        if (!textarea) return;

        // Collect context from form fields
        const context = {};
        for (const [displayName, selector] of Object.entries(options.fields)) {
            const element = document.querySelector(selector);
            if (element) {
                // Handle select elements - get the selected text, not value
                if (element.tagName === 'SELECT') {
                    const selectedOption = element.options[element.selectedIndex];
                    context[displayName] = selectedOption ? selectedOption.text : '';
                } else {
                    context[displayName] = element.value || '';
                }
            }
        }

        // Check if any required field has data
        const hasData = Object.values(context).some(val => val && val.trim() !== '' && val !== '-- Select Category --');
        if (!hasData) {
            Swal.fire({
                icon: 'warning',
                title: 'Missing Information',
                text: 'Please fill in some form fields first before generating a description.',
                confirmButtonColor: '#3085d6'
            });
            return;
        }

        // Show loading state
        const originalIcon = icon ? icon.className : '';
        const originalText = textSpan.textContent || textSpan.innerText;
        
        button.disabled = true;
        if (icon) icon.className = 'fas fa-spinner fa-spin';
        if (textSpan !== button) {
            textSpan.textContent = 'Generating...';
        }

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            // Add timeout using AbortController
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout
            
            const response = await fetch(API_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify({
                    context: context,
                    type: options.type
                }),
                signal: controller.signal
            });
            
            clearTimeout(timeoutId);

            const data = await response.json();

            if (data.success && data.description) {
                textarea.value = data.description;
                textarea.dispatchEvent(new Event('input', { bubbles: true }));
                
                // Change button to regenerate state
                if (icon) icon.className = 'fas fa-sync-alt';
                if (textSpan !== button) {
                    textSpan.textContent = 'Regenerate';
                }
                button.classList.remove('btn-outline-primary');
                button.classList.add('btn-outline-success');
            } else {
                throw new Error(data.message || 'Failed to generate description');
            }
        } catch (error) {
            console.error('AI Generation Error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Generation Failed',
                text: error.message || 'Failed to generate description. Please try again or write manually.',
                confirmButtonColor: '#d33'
            });
            
            // Restore original button state on error
            if (icon) icon.className = originalIcon;
            if (textSpan !== button) {
                textSpan.textContent = originalText;
            }
        } finally {
            button.disabled = false;
        }
    }

    return { init, generate };
})();

// Export for ES6 modules if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIGenerator;
}

if (typeof window !== 'undefined') {
    window.AIGenerator = AIGenerator;
}
