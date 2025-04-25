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