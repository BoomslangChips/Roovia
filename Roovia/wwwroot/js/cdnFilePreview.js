// cdnFilePreview.js - Advanced File Preview Utilities
window.cdnFilePreview = (() => {
    let dotNetRef = null;
    let currentZoomLevel = 1;
    const maxZoomLevel = 4;
    const minZoomLevel = 0.5;
    const zoomStep = 0.25;

    // Load required libraries asynchronously if not already loaded
    function loadScript(url, callback) {
        if (document.querySelector(`script[src="${url}"]`)) {
            if (callback) callback();
            return;
        }

        const script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = url;
        script.onload = callback;
        document.head.appendChild(script);
    }

    function loadStylesheet(url) {
        if (document.querySelector(`link[href="${url}"]`)) return;

        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = url;
        document.head.appendChild(link);
    }

    const librariesToLoad = [
        { type: 'script', url: 'https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.7.0/highlight.min.js' },
        { type: 'style', url: 'https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.7.0/styles/github.min.css' },
        { type: 'script', url: 'https://cdnjs.cloudflare.com/ajax/libs/js-beautify/1.14.7/beautify.min.js' },
        { type: 'script', url: 'https://cdnjs.cloudflare.com/ajax/libs/js-beautify/1.14.7/beautify-html.min.js' },
        { type: 'script', url: 'https://cdnjs.cloudflare.com/ajax/libs/js-beautify/1.14.7/beautify-css.min.js' },
        { type: 'script', url: 'https://cdnjs.cloudflare.com/ajax/libs/PapaParse/5.4.0/papaparse.min.js' },
        { type: 'script', url: 'https://cdnjs.cloudflare.com/ajax/libs/xlsx/0.18.5/xlsx.full.min.js' }
    ];

    function initialize(reference) {
        console.log('[CdnFilePreview] Initializing...');
        dotNetRef = reference;

        // Load all required libraries
        librariesToLoad.forEach(lib => {
            if (lib.type === 'script') {
                loadScript(lib.url);
            } else if (lib.type === 'style') {
                loadStylesheet(lib.url);
            }
        });

        // Initialize toast container if it doesn't exist
        if (!document.getElementById('cdnToastContainer')) {
            const toastContainer = document.createElement('div');
            toastContainer.id = 'cdnToastContainer';
            toastContainer.style.position = 'fixed';
            toastContainer.style.bottom = '20px';
            toastContainer.style.right = '20px';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        console.log('[CdnFilePreview] Initialized');
        return true;
    }

    function dispose() {
        dotNetRef = null;
        // No need to unload scripts
    }

    async function getFileInfo(url, apiKey) {
        if (!url) {
            return { success: false, error: 'No URL provided' };
        }

        try {
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
                console.warn('HEAD request failed, falling back to file-details API');
            }

            // If HEAD fails, try our API endpoint
            const baseUrl = window.location.origin;
            const apiUrl = `${baseUrl}/api/cdn/file-details?path=${encodeURIComponent(url)}`;
            
            const response = await fetch(apiUrl, {
                headers: {
                    'X-Api-Key': apiKey
                }
            });

            if (!response.ok) {
                return { success: false, error: `HTTP error: ${response.status}` };
            }

            const data = await response.json();
            
            if (data.success) {
                return {
                    success: true,
                    modified: new Date(data.modified),
                    size: data.size,
                    contentType: data.contentType
                };
            } else {
                return { success: false, error: data.message || 'Failed to get file info' };
            }
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
            const response = await fetch(url);

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.text();
        } catch (error) {
            console.error('Error fetching text content:', error);
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('HandlePreviewError', error.message);
            }
            throw error;
        }
    }

    async function applySyntaxHighlighting(selector, fileExtension) {
        // Wait for highlight.js to be available
        for (let attempt = 0; attempt < 10; attempt++) {
            if (window.hljs) break;
            await new Promise(r => setTimeout(r, 100));
        }

        if (!window.hljs) {
            console.warn('highlight.js not available for syntax highlighting');
            return false;
        }

        try {
            // Map file extension to language
            const langMap = {
                'js': 'javascript',
                'ts': 'typescript',
                'cs': 'csharp',
                'py': 'python',
                'java': 'java',
                'html': 'html',
                'css': 'css',
                'json': 'json',
                'xml': 'xml',
                'md': 'markdown',
                'sql': 'sql',
                'php': 'php',
                'rb': 'ruby',
                'go': 'go',
                'sh': 'bash',
                'bat': 'batch',
                'ps1': 'powershell',
                'yaml': 'yaml',
                'yml': 'yaml',
                'txt': 'plaintext'
            };

            // Get the language from the extension
            const language = langMap[fileExtension.toLowerCase()] || '';
            
            // Find the element
            const element = document.querySelector(selector);
            if (!element) {
                console.warn(`Element not found: ${selector}`);
                return false;
            }

            // Add the appropriate class for highlight.js
            if (language) {
                element.classList.add(`language-${language}`);
            }

            // Highlight the element
            window.hljs.highlightElement(element);
            return true;
        } catch (error) {
            console.error('Error applying syntax highlighting:', error);
            return false;
        }
    }

    async function formatTextContent(text, fileExtension) {
        if (!text) {
            return { success: false, error: 'No text to format' };
        }

        // Wait for beautify library to be available
        for (let attempt = 0; attempt < 10; attempt++) {
            if (window.js_beautify && window.html_beautify && window.css_beautify) break;
            await new Promise(r => setTimeout(r, 100));
        }

        try {
            let formattedText = text;
            
            // Format based on file extension
            switch (fileExtension.toLowerCase()) {
                case 'js':
                case 'json':
                case 'ts':
                    if (window.js_beautify) {
                        formattedText = window.js_beautify(text, {
                            indent_size: 2,
                            space_in_empty_paren: true
                        });
                    }
                    break;
                
                case 'html':
                case 'htm':
                case 'xml':
                    if (window.html_beautify) {
                        formattedText = window.html_beautify(text, {
                            indent_size: 2,
                            wrap_line_length: 100,
                            preserve_newlines: true,
                            max_preserve_newlines: 2
                        });
                    }
                    break;
                
                case 'css':
                    if (window.css_beautify) {
                        formattedText = window.css_beautify(text, {
                            indent_size: 2
                        });
                    }
                    break;
                
                case 'cs':
                case 'java':
                case 'py':
                default:
                    // No specific formatter for these languages
                    return { success: false, message: 'No formatter available for this file type' };
            }
            
            return { success: true, formattedText };
        } catch (error) {
            console.error('Error formatting text:', error);
            return { success: false, error: error.message };
        }
    }

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
        const container = document.getElementById('cdnToastContainer');
        if (!container) return;
        
        const toast = document.createElement('div');
        toast.className = `cdn-toast cdn-toast-${type}`;
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
    }

    async function downloadFile(url, apiKey, fileName) {
        if (!url) return;
        
        try {
            // Add API key to URL query parameters
            const separator = url.includes('?') ? '&' : '?';
            const urlWithKey = `${url}${separator}key=${apiKey}`;
            
            // Create a temporary link for download
            const link = document.createElement('a');
            link.href = urlWithKey;
            link.download = fileName || url.split('/').pop();
            link.target = '_blank';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } catch (error) {
            console.error('Error downloading file:', error);
            alert('Error downloading file: ' + error.message);
        }
    }

    function zoomImage(action) {
        const imageContainer = document.querySelector('.image-preview');
        if (!imageContainer) return;
        
        const img = imageContainer.querySelector('img');
        if (!img) return;
        
        switch (action) {
            case 'in':
                if (currentZoomLevel < maxZoomLevel) {
                    currentZoomLevel += zoomStep;
                }
                break;
            case 'out':
                if (currentZoomLevel > minZoomLevel) {
                    currentZoomLevel -= zoomStep;
                }
                break;
            case 'reset':
                currentZoomLevel = 1;
                break;
        }
        
        img.style.transform = `scale(${currentZoomLevel})`;
    }

    async function loadSpreadsheetData(url, fileName) {
        if (!url) {
            return { success: false, message: 'No URL provided' };
        }

        // Wait for required libraries
        for (let attempt = 0; attempt < 10; attempt++) {
            if (window.Papa && window.XLSX) break;
            await new Promise(r => setTimeout(r, 100));
        }

        if (!window.Papa || !window.XLSX) {
            return { success: false, message: 'Required libraries not loaded' };
        }

        try {
            const extension = fileName.split('.').pop().toLowerCase();
            
            if (extension === 'csv') {
                return await loadCsvData(url);
            } else if (extension === 'xlsx' || extension === 'xls') {
                return await loadExcelData(url);
            } else {
                return { success: false, message: 'Unsupported file type' };
            }
        } catch (error) {
            console.error('Error loading spreadsheet data:', error);
            return { success: false, message: error.message };
        }
    }

    async function loadCsvData(url) {
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error: ${response.status}`);
            }
            
            const csvText = await response.text();
            
            const result = Papa.parse(csvText, {
                header: true,
                skipEmptyLines: true,
                dynamicTyping: true
            });
            
            if (result.errors && result.errors.length > 0) {
                console.warn('CSV parsing errors:', result.errors);
            }
            
            // Create spreadsheet data structure
            const sheetData = {
                Name: 'Sheet1',
                Columns: result.meta.fields || [],
                Rows: []
            };
            
            // Add rows data
            for (const row of result.data) {
                const rowData = [];
                for (const col of sheetData.Columns) {
                    rowData.push(row[col] === null || row[col] === undefined ? '' : String(row[col]));
                }
                sheetData.Rows.push(rowData);
            }
            
            return {
                success: true,
                data: {
                    Sheets: [sheetData]
                }
            };
        } catch (error) {
            console.error('Error loading CSV:', error);
            return { success: false, message: error.message };
        }
    }

    async function loadExcelData(url) {
        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`HTTP error: ${response.status}`);
            }
            
            const arrayBuffer = await response.arrayBuffer();
            const data = new Uint8Array(arrayBuffer);
            
            // Parse with SheetJS
            const workbook = XLSX.read(data, { type: 'array' });
            
            // Process each sheet
            const sheets = [];
            for (const sheetName of workbook.SheetNames) {
                const worksheet = workbook.Sheets[sheetName];
                
                // Convert to JSON
                const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1 });
                
                if (jsonData.length === 0) {
                    continue; // Skip empty sheets
                }
                
                // First row is headers
                const columns = jsonData[0].map(col => col ? String(col) : `Column${jsonData[0].indexOf(col) + 1}`);
                
                // Remaining rows are data
                const rows = jsonData.slice(1).map(row => {
                    // Ensure all columns are present by mapping through column indexes
                    return columns.map((_, idx) => {
                        const value = row[idx];
                        return value === undefined || value === null ? '' : String(value);
                    });
                });
                
                sheets.push({
                    Name: sheetName,
                    Columns: columns,
                    Rows: rows
                });
            }
            
            return {
                success: true,
                data: {
                    Sheets: sheets
                }
            };
        } catch (error) {
            console.error('Error loading Excel:', error);
            return { success: false, message: error.message };
        }
    }

    function exportSheetToCSV(sheetName, columns, rows) {
        if (!window.Papa) {
            alert('CSV export library not available.');
            return;
        }
        
        try {
            // Create array of data with headers
            const data = [columns, ...rows];
            
            // Generate CSV
            const csv = Papa.unparse(data);
            
            // Create download
            const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
            const url = URL.createObjectURL(blob);
            
            const link = document.createElement('a');
            link.href = url;
            link.setAttribute('download', `${sheetName}.csv`);
            link.style.visibility = 'hidden';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        } catch (error) {
            console.error('Error exporting CSV:', error);
            alert('Error exporting sheet: ' + error.message);
        }
    }

    // Public API
    return {
        initialize,
        dispose,
        fetchTextContent,
        getFileInfo,
        applySyntaxHighlighting,
        formatTextContent,
        copyToClipboard,
        showToast,
        downloadFile,
        zoomImage,
        loadSpreadsheetData,
        exportSheetToCSV
    };
})();