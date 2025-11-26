/**
 * Tailwind CSS Configuration for LocalScout
 * This configuration is used with the CDN version of Tailwind CSS
 * 
 * To use: Add this script tag before the Tailwind CDN in your HTML:
 * <script src="~/tailwind.config.js"></script>
 * 
 * For CDN usage, Tailwind looks for window.tailwind.config
 */

if (typeof window !== 'undefined') {
    window.tailwind = window.tailwind || {};
    window.tailwind.config = {
        // Prevent Tailwind from conflicting with Bootstrap
        prefix: 'tw-',
        
        // Important to override Bootstrap styles when needed
        important: false,
        
        theme: {
            extend: {
                colors: {
                    // LocalScout brand colors
                    primary: {
                        DEFAULT: '#3f72af',
                        dark: '#112d4e',
                        light: '#dbe2ef',
                    },
                    background: '#f9f7f7',
                },
                // Add custom spacing if needed
                spacing: {
                    '128': '32rem',
                    '144': '36rem',
                },
            },
        },
        
        // Disable preflight to avoid conflicts with Bootstrap
        corePlugins: {
            preflight: false,
        },
        
        // Content paths (not used with CDN but good for reference)
        content: [
            './Views/**/*.cshtml',
            './Areas/**/*.cshtml',
            './wwwroot/js/**/*.js',
        ],
    };
}
