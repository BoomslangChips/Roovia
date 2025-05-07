server {
    listen 80;
    server_name cdn.roovia.co.za;
    return 301 https://$host$request_uri;
}
server {
    listen 443 ssl;
    server_name cdn.roovia.co.za;
    
    # SSL settings (added by certbot)
    ssl_certificate /etc/letsencrypt/live/cdn.roovia.co.za/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/cdn.roovia.co.za/privkey.pem;
    
    # Root directory
    root /var/www/cdn;
    
    # CORS headers for all responses
    add_header 'Access-Control-Allow-Origin' '*' always;
    add_header 'Access-Control-Allow-Methods' 'GET, OPTIONS' always;
    add_header 'Access-Control-Allow-Headers' 'DNT,X-CustomHeader,Keep-Alive,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,X-Api-Key' always;
    
    # Caching headers for better performance
    add_header Cache-Control "public, max-age=604800, immutable" always;
    expires 7d;
    
    # Security headers
    add_header X-Content-Type-Options nosniff always;
    
    # Handle OPTIONS method for CORS preflight
    if ($request_method = 'OPTIONS') {
        return 204;
    }
    
    # Simple API key check
    location / {
        # API key validation
        set $allow 0;
        
        if ($http_x_api_key = "RooviaCDNKey") {
            set $allow 1;
        }
        
        if ($arg_key = "RooviaCDNKey") {
            set $allow 1;
        }
        
        if ($remote_addr = "127.0.0.1") {
            set $allow 1;
        }
        
        if ($remote_addr = "::1") {
            set $allow 1;
        }
        
        if ($allow = 0) {
            return 403 "API Key Required";
        }
        
        # Enable compression
        gzip on;
        gzip_comp_level 6;
        gzip_min_length 1000;
        gzip_types image/jpeg image/png application/pdf text/plain application/javascript;
        
        # Disable directory listing
        autoindex off;
        
        try_files $uri $uri/ =404;
    }
}

# CDN System Documentation

## Overview

The CDN (Content Delivery Network) system provides file storage and delivery capabilities for the Roovia application. It allows uploading, viewing, and deleting files such as images, documents, and other media files.

## Configuration

The CDN system is configured in `appsettings.json` with the following settings:

```json
"CDN": {
  "BaseUrl": "https://cdn.roovia.co.za",
  "StoragePath": "/var/www/cdn"
}
```

- **BaseUrl**: The URL where CDN files are publicly accessible
- **StoragePath**: The physical file system path where CDN files are stored

## Authentication

The CDN system uses API key authentication for external access. The API key is defined in the `CdnService` class and is used for all API endpoints. The current API key is: `RooviaCDNKey`.

## File Categories

Files are organized into categories:

- `documents`: General document files (default)
- `images`: Image files
- `hr`: HR-related documents
- `weighbridge`: Weighbridge files
- `lab`: Laboratory files

## API Endpoints

The CDN system exposes several API endpoints:

### Upload Files

```
POST /api/cdn/upload
```

Parameters:
- `file`: The file to upload (multipart/form-data)
- `category`: The file category (optional, defaults to "documents")

Headers:
- `X-Api-Key`: API key for authentication

### Delete Files

```
DELETE /api/cdn/delete?path={fileUrl}
```

Parameters:
- `path`: The full URL of the file to delete

Headers:
- `X-Api-Key`: API key for authentication

### List Files

```
GET /api/cdn/files?category={category}&pattern={pattern}
```

Parameters:
- `category`: The file category to list (optional, defaults to "documents")
- `pattern`: File name pattern to filter (optional, defaults to "*")

Headers:
- `X-Api-Key`: API key for authentication

### View Files Directly

```
GET /api/cdn/view?path={fileUrl}
```

Parameters:
- `path`: The full URL of the file to view

Headers:
- `X-Api-Key`: API key for authentication

## Blazor Components

### CdnFileUpload

A reusable Blazor component for file uploads with preview capabilities:

```csharp
<CdnFileUpload 
    Category="images"
    Title="Upload Images"
    Description="Drag and drop images here, or click to browse"
    FileTypeHint="Accepted file types: JPG, PNG, GIF"
    AcceptedFileTypes=".jpg,.jpeg,.png,.gif"
    OnFileUploaded="HandleImageUploaded"
    OnFileDeleted="HandleFileDeleted"
    Multiple="true"
    AllowDelete="true"
    MaxFileSize="5242880" />
```

Parameters:
- `Category`: The CDN category to upload to
- `Title`: The component title
- `Description`: Upload instructions
- `FileTypeHint`: Hint text for allowed file types
- `AcceptedFileTypes`: File extension filter
- `Multiple`: Allow multiple file uploads
- `MaxFileSize`: Maximum file size in bytes
- `IsCompact`: Use compact display mode
- `HideFileList`: Hide the uploaded files list
- `AllowDelete`: Allow file deletion
- `OnFileUploaded`: Event when a file is uploaded
- `OnFilesUploaded`: Event when multiple files are uploaded
- `OnFileDeleted`: Event when a file is deleted

### ViewCdnFiles

A component for viewing and managing CDN files:

```csharp
<ViewCdnFiles
    Title="Image Gallery"
    Category="images"
    AllowDelete="true"
    EmptyMessage="No images found."
    OnFileDeleted="HandleFileDeleted"
    AutoLoad="true"
    FilePattern="*.jpg" />
```

Parameters:
- `Title`: The component title
- `Category`: The CDN category to display
- `AllowDelete`: Allow file deletion
- `EmptyMessage`: Message to show when no files are found
- `OnFileDeleted`: Event when a file is deleted
- `AutoLoad`: Automatically load files on initialization
- `FilePattern`: File name pattern to filter

## JavaScript Functions

Two main JavaScript functions are included in `fileUpload.js`:

### addDragDropListeners

Adds drag and drop functionality to the file upload component.

### openUrlWithApiKey

Opens a file URL with authentication, handling both viewing and downloading based on the file type.

## File Storage Organization

Files are stored in the following structure:

```
/var/www/cdn/
  ├── documents/
  │   └── [files]
  ├── images/
  │   └── [files]
  ├── hr/
  │   └── [files]
  ├── weighbridge/
  │   └── [files]
  └── lab/
      └── [files]
```

## CORS Configuration

To allow cross-domain access between the main application and CDN domains, CORS headers must be configured on the CDN server:

```
Access-Control-Allow-Origin: https://portal.roovia.co.za
Access-Control-Allow-Methods: GET, POST, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, X-Api-Key
```

## Security Considerations

1. The API key should be kept secure and not exposed in client-side code
2. File type validation prevents upload of potentially dangerous files
3. File size limits prevent DoS attacks via large file uploads
4. Unique filenames prevent collisions and overwrites
5. Authentication is required for all API endpoints