// theme-manager.js - Bulletproof theme persistence system

(function () {
    // Theme constants
    const THEME_KEY = 'roovia_theme';
    const THEME_DARK = 'dark';
    const THEME_LIGHT = 'light';
    const THEME_CLASS = 'theme-dark';

    // Global theme tracker (in-memory state)
    let currentTheme = null;

    // Core functions
    const ThemeManager = {
        // Get theme with multiple fallbacks
        getTheme: function () {
            // First try localStorage
            let theme = localStorage.getItem(THEME_KEY);

            // Then try sessionStorage as fallback
            if (!theme) {
                theme = sessionStorage.getItem(THEME_KEY);
            }

            // Check for theme in data attribute as another fallback
            if (!theme && document.documentElement.dataset.theme) {
                theme = document.documentElement.dataset.theme;
            }

            // Default to system preference if still no theme
            if (!theme) {
                const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
                theme = prefersDark ? THEME_DARK : THEME_LIGHT;
                // But don't save this as explicit preference
            }

            return theme;
        },

        // Save theme to multiple storage locations for redundancy
        saveTheme: function (theme) {
            try {
                localStorage.setItem(THEME_KEY, theme);
                sessionStorage.setItem(THEME_KEY, theme);
                document.documentElement.dataset.theme = theme;
                currentTheme = theme;

                // Also set a backup cookie (expires in 1 year)
                const oneYear = new Date();
                oneYear.setFullYear(oneYear.getFullYear() + 1);
                document.cookie = `${THEME_KEY}=${theme}; expires=${oneYear.toUTCString()}; path=/; SameSite=Strict`;
            } catch (e) {
                console.error('Error saving theme preference:', e);
            }
        },

        // Apply theme to document with all visual updates
        applyTheme: function (theme) {
            // Always remove first then add if needed (prevents multiple additions)
            document.documentElement.classList.remove(THEME_CLASS);

            if (theme === THEME_DARK) {
                document.documentElement.classList.add(THEME_CLASS);
            }

            // Update all theme toggle icons in document
            this.updateAllThemeIcons(theme);

            // Store the newly applied theme
            this.saveTheme(theme);

            // Dispatch event for components to react
            document.dispatchEvent(new CustomEvent('themeChanged', {
                detail: { theme }
            }));
        },

        // Toggle between themes
        toggleTheme: function () {
            const currentTheme = this.getTheme();
            const newTheme = currentTheme === THEME_DARK ? THEME_LIGHT : THEME_DARK;
            this.applyTheme(newTheme);
            return newTheme;
        },

        // Update all theme toggle icons in the document
        updateAllThemeIcons: function (theme) {
            // Get all theme icons (could be multiple in document)
            const themeIcons = document.querySelectorAll('.theme-toggle-icon, #theme-toggle-icon');

            themeIcons.forEach(icon => {
                if (theme === THEME_DARK) {
                    icon.classList.remove('fa-moon');
                    icon.classList.add('fa-sun');
                } else {
                    icon.classList.remove('fa-sun');
                    icon.classList.add('fa-moon');
                }
            });
        },

        // Initialize theme system
        initialize: function () {
            const theme = this.getTheme();
            this.applyTheme(theme);

            // Set up listeners for preference changes
            this.setupListeners();

            // Set up mutation observer to detect DOM changes
            this.setupObserver();

            // Set up periodic verification
            this.setupVerification();

            return theme;
        },

        // Set up all event listeners
        setupListeners: function () {
            // Listen for system theme preference changes
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
                // Only auto-switch if user hasn't explicitly chosen a theme
                if (!localStorage.getItem(THEME_KEY)) {
                    const theme = e.matches ? THEME_DARK : THEME_LIGHT;
                    this.applyTheme(theme);
                }
            });

            // Handle Blazor navigation/routing events
            if (window.Blazor) {
                setTimeout(() => {
                    if (window.Blazor.navigateTo) {
                        const originalNavigateTo = window.Blazor.navigateTo;
                        window.Blazor.navigateTo = function () {
                            const result = originalNavigateTo.apply(this, arguments);
                            // Re-apply theme after navigation
                            ThemeManager.verifyTheme();
                            return result;
                        };
                    }
                }, 1000); // Delay to ensure Blazor is fully initialized
            }

            // Listen for storage changes from other tabs
            window.addEventListener('storage', e => {
                if (e.key === THEME_KEY) {
                    this.applyTheme(e.newValue);
                }
            });

            // Listen for visibility changes (tab becomes active)
            document.addEventListener('visibilitychange', () => {
                if (document.visibilityState === 'visible') {
                    this.verifyTheme();
                }
            });
        },

        // Set up mutation observer to detect theme-relevant DOM changes
        setupObserver: function () {
            const observer = new MutationObserver(mutations => {
                // Check if theme toggle icons were added or class was changed
                let shouldUpdateIcons = false;

                for (const mutation of mutations) {
                    // If HTML element class was modified
                    if (mutation.type === 'attributes' &&
                        mutation.attributeName === 'class' &&
                        mutation.target === document.documentElement) {
                        shouldUpdateIcons = true;
                        break;
                    }

                    // If new nodes were added that might contain icons
                    if (mutation.type === 'childList' && mutation.addedNodes.length) {
                        for (const node of mutation.addedNodes) {
                            if (node.nodeType === 1 && (
                                node.querySelector('.theme-toggle-icon, #theme-toggle-icon') ||
                                node.id === 'theme-toggle-icon' ||
                                node.classList.contains('theme-toggle-icon')
                            )) {
                                shouldUpdateIcons = true;
                                break;
                            }
                        }
                    }
                }

                if (shouldUpdateIcons) {
                    this.updateAllThemeIcons(this.getTheme());
                }

                // Verify theme class is correctly applied
                this.verifyTheme();
            });

            // Observe the entire document for changes
            observer.observe(document, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['class']
            });
        },

        // Set up periodic verification to prevent any reversion
        setupVerification: function () {
            // Check theme every 2 seconds as a safety measure
            setInterval(() => this.verifyTheme(), 2000);

            // Also verify on any dynamic changes
            window.addEventListener('load', () => this.verifyTheme());
            window.addEventListener('resize', () => this.verifyTheme());
            window.addEventListener('DOMContentLoaded', () => this.verifyTheme());
        },

        // Verify theme is correctly applied and fix if not
        verifyTheme: function () {
            const savedTheme = this.getTheme();
            const isDarkMode = document.documentElement.classList.contains(THEME_CLASS);

            // If there's a mismatch between saved theme and applied theme
            if ((savedTheme === THEME_DARK && !isDarkMode) ||
                (savedTheme === THEME_LIGHT && isDarkMode)) {
                console.log('Theme mismatch detected, fixing...');
                this.applyTheme(savedTheme);
            }

            // Also update icons if they're out of sync
            this.updateAllThemeIcons(savedTheme);
        }
    };

    // Make globally available
    window.ThemeManager = ThemeManager;

    // Auto-initialize as early as possible
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => ThemeManager.initialize());
    } else {
        ThemeManager.initialize();
    }

    // Replace the existing toggleTheme function
    window.toggleTheme = function () {
        return ThemeManager.toggleTheme();
    };
})();