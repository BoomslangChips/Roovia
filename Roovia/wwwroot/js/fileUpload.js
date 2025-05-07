// fileUpload.js - Enhanced file handling functions

/**
 * Adds drag and drop event listeners to the file upload component
 * @param {any} dotNetHelper - .NET reference to the component
 */
window.addDragDropListeners = function (dotNetHelper) {
    const dropZone = document.querySelector('.file-upload-container');
    if (!dropZone) return;

    // Prevent default drag behaviors
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });

    // Handle drag enter/leave events
    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dotNetHelper.invokeMethodAsync('OnDragEnter');
        }, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => {
            dotNetHelper.invokeMethodAsync('OnDragLeave');
        }, false);
    });

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }
};

/**
 * Opens a file URL with API key authentication
 * Enhanced to properly handle different file types and optimize viewing experience
 * @param {string} url - File URL
 * @param {string} apiKey - API key for authentication
 */
window.openUrlWithApiKey = function (url, apiKey) {
    if (!url) return;

    // Determine file type from URL
    const extension = url.split('.').pop().toLowerCase();
    const isImage = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'svg', 'webp'].includes(extension);
    const isPdf = extension === 'pdf';
    const isMedia = ['mp4', 'webm', 'mp3', 'wav'].includes(extension);

    // Add API key to URL if not already present
    const hasParams = url.includes('?');
    const separator = hasParams ? '&' : '?';
    let viewUrl = url;

    // Check if URL is from the CDN domain or needs to go through the API
    const cdnDomain = new URL(url).hostname;
    const appDomain = window.location.hostname;

    if (cdnDomain === appDomain) {
        // The URL is on the same domain, use the view API endpoint
        const baseUrl = window.location.origin;
        const relativePath = url.replace(baseUrl, '').replace('/cdn', '');
        viewUrl = `${baseUrl}/api/cdn/view?path=${encodeURIComponent(url)}${separator}key=${apiKey}`;
    } else {
        // Direct CDN URL, add key as query parameter
        viewUrl = `${url}${separator}key=${apiKey}`;
    }

    if (isImage) {
        // For images, open in a lightbox-style viewer
        const overlay = document.createElement('div');
        overlay.style.position = 'fixed';
        overlay.style.top = '0';
        overlay.style.left = '0';
        overlay.style.width = '100%';
        overlay.style.height = '100%';
        overlay.style.backgroundColor = 'rgba(0, 0, 0, 0.8)';
        overlay.style.display = 'flex';
        overlay.style.alignItems = 'center';
        overlay.style.justifyContent = 'center';
        overlay.style.zIndex = '9999';
        overlay.style.cursor = 'pointer';
        overlay.onclick = () => document.body.removeChild(overlay);

        const img = document.createElement('img');
        img.src = viewUrl;
        img.style.maxWidth = '90%';
        img.style.maxHeight = '90%';
        img.style.objectFit = 'contain';
        img.style.boxShadow = '0 0 20px rgba(0, 0, 0, 0.5)';
        img.onclick = (e) => e.stopPropagation();

        const closeBtn = document.createElement('button');
        closeBtn.innerText = '×';
        closeBtn.style.position = 'absolute';
        closeBtn.style.top = '20px';
        closeBtn.style.right = '20px';
        closeBtn.style.fontSize = '30px';
        closeBtn.style.background = 'none';
        closeBtn.style.border = 'none';
        closeBtn.style.color = 'white';
        closeBtn.style.cursor = 'pointer';
        closeBtn.onclick = () => document.body.removeChild(overlay);

        overlay.appendChild(img);
        overlay.appendChild(closeBtn);
        document.body.appendChild(overlay);
    } else if (isPdf || isMedia) {
        // For PDF and media files, open in a new tab
        window.open(viewUrl, '_blank');
    } else {
        // For other files, prompt download
        const link = document.createElement('a');
        link.href = viewUrl;
        link.setAttribute('download', '');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};

/**
 * Rename a file in the CDN
 * @param {string} url - Current file URL
 * @param {string} newName - New filename
 * @param {string} apiKey - API key for authentication
 * @returns {Promise<Object>} - Result with new URL
 */
window.renameFile = async function (url, newName, apiKey) {
    if (!url || !newName) return { success: false, message: "URL or new name is missing" };

    try {
        const baseUrl = window.location.origin;
        const response = await fetch(`${baseUrl}/api/cdn/rename`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Api-Key': apiKey
            },
            body: JSON.stringify({ path: url, newName: newName })
        });

        return await response.json();
    } catch (error) {
        console.error("Error renaming file:", error);
        return { success: false, message: error.message };
    }
};

/**
 * Get file details including size, upload date, etc.
 * @param {string} url - File URL
 * @param {string} apiKey - API key for authentication
 * @returns {Promise<Object>} - File details
 */
window.getFileDetails = async function (url, apiKey) {
    if (!url) return { success: false, message: "URL is missing" };

    try {
        const baseUrl = window.location.origin;
        const response = await fetch(`${baseUrl}/api/cdn/details?path=${encodeURIComponent(url)}`, {
            method: 'GET',
            headers: {
                'X-Api-Key': apiKey
            }
        });

        return await response.json();
    } catch (error) {
        console.error("Error getting file details:", error);
        return { success: false, message: error.message };
    }
};