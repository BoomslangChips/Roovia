# CDN System Architecture Overview

This document provides a comprehensive overview of the enhanced CDN system architecture.

## System Architecture

The CDN system consists of several integrated components working together to provide a robust file management solution:

```
┌───────────────────────────────────────────────────────────────────┐
│                     Roovia CDN System Architecture                 │
└───────────────────────────────────────────────────────────────────┘
                                  │
┌─────────────────┐   ┌───────────┴──────────────┐   ┌──────────────┐
│  User Interface │◄──┤      CDN Controllers     ├──►│  CDN Service │
└─────────┬───────┘   └───────────┬──────────────┘   └───────┬──────┘
          │                       │                          │
          │                       │                          │
┌─────────▼───────┐   ┌───────────▼──────────────┐   ┌──────▼───────┐
│ Admin Components│   │   Middleware Pipeline    │   │   Database   │
└─────────────────┘   └───────────┬──────────────┘   └──────┬───────┘
                                  │                          │
                      ┌───────────▼──────────────┐           │
                      │     File System Storage  │◄──────────┘
                      └──────────────────────────┘
```

### Core Components

1. **User Interface Layer**
   - CDN Dashboard (`CdnDashboard.razor`)
   - Configuration Admin (`CdnConfigAdmin.razor`)
   - File and Folder Management Components
   - Preview and Upload Components

2. **Controller Layer**
   - CDN API Controllers (`ExtendedCdnController.cs`)
   - File Operation Endpoints
   - Configuration Management API

3. **Service Layer**
   - CDN Service (`CdnService.cs`)
   - File Management Logic
   - Configuration Access

4. **Middleware Layer**
   - API Key Validation (`ApiKeyMiddleware.cs`)
   - Request Pipeline Integration
   - Security Enforcement

5. **Data Layer**
   - Database Models (`CdnConfiguration.cs`, etc.)
   - SQL Database Tables
   - File Metadata Storage

6. **Storage Layer**
   - Physical File Storage
   - Directory Structure Management
   - File Operations

## Component Interactions

### Request Flow

1. **File Upload Process**
   ```
   Client → API Controller → API Key Validation → CDN Service → 
   File System + Database → Response
   ```

2. **File Download Process**
   ```
   Client → API Controller → API Key Validation → CDN Service → 
   File System → Response
   ```

3. **Configuration Process**
   ```
   Admin → Config UI → API Controller → Database → CDN Service Update
   ```

### Data Flow

1. **File Metadata Flow**
   ```
   Upload → File Metadata Creation → Database Storage → 
   Usage Statistics Update
   ```

2. **Configuration Flow**
   ```
   Admin UI → Database → Service Configuration Refresh
   ```

3. **Access Logging Flow**
   ```
   Request → API Key Validation → Access Log Entry → 
   Usage Statistics Update
   ```

## Database Schema

The database schema includes several key tables:

1. **CdnConfigurations**
   - Stores CDN configuration settings
   - Only one active configuration at a time
   - Contains paths, limits, and API keys

2. **CdnApiKeys**
   - Stores API key information
   - Includes usage tracking
   - Supports restrictions and expiration

3. **CdnFileMetadata**
   - Maps physical files to logical structure
   - Tracks file usage and metadata
   - Supports file management operations

4. **CdnUsageStatistics**
   - Tracks usage by category and date
   - Stores count and size metrics
   - Supports reporting and dashboard

5. **CdnAccessLogs**
   - Records all CDN access attempts
   - Includes success/failure status
   - Supports security auditing

## Security Architecture

The security model relies on several layers:

1. **API Key Authentication**
   - All CDN operations require valid API key
   - Keys can be restricted by IP, domain, and time
   - Access is logged for auditing

2. **Path Validation**
   - All file paths are sanitized
   - Directory traversal attacks are prevented
   - File type validation is enforced

3. **User Authorization**
   - Admin operations require appropriate roles
   - Configuration changes are tracked
   - Operations are logged with user identity

## Optimization Features

The system includes several optimization features:

1. **Caching Strategy**
   - File path caching
   - Content delivery caching
   - Cache invalidation triggers

2. **File Storage Optimization**
   - Orphaned file cleanup
   - Storage space reclamation
   - File deduplication (planned)

3. **Performance Enhancements**
   - Large buffer sizes for file operations
   - Asynchronous I/O operations
   - Parallel processing for batch operations

## Scalability Considerations

The architecture supports scaling in several ways:

1. **Vertical Scaling**
   - Configurable buffer sizes
   - Memory usage optimization
   - Efficient I/O operations

2. **Horizontal Scaling (Planned)**
   - Distributed file storage support
   - Load balancing considerations
   - Shared metadata database

3. **External CDN Integration (Planned)**
   - Support for external CDN providers
   - Edge caching capability
   - Global content distribution

## Monitoring and Maintenance

The system includes monitoring and maintenance capabilities:

1. **Usage Monitoring**
   - Daily statistics collection
   - Category-based metrics
   - Storage utilization tracking

2. **Maintenance Routines**
   - Orphaned file cleanup
   - Storage optimization
   - Cache management

3. **Performance Metrics**
   - Upload/download throughput
   - API key usage tracking
   - Error rate monitoring

## Integration Points

The CDN system integrates with other systems through:

1. **API Endpoints**
   - RESTful API for all operations
   - JSON response format
   - Standard HTTP status codes

2. **Database Integration**
   - Shared database schema
   - Entity Framework Core models
   - Transaction support

3. **File System Access**
   - Configurable storage paths
   - Direct file system operations
   - Remote file system support (planned)

## Future Enhancements

Planned future enhancements include:

1. **Content Optimization**
   - Image compression
   - On-the-fly resizing
   - Format conversion

2. **Advanced Security**
   - Content signing
   - Encryption at rest
   - Advanced access controls

3. **Integration Expansion**
   - WebDAV support
   - S3-compatible API
   - Cloud storage backends