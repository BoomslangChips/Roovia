// modalHelpers.js
// Add this to your wwwroot/js folder and reference it in _Host.cshtml

window.addModalKeyListener = (dotnetRef) => {
    window.modalKeyListener = (e) => {
        if (e.key === 'Escape') {
            dotnetRef.invokeMethodAsync('HandleKeyDown', 'Escape');
        }
    };
    document.addEventListener('keydown', window.modalKeyListener);
};

window.removeModalKeyListener = () => {
    if (window.modalKeyListener) {
        document.removeEventListener('keydown', window.modalKeyListener);
    }
};

// Helper to prevent body scrolling when modal is open
window.preventBodyScroll = (isPrevent) => {
    if (isPrevent) {
        document.body.style.overflow = 'hidden';
    } else {
        document.body.style.overflow = '';
    }
};

// Loads heavy third-party assets on demand (each URL only once) instead of on every page
window.rooviaAssets = (() => {
    const loads = new Map();

    function loadOnce(url, createElement) {
        if (!url) return Promise.resolve();

        if (!loads.has(url)) {
            loads.set(url, new Promise((resolve, reject) => {
                const element = createElement(url);
                element.onload = () => resolve();
                element.onerror = () => {
                    // Allow a later attempt to try again
                    loads.delete(url);
                    element.remove();
                    reject(new Error(`Failed to load ${url}`));
                };
                document.head.appendChild(element);
            }));
        }

        return loads.get(url);
    }

    function script(url) {
        const element = document.createElement('script');
        element.src = url;
        return element;
    }

    function stylesheet(url) {
        const element = document.createElement('link');
        element.rel = 'stylesheet';
        element.href = url;
        return element;
    }

    return {
        load: (scriptUrl, stylesheetUrl) => Promise.all([
            loadOnce(stylesheetUrl, stylesheet),
            loadOnce(scriptUrl, script)
        ])
    };
})();