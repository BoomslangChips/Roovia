// roovia-layout.js
// Sidebar, user menu and theme behaviour for MainLayout / NavMenu.
// Loaded once from App.razor; rooviaLayout.init() re-applies saved state whenever the layout
// is (re)rendered: by the interactive circuit, a full page load, or an enhanced navigation.

(function () {
    const MOBILE_BREAKPOINT = 991;
    const THEME_KEY = 'theme';
    const SIDEBAR_KEY = 'sidebar_state';

    function readSetting(key) {
        try { return localStorage.getItem(key); } catch { return null; }
    }

    function writeSetting(key, value) {
        try { localStorage.setItem(key, value); } catch { /* storage unavailable */ }
    }

    function isMobile() {
        return window.innerWidth <= MOBILE_BREAKPOINT;
    }

    function setIcon(icon, from, to) {
        if (!icon) return;
        icon.classList.remove(from);
        icon.classList.add(to);
    }

    function setSidebarIcons(collapsed) {
        const from = collapsed ? 'fa-angle-double-left' : 'fa-angle-double-right';
        const to = collapsed ? 'fa-angle-double-right' : 'fa-angle-double-left';
        setIcon(document.getElementById('desktop-toggle-icon'), from, to);
        setIcon(document.getElementById('sidebar-toggle-icon'), from, to);
    }

    function applyTheme() {
        const dark = readSetting(THEME_KEY) === 'dark';
        document.documentElement.classList.toggle('theme-dark', dark);
        setIcon(document.getElementById('theme-toggle-icon'), dark ? 'fa-moon' : 'fa-sun', dark ? 'fa-sun' : 'fa-moon');
    }

    // ====== Theme Toggle ======
    window.toggleTheme = function () {
        writeSetting(THEME_KEY, document.documentElement.classList.contains('theme-dark') ? 'light' : 'dark');
        applyTheme();
    };

    // ====== Toggle sidebar collapsed state ======
    window.toggleSidebar = function (event) {
        if (event) event.stopPropagation();

        const sidebarContainer = document.getElementById('sidebarContainer');
        if (!sidebarContainer) return;

        const collapsed = !sidebarContainer.classList.contains('collapsed');
        sidebarContainer.classList.toggle('collapsed', collapsed);
        writeSetting(SIDEBAR_KEY, collapsed ? 'collapsed' : 'expanded');
        setSidebarIcons(collapsed);

        // Close user menu when sidebar state changes
        window.closeUserMenu();
    };

    // ====== Toggle mobile menu ======
    window.toggleMobileMenu = function (event) {
        if (event) event.stopPropagation();

        const sidebarContainer = document.getElementById('sidebarContainer');
        if (!sidebarContainer) return;

        sidebarContainer.classList.toggle('mobile-open');

        // Close user menu when mobile menu toggles
        window.closeUserMenu();
    };

    // Function for NavMenu to close mobile menu
    window.closeNavMenuMobile = function (event) {
        if (event) event.stopPropagation();

        document.getElementById('sidebarContainer')?.classList.remove('mobile-open');

        // Close user menu when mobile menu closes
        window.closeUserMenu();
    };

    // ====== User Menu Functions ======
    function removeUserMenuBackdrop() {
        const backdrop = document.getElementById('userMenuBackdrop');
        if (backdrop) {
            backdrop.removeEventListener('click', window.closeUserMenu);
            backdrop.remove();
        }
    }

    function createUserMenuBackdrop() {
        removeUserMenuBackdrop();

        const backdrop = document.createElement('div');
        backdrop.className = 'roovia-user-menu-backdrop show';
        backdrop.id = 'userMenuBackdrop';
        backdrop.addEventListener('click', window.closeUserMenu);

        document.body.appendChild(backdrop);
    }

    window.toggleUserMenu = function (event) {
        if (event) event.stopPropagation();

        const userMenu = document.getElementById('userMenu');
        if (!userMenu) return;

        const open = !userMenu.classList.contains('show');
        userMenu.classList.toggle('show', open);
        document.querySelector('.roovia-user-dropdown-icon')?.classList.toggle('active', open);

        if (open) {
            createUserMenuBackdrop();
        } else {
            removeUserMenuBackdrop();
        }
    };

    window.closeUserMenu = function () {
        document.getElementById('userMenu')?.classList.remove('show');
        document.querySelector('.roovia-user-dropdown-icon')?.classList.remove('active');
        removeUserMenuBackdrop();
    };

    // ====== Layout initialisation (safe to call repeatedly) ======
    function init() {
        applyTheme();

        const sidebarContainer = document.getElementById('sidebarContainer');
        if (!sidebarContainer || sidebarContainer.dataset.rooviaLayoutReady) return;
        sidebarContainer.dataset.rooviaLayoutReady = 'true';

        // Restore the collapsed sidebar ONLY on desktop
        if (!isMobile() && readSetting(SIDEBAR_KEY) === 'collapsed') {
            sidebarContainer.classList.add('collapsed');
            setSidebarIcons(true);
        }
    }

    window.rooviaLayout = { init };

    // Theme first, before the body paints, to avoid a light/dark flash
    applyTheme();

    // Document-level listeners are bound once and look elements up on demand,
    // so they keep working after the layout is re-rendered
    document.addEventListener('click', function (e) {
        const sidebarContainer = document.getElementById('sidebarContainer');
        const mobileToggle = document.getElementById('mobile-menu-toggle');

        // Close mobile menu when clicking outside (but not on the toggle button)
        if (isMobile() &&
            sidebarContainer?.classList.contains('mobile-open') &&
            !mobileToggle?.contains(e.target) &&
            !sidebarContainer.contains(e.target)) {
            sidebarContainer.classList.remove('mobile-open');
        }

        // Close user menu when clicking outside
        const userMenu = document.getElementById('userMenu');
        const userProfile = document.querySelector('.roovia-user-profile');
        if (userMenu?.classList.contains('show') &&
            !userMenu.contains(e.target) &&
            !userProfile?.contains(e.target)) {
            window.closeUserMenu();
        }
    });

    // Close menus on ESC key
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;

        window.closeUserMenu();
        document.getElementById('sidebarContainer')?.classList.remove('mobile-open');
    });

    // Responsive behavior
    window.addEventListener('resize', function () {
        const sidebarContainer = document.getElementById('sidebarContainer');

        if (sidebarContainer) {
            // Auto-close mobile menu on resize to desktop
            if (!isMobile()) {
                sidebarContainer.classList.remove('mobile-open');
            }

            // Reset sidebar state on mobile
            if (isMobile() && sidebarContainer.classList.contains('collapsed')) {
                sidebarContainer.classList.remove('collapsed');
                setSidebarIcons(false);
            }
        }

        // Close user menu on resize
        window.closeUserMenu();
    });

    document.addEventListener('DOMContentLoaded', function () {
        init();
        // Static (server-rendered) pages are patched in place by enhanced navigation
        window.Blazor?.addEventListener?.('enhancedload', init);
    });
})();
