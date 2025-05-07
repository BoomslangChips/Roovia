// fileHelpers.js - Additional helpers for file operations

// File fetch function that handles text file content
window.fetchTextContent = async function (url) {
    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to fetch: ${response.status} ${response.statusText}`);
        }
        return await response.text();
    } catch (error) {
        console.error("Error fetching text content:", error);
        throw error;
    }
};

// Enhanced file opening with download option
window.openUrlWithApiKey = function (url, apiKey, forceDownload = false) {
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

    // If force download, create download link
    if (forceDownload) {
        const link = document.createElement('a');
        link.href = viewUrl;
        link.setAttribute('download', '');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        return;
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
        // For other files, just open in a new tab
        window.open(viewUrl, '_blank');
    }
};