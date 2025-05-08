// cdnHelpers.js
/**
 * CDN Helper Functions for Roovia
 */

// Global API key cache
let cdnApiKey = '';

/**
 * Set the CDN API key
 * @param {string} apiKey - The API key to use for CDN operations
 */
function setCdnApiKey(apiKey) {
    cdnApiKey = apiKey;
    // Store in session storage for page refreshes
    sessionStorage.setItem('cdnApiKey', apiKey);
    console.log('CDN API key set');
}

/**
 * Get the CDN API key
 * @returns {string} The current API key
 */
function getCdnApiKey() {
    // Try to get from session storage if not already set
    if (!cdnApiKey) {
        cdnApiKey = sessionStorage.getItem('cdnApiKey') || '';
    }
    return cdnApiKey;
}

/**
 * Open a URL with API key
 * @param {string} url - The URL to open
 * @param {string} apiKey - The API key to use (optional, uses global key if not provided)
 * @param {boolean} download - Whether to force download (optional, defaults to false)
 */
function openUrlWithApiKey(url, apiKey, download = false) {
    if (!url) {
        console.error('No URL provided');
        return;
    }

    // Use provided API key or fall back to global key
    const key = apiKey || getCdnApiKey();

    if (!key) {
        console.error('No API key available');
        alert('API key is required to view files');
        return;
    }

    // Add API key to URL query parameters
    const separator = url.includes('?') ? '&' : '?';
    const urlWithKey = `${url}${separator}key=${key}`;

    if (download) {
        // Create a temporary link for download
        const link = document.createElement('a');
        link.href = urlWithKey;
        link.download = url.split('/').pop(); // Get filename from URL
        link.target = '_blank';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } else {
        // Open in new tab
        window.open(urlWithKey, '_blank');
    }
}

/**
 * Fetch text content from a URL
 * @param {string} url - The URL to fetch
 * @returns {Promise<string>} A promise that resolves with the text content
 */
async function fetchTextContent(url) {
    if (!url) {
        throw new Error('No URL provided');
    }

    try {
        const response = await fetch(url, {
            headers: {
                'X-Api-Key': getCdnApiKey()
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.text();
    } catch (error) {
        console.error('Error fetching text content:', error);
        throw error;
    }
}

/**
 * Add drag and drop event listeners to file upload component
 * @param {object} dotNetRef - Reference to the .NET component
 */
function addDragDropListeners(dotNetRef) {
    if (!dotNetRef) {
        console.error('No .NET reference provided');
        return;
    }

    // Get file upload containers
    const containers = document.querySelectorAll('.file-upload-container');
    if (!containers.length) {
        console.error('No file upload containers found');
        // Try again after a delay in case containers are not yet rendered
        setTimeout(() => addDragDropListeners(dotNetRef), 500);
        return;
    }

    // Add event listeners to each container
    containers.forEach(container => {
        container.addEventListener('dragenter', function (e) {
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync('OnDragEnter');
        });

        container.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.stopPropagation();
        });

        container.addEventListener('dragleave', function (e) {
            e.preventDefault();
            e.stopPropagation();

            // Check if the mouse has left the container
            const rect = container.getBoundingClientRect();
            const x = e.clientX;
            const y = e.clientY;

            if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
                dotNetRef.invokeMethodAsync('OnDragLeave');
            }
        });

        container.addEventListener('drop', function (e) {
            e.preventDefault();
            e.stopPropagation();
            dotNetRef.invokeMethodAsync('OnDragLeave');
        });
    });
}

/**
 * Rename a file
 * @param {string} url - The URL of the file to rename
 * @param {string} newName - The new name for the file
 * @param {string} apiKey - The API key to use
 * @returns {Promise} A promise that resolves with the rename result
 */
async function renameFile(url, newName, apiKey) {
    if (!url || !newName) {
        return { success: false, message: 'URL and new name are required' };
    }

    try {
        // Create request data
        const data = {
            Path: url,
            NewName: newName
        };

        // Get base URL
        const baseUrl = window.location.origin;
        const apiUrl = `${baseUrl}/api/cdn/rename`;

        // Make API request
        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Api-Key': apiKey || getCdnApiKey()
            },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            const result = await response.json();
            return result;
        } else {
            const error = await response.text();
            console.error('Error renaming file:', error);
            return { success: false, message: error };
        }
    } catch (error) {
        console.error('Error renaming file:', error);
        return { success: false, message: error.message };
    }
}

// Initialize from session storage on page load
document.addEventListener('DOMContentLoaded', function () {
    const storedKey = sessionStorage.getItem('cdnApiKey');
    if (storedKey) {
        cdnApiKey = storedKey;
    }
});