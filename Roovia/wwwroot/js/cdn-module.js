// cdn-module.js - Consolidated JavaScript module for CDN functionality
window.cdnModule = (function () {
    // Private variables
    let dotNetReference = null;
    let apiKey = '';
    let currentZoomLevel = 1;
    let uploadCancellationCallbacks = new Map();
    
    // Cache API key in sessionStorage
    function loadApiKeyFromStorage() {
        const storedKey = sessionStorage.getItem('cdnApiKey');
        if (storedKey) {
            apiKey = storedKey;
            console.log('CDN API key loaded from session storage');
        }
    }
    
    // Load key from storage on page load
    if (typeof window !== 'undefined') {
        loadApiKeyFromStorage();
        
        // Listen for page load to ensure everything is ready
        window.addEventListener('DOMContentLoaded', function() {
            console.log('CDN Module initialized on page load');
        });
    }
    
    // ------------------------
    // INITIALIZATION FUNCTIONS
    // ------------------------
    
    function initialize(reference, key) {
        console.log('Initializing CDN module');
        
        if (reference) {
            dotNetReference = reference;
            console.log('Stored .NET reference for callbacks');
        }
        
        if (key) {
            apiKey = key;
            sessionStorage.setItem('cdnApiKey', key);
            console.log('API key saved to session storage');
        }

        // Add drag/drop listeners with slight delay to ensure DOM is ready
        if (dotNetReference) {
            setTimeout(function() {
                addDragDropListeners();
            }, 100);
        }
        
        return true;
    }
    
    function dispose() {
        console.log('Disposing CDN module');
        dotNetReference = null;
        // Clear any event listeners or active operations here
        return true;
    }
    
    // ------------------------
    // FILE UPLOAD FUNCTIONS
    // ------------------------
    
    function addDragDropListeners() {
        if (!dotNetReference) {
            console.warn('No .NET reference available for drag/drop callbacks');
            return;
        }
        
        // Find all file upload containers
        const containers = document.querySelectorAll('.file-upload-container');
        if (!containers || containers.length === 0) {
            console.warn('No file upload containers found in the DOM');
            return;
        }
        
        console.log(`Found ${containers.length} file upload containers`);
        
        // Add event listeners to each container
        containers.forEach(container => {
            // Prevent default behaviors
            ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
                container.addEventListener(eventName, function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                });
            });
            
            // Handle drag enter/over
            container.addEventListener('dragenter', function() {
                try {
                    dotNetReference.invokeMethodAsync('OnDragEnter');
                } catch (e) {
                    console.error('Error invoking OnDragEnter:', e);
                }
            });
            
            container.addEventListener('dragover', function() {
                // We don't need to call OnDragEnter again, but we need to prevent default
            });
            
            // Handle drag leave
            container.addEventListener('dragleave', function(e) {
                // Check if we're actually leaving the container (not entering a child element)
                const rect = container.getBoundingClientRect();
                if (e.clientX < rect.left || e.clientX >= rect.right || 
                    e.clientY < rect.top || e.clientY >= rect.bottom) {
                    try {
                        dotNetReference.invokeMethodAsync('OnDragLeave');
                    } catch (e) {
                        console.error('Error invoking OnDragLeave:', e);
                    }
                }
            });
            
            // Handle drop
            container.addEventListener('drop', function() {
                try {
                    dotNetReference.invokeMethodAsync('OnDragLeave');
                } catch (e) {
                    console.error('Error invoking OnDragLeave on drop:', e);
                }
            });
        });
        
        console.log('Drag/drop listeners added successfully');
    }
    
    function cancelUpload(uploadId) {
        if (uploadCancellationCallbacks.has(uploadId)) {
            const callback = uploadCancellationCallbacks.get(uploadId);
            callback();
            uploadCancellationCallbacks.delete(uploadId);
            return true;
        }
        return false;
    }
    
    // ------------------------
    // FILE OPERATIONS
    // ------------------------
    
    function openFileWithApiKey(url, forceDownload = false) {
        if (!url) {
            console.error('No URL provided to open');
            return false;
        }
        
        // Add API key if not already present
        const hasParams = url.includes('?');
        const separator = hasParams ? '&' : '?';
        const keyParam = `key=${apiKey}`;
        
        // Don't append key if it's already there
        const urlWithKey = url.includes(keyParam) ? url : `${url}${separator}${keyParam}`;
        
        // Determine file type from extension
        const extension = url.split('.').pop().toLowerCase();
        const isImage = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'svg', 'webp'].includes(extension);
        const isPdf = extension === 'pdf';
        const isMedia = ['mp4', 'webm', 'mp3', 'wav'].includes(extension);
        
        // Handle download request
        if (forceDownload) {
            const link = document.createElement('a');
            link.href = urlWithKey;
            link.setAttribute('download', '');
            link.target = '_blank';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            return true;
        }
        
        // Handle media preview
        if (isImage || isPdf || isMedia) {
            window.open(urlWithKey, '_blank');
            return true;
        }
        
        // Default to download for other file types
        const link = document.createElement('a');
        link.href = urlWithKey;
        link.setAttribute('download', '');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        return true;
    }
    
    async function renameFile(url, newName, key) {
        try {
            if (!url || !newName) {
                return { success: false, message: 'URL and new name are required' };
            }
            
            const activeKey = key || apiKey;
            
            if (!activeKey) {
                return { success: false, message: 'API key is required' };
            }
            
            // Prepare request data
            const data = {
                Path: url,
                NewName: newName
            };
            
            // Common request options
            const requestOptions = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Api-Key': activeKey
                },
                body: JSON.stringify(data)
            };
            
            // Get the base URL from the current location
            const baseUrl = window.location.origin;
            const endpoint = `${baseUrl}/api/cdn/rename`;
            
            // Execute the request
            const response = await fetch(endpoint, requestOptions);
            
            if (!response.ok) {
                const errorText = await response.text();
                return {
                    success: false,
                    message: `Failed to rename file: ${response.status} ${response.statusText}`,
                    details: errorText
                };
            }
            
            return await response.json();
            
        } catch (error) {
            console.error('Error in renameFile:', error);
            return { 
                success: false, 
                message: `Error: ${error.message || 'Unknown error'}`
            };
        }
    }
    
    async function createFolder(category, parentFolder, folderName, key) {
        try {
            if (!category || !folderName) {
                return { success: false, message: 'Category and folder name are required' };
            }
            
            const activeKey = key || apiKey;
            
            if (!activeKey) {
                return { success: false, message: 'API key is required' };
            }
            
            // Prepare request data
            const data = {
                Category: category,
                ParentFolder: parentFolder || '',
                FolderName: folderName
            };
            
            // Common request options
            const requestOptions = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Api-Key': activeKey
                },
                body: JSON.stringify(data)
            };
            
            // Get the base URL from the current location
            const baseUrl = window.location.origin;
            const endpoint = `${baseUrl}/api/cdn/create-folder`;
            
            // Execute the request
            const response = await fetch(endpoint, requestOptions);
            
            if (!response.ok) {
                const errorText = await response.text();
                return {
                    success: false,
                    message: `Failed to create folder: ${response.status} ${response.statusText}`,
                    details: errorText
                };
            }
            
            return await response.json();
            
        } catch (error) {
            console.error('Error in createFolder:', error);
            return { 
                success: false, 
                message: `Error: ${error.message || 'Unknown error'}`
            };
        }
    }
    
    // ------------------------
    // FILE PREVIEW FUNCTIONS
    // ------------------------
    
    async function getFileInfo(url) {
        try {
            if (!url) {
                return { success: false, error: 'No URL provided' };
            }
            
            // Add API key to URL query parameters
            const separator = url.includes('?') ? '&' : '?';
            const urlWithKey = `${url}${separator}key=${apiKey}`;
            
            // Try to use HEAD request first
            try {
                const response = await fetch(urlWithKey, { method: 'HEAD' });
                
                if (response.ok) {
                    return {
                        success: true,
                        modified: new Date(response.headers.get('Last-Modified')),
                        size: parseInt(response.headers.get('Content-Length'), 10),
                        contentType: response.headers.get('Content-Type')
                    };
                }
            } catch (e) {
                console.warn('HEAD request failed, trying full GET');
            }
            
            // If HEAD fails, try a GET request
            const response = await fetch(urlWithKey);
            
            if (!response.ok) {
                return { success: false, error: `HTTP error: ${response.status}` };
            }
            
            return {
                success: true,
                modified: new Date(response.headers.get('Last-Modified')),
                size: parseInt(response.headers.get('Content-Length'), 10),
                contentType: response.headers.get('Content-Type')
            };
            
        } catch (error) {
            console.error('Error getting file info:', error);
            return { success: false, error: error.message };
        }
    }
    
    async function fetchTextContent(url) {
        if (!url) {
            throw new Error('No URL provided');
        }
        
        try {
            // Add API key if not already present
            const hasParams = url.includes('?');
            const separator = hasParams ? '&' : '?';
            const keyParam = `key=${apiKey}`;
            const urlWithKey = url.includes(keyParam) ? url : `${url}${separator}${keyParam}`;
            
            const response = await fetch(urlWithKey);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.text();
        } catch (error) {
            console.error('Error fetching text content:', error);
            throw error;
        }
    }
    
    function zoomImage(action) {
        const maxZoom = 4;
        const minZoom = 0.5;
        const zoomStep = 0.25;
        
        const imageContainer = document.querySelector('.image-preview');
        if (!imageContainer) return false;
        
        const img = imageContainer.querySelector('img');
        if (!img) return false;
        
        switch (action) {
            case 'in':
                if (currentZoomLevel < maxZoom) {
                    currentZoomLevel += zoomStep;
                }
                break;
            case 'out':
                if (currentZoomLevel > minZoom) {
                    currentZoomLevel -= zoomStep;
                }
                break;
            case 'reset':
                currentZoomLevel = 1;
                break;
            default:
                return false;
        }
        
        img.style.transform = `scale(${currentZoomLevel})`;
        return true;
    }
    
    // ------------------------
    // UTILITY FUNCTIONS
    // ------------------------
    
    function copyToClipboard(text) {
        if (!navigator.clipboard) {
            const textArea = document.createElement('textarea');
            textArea.value = text;
            
            // Make the textarea out of viewport
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            
            try {
                document.execCommand('copy');
                return true;
            } catch (err) {
                console.error('Fallback clipboard copy failed:', err);
                return false;
            } finally {
                document.body.removeChild(textArea);
            }
        }
        
        return navigator.clipboard.writeText(text)
            .then(() => true)
            .catch(err => {
                console.error('Clipboard API copy failed:', err);
                return false;
            });
    }
    
    function showToast(message, type = 'info', duration = 3000) {
        // Create toast container if it doesn't exist
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.style.position = 'fixed';
            container.style.bottom = '20px';
            container.style.right = '20px';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }
        
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.style.backgroundColor = type === 'info' ? '#17a2b8' : 
                                     type === 'success' ? '#28a745' : 
                                     type === 'warning' ? '#ffc107' : 
                                     type === 'error' ? '#dc3545' : '#17a2b8';
        toast.style.color = type === 'warning' ? '#212529' : '#fff';
        toast.style.padding = '10px 15px';
        toast.style.borderRadius = '4px';
        toast.style.marginTop = '10px';
        toast.style.boxShadow = '0 2px 5px rgba(0,0,0,0.2)';
        toast.style.minWidth = '200px';
        toast.style.transition = 'opacity 0.5s ease-in-out';
        toast.style.opacity = '0';
        
        toast.innerHTML = message;
        container.appendChild(toast);
        
        // Animate in
        setTimeout(() => {
            toast.style.opacity = '1';
        }, 10);
        
        // Remove after duration
        setTimeout(() => {
            toast.style.opacity = '0';
            setTimeout(() => {
                if (container.contains(toast)) {
                    container.removeChild(toast);
                }
            }, 500);
        }, duration);
        
        return true;
    }
    
    // Public API
    return {
        initialize,
        dispose,
        getApiKey: function() { return apiKey; },
        setApiKey: function(key) { 
            apiKey = key; 
            sessionStorage.setItem('cdnApiKey', key);
        },
        addDragDropListeners,
        cancelUpload,
        openFile: openFileWithApiKey,
        renameFile,
        createFolder,
        getFileInfo,
        fetchTextContent,
        zoomImage,
        copyToClipboard,
        showToast
    };
})();