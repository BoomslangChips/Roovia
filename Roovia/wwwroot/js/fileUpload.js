// wwwroot/js/fileUpload.js
window.addDragDropListeners = (dotNetRef) => {
    const preventDefaults = (e) => {
        e.preventDefault();
        e.stopPropagation();
    };

    document.addEventListener('dragenter', (e) => {
        preventDefaults(e);
        if (e.dataTransfer.types.includes('Files')) {
            dotNetRef.invokeMethodAsync('OnDragEnter');
        }
    });

    document.addEventListener('dragleave', (e) => {
        preventDefaults(e);
        if (e.target.classList && e.target.classList.contains('file-upload-container')) {
            dotNetRef.invokeMethodAsync('OnDragLeave');
        }
    });

    document.addEventListener('dragover', preventDefaults);
    document.addEventListener('drop', preventDefaults);
};