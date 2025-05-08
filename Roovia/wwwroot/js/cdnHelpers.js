// cdnHelpers.js - Enhanced CDN utilities for Roovia
let cdnApiKey = '';

// Initialize from session storage on page load
document.addEventListener('DOMContentLoaded', function () {
    const storedKey = sessionStorage.getItem('cdnApiKey');
    if (storedKey) {
        cdnApiKey = storedKey;
        console.log('CDN API key loaded from session storage');
    }
});

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
 * Log detailed request information to console
 * @param {string} message - Message to log
 * @param {Object} data - Data to log
 */
function logDebug(message, data) {
    console.log(`[CDN Debug] ${message}`, data);
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

    logDebug(`Opening URL: ${url} with key: ${key.substring(0, 4)}...`, { fullUrl: urlWithKey, download });

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
 * Fetch text content from a URL with API key
 * @param {string} url - The URL to fetch
 * @returns {Promise<string>} A promise that resolves with the text content
 */
async function fetchTextContent(url) {
    if (!url) {
        throw new Error('No URL provided');
    }

    try {
        const key = getCdnApiKey();

        // Add API key to query parameters if not already present
        const separator = url.includes('?') ? '&' : '?';
        const urlWithKey = `${url}${separator}key=${key}`;

        logDebug(`Fetching text from: ${url}`, { fullUrl: urlWithKey });

        const response = await fetch(urlWithKey);

        if (!response.ok) {
            const errorText = await response.text();
            logDebug('Error response:', {
                status: response.status,
                statusText: response.statusText,
                content: errorText.length > 200 ? errorText.substring(0, 200) + '...' : errorText
            });
            throw new Error(`HTTP error! status: ${response.status}, message: ${response.statusText}`);
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

    logDebug('Adding drag/drop listeners to containers', { count: containers.length });

    // Add event listeners to each container
    containers.forEach(container => {
        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            container.addEventListener(eventName, function (e) {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        container.addEventListener('dragenter', function (e) {
            dotNetRef.invokeMethodAsync('OnDragEnter');
        });

        container.addEventListener('dragover', function (e) {
            // Keep the drag effect active
        });

        container.addEventListener('dragleave', function (e) {
            // Check if the mouse has left the container
            const rect = container.getBoundingClientRect();
            const x = e.clientX;
            const y = e.clientY;

            if (x < rect.left || x >= rect.right || y < rect.top || y >= rect.bottom) {
                dotNetRef.invokeMethodAsync('OnDragLeave');
            }
        });

        container.addEventListener('drop', function (e) {
            dotNetRef.invokeMethodAsync('OnDragLeave');
        });
    });
}

/**
 * Rename a file with enhanced error handling
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

        const key = apiKey || getCdnApiKey();

        logDebug(`Renaming file: ${url} to ${newName}`, { data });

        // Try each endpoint in sequence until one works
        const endpoints = [
            // 1. Debug controller (most robust error handling)
            `${window.location.origin}/api/cdn-debug/rename`,
            // 2. Production API directly
            'https://portal.roovia.co.za/api/cdn/rename',
            // 3. Local extended CDN controller
            `${window.location.origin}/api/cdn/rename`,
            // 4. Local compatibility controller
            `${window.location.origin}/api/cdncompat/rename`
        ];

        // Create common request options
        const requestOptions = {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Api-Key': key
            },
            body: JSON.stringify(data)
        };

        // Try each endpoint
        for (const endpoint of endpoints) {
            try {
                logDebug(`Trying endpoint: ${endpoint}`);

                // Make the request with built-in timeout handling
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout

                const response = await fetch(endpoint, {
                    ...requestOptions,
                    signal: controller.signal
                });

                clearTimeout(timeoutId);

                // If we get HTML instead of JSON (which would cause JSON parse error)
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('text/html')) {
                    logDebug(`HTML response detected from ${endpoint}:`, { contentType });
                    const htmlContent = await response.text();
                    console.warn(`Endpoint ${endpoint} returned HTML instead of JSON`, {
                        firstChars: htmlContent.substring(0, 100),
                        status: response.status,
                        headers: Object.fromEntries([...response.headers])
                    });
                    continue; // Try next endpoint
                }

                const result = await response.json();

                if (response.ok) {
                    logDebug(`Success with endpoint: ${endpoint}`, result);
                    return result;
                } else {
                    logDebug(`Failed with endpoint ${endpoint}: ${response.status}`, result);
                }
            } catch (err) {
                console.warn(`Error with endpoint ${endpoint}:`, err);
                // Continue to next endpoint
            }
        }

        // If we get here, all endpoints failed
        return {
            success: false,
            message: 'All rename endpoints failed. Check console for details.'
        };
    } catch (error) {
        console.error('Error renaming file:', error);
        return { success: false, message: error.message };
    }
}

/**
 * Test controller connectivity and log detailed results
 */
function logEndpointStatus() {
    const baseUrl = window.location.origin;
    const key = getCdnApiKey();

    const endpoints = [
        `${baseUrl}/api/cdn-debug/ping`,
        `${baseUrl}/api/cdncompat/ping`,
        `${baseUrl}/api/cdn/ping`,
        'https://portal.roovia.co.za/api/cdn/ping',
        'https://portal.roovia.co.za/api/cdn/categories'
    ];

    console.group('🔍 CDN Endpoint Status Check');
    console.log(`Testing with API key: ${key.substring(0, 4)}...${key.substring(key.length - 4)}`);

    // Add request headers with API key
    const headers = new Headers({
        'X-Api-Key': key,
        'Accept': 'application/json'
    });

    endpoints.forEach(endpoint => {
        // Show we're checking each endpoint
        console.log(`Testing ${endpoint}...`);

        fetch(endpoint, { headers })
            .then(async response => {
                const contentType = response.headers.get('content-type');

                console.group(`📌 ${endpoint}: ${response.status} ${response.statusText}`);
                console.log('Content-Type:', contentType);

                try {
                    // Try to parse as JSON first
                    if (contentType && contentType.includes('application/json')) {
                        const data = await response.json();
                        console.log('Response:', data);
                    } else {
                        // If not JSON, get text and show first 200 chars
                        const text = await response.text();
                        console.log('Response (first 200 chars):', text.substring(0, 200));
                        console.log('Full response length:', text.length);
                    }
                } catch (err) {
                    console.error('Error parsing response:', err);
                }

                console.groupEnd();
            })
            .catch(err => {
                console.error(`❌ ${endpoint}: Error - ${err.message}`);
            });
    });

    console.groupEnd();
}

/**
 * Get file details including size, upload date, etc.
 * @param {string} url - File URL
 * @param {string} apiKey - API key for authentication
 * @returns {Promise<Object>} - File details
 */
async function getFileDetails(url, apiKey) {
    if (!url) return { success: false, message: "URL is missing" };

    try {
        const baseUrl = window.location.origin;
        const key = apiKey || getCdnApiKey();

        logDebug(`Getting file details for: ${url}`);

        // Add necessary headers
        const headers = new Headers({
            'X-Api-Key': key,
            'Accept': 'application/json'
        });

        const response = await fetch(`${baseUrl}/api/cdn-debug/file-details?path=${encodeURIComponent(url)}`, {
            method: 'GET',
            headers: headers
        });

        // Handle potential HTML responses
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('text/html')) {
            const htmlContent = await response.text();
            console.warn('HTML response detected instead of JSON', {
                firstChars: htmlContent.substring(0, 100),
                status: response.status
            });
            return { success: false, message: "Received HTML instead of JSON" };
        }

        const result = await response.json();
        logDebug(`File details result:`, result);
        return result;
    } catch (error) {
        console.error("Error getting file details:", error);
        return { success: false, message: error.message };
    }
}