# Roovia Deployment Guide

## Table of Contents
- [Overview](#overview)
- [Publishing Your Application](#publishing-your-application)
- [Versioning System](#versioning-system)
- [Managing the Service](#managing-the-service)
- [Troubleshooting](#troubleshooting)
- [Server Structure](#server-structure)
- [Security Notes](#security-notes)
- [Advanced Usage](#advanced-usage)

## Overview

This document provides comprehensive instructions for deploying, managing, and troubleshooting the Roovia application on the production server. The system includes automatic versioning, easy rollback capabilities, and service management tools.

## Publishing Your Application

### Setting Up Your Visual Studio Publish Profile

1. In Visual Studio, right-click on your project and select "Publish"
2. Create a new FTP profile with these settings:
   - **Publish method**: FTP
   - **Server**: portal.roovia.co.za
   - **Site path**: /roovia
   - **Username**: roovia-deploy
   - **Password**: [contact administrator for password]
   - **Deployment mode**: Remove additional files at destination
   - **Passive mode**: Enabled (important!)

![Publish Profile Settings](https://placeholder-image.com/publish-settings.jpg)

### Important Publishing Notes

- **Wait for completion**: Publishing can take 5+ minutes for larger applications
- **Automatic versioning**: The system will automatically create a version backup ~5 minutes after publishing completes
- **Service restart**: The service will automatically restart after versioning
- **Remove additional files**: Always keep this option checked to prevent stale files

### Manual Deployment

If you need to deploy manually without Visual Studio:

```bash
# Connect via SFTP/SCP
scp -r ./bin/Release/net9.0/publish/* roovia-deploy@portal.roovia.co.za:/var/www/roovia/
```

## Versioning System

The server implements an automatic versioning system that creates backups of your application whenever changes are detected.

### How Versioning Works

1. When files are uploaded via FTP, an activity monitor tracks the changes
2. After 5 minutes of inactivity (indicating publish is complete), a backup is created
3. Each backup is stored in `/var/www/roovia-versions/[TIMESTAMP]`
4. The system keeps the 15 most recent versions

### Managing Versions

To interact with the versioning system, use the management script:

```bash
# List all available versions
sudo /var/www/manage-versions.sh list

# Create a manual backup
sudo /var/www/manage-versions.sh backup

# Roll back to a specific version
sudo /var/www/manage-versions.sh rollback [VERSION_TIMESTAMP]

# Check system status
sudo /var/www/manage-versions.sh status
```

Example output:
```
Available versions:
total 56
drwxr-xr-x 16 www-data www-data 4096 Apr 30 14:32 20250430_143245
drwxr-xr-x 16 www-data www-data 4096 Apr 30 12:15 20250430_121510
drwxr-xr-x 16 www-data www-data 4096 Apr 30 09:58 20250430_095830
```

## Managing the Service

The Roovia application runs as a system service managed by systemd.

### Basic Service Commands

```bash
# Restart the service (apply changes immediately)
sudo systemctl restart roovia.service

# Check service status
sudo systemctl status roovia.service

# View application logs
sudo journalctl -u roovia.service -n 100

# View the last 50 lines of logs and follow new entries
sudo journalctl -u roovia.service -n 50 -f
```

### Quick Restart Script

For convenience, a restart script is available:

```bash
sudo /var/www/restart-app.sh
```

This will restart the service and show its current status.

### When to Restart the Service

- After publishing (if you don't want to wait for automatic restart)
- After configuration changes
- If the application becomes unresponsive
- After server updates or reboots

## Troubleshooting

### Common Issues and Solutions

#### FTP Connection Problems

```bash
# Check FTP service status
sudo systemctl status vsftpd

# Restart FTP service if needed
sudo systemctl restart vsftpd

# Check FTP logs
sudo tail -n 100 /var/log/vsftpd.log
```

#### Application Not Responding

```bash
# Check service status
sudo systemctl status roovia.service

# Restart the service
sudo systemctl restart roovia.service

# Check for errors in logs
sudo journalctl -u roovia.service -n 100 --no-pager
```

#### Versioning Issues

```bash
# Check versioning logs
sudo tail -n 100 /var/log/roovia-backup.log

# Check FTP activity monitor
sudo systemctl status ftp-activity-monitor.service

# Force a backup manually
sudo /var/www/manage-versions.sh backup
```

#### Server Errors (HTTP 500)

```bash
# Check Apache error logs
sudo tail -n 100 /var/log/apache2/roovia-error.log

# Check application logs for exceptions
sudo journalctl -u roovia.service -n 100 --no-pager | grep -i exception
```

## Server Structure

The server is configured with the following components:

### Directory Structure

- `/var/www/roovia/` - Main application directory
- `/var/www/roovia-versions/` - Version backups
- `/var/log/` - Log files
  - `/var/log/roovia-backup.log` - Versioning logs
  - `/var/log/ftp-activity.log` - FTP monitoring logs
  - `/var/log/apache2/roovia-*.log` - Apache access/error logs

### Services

- `roovia.service` - Main application service
- `vsftpd.service` - FTP server
- `apache2.service` - Web server/reverse proxy
- `ftp-activity-monitor.service` - FTP activity tracking

### Configuration Files

- `/etc/systemd/system/roovia.service` - Service configuration
- `/etc/apache2/sites-available/portal.roovia.co.za.conf` - Apache virtual host
- `/etc/vsftpd.conf` - FTP server configuration

## Security Notes

- FTP credentials grant access to deploy to the application directory
- The FTP user is chrooted to `/var/www`
- The application runs under the `www-data` user
- Versioning and management scripts require sudo access
- Only use the management scripts for versioning/rollback to maintain proper permissions

## Advanced Usage

### Custom Deployment Scripts

For automated CI/CD pipelines, you can use:

```bash
scp -r ./bin/Release/net9.0/publish/* roovia-deploy@portal.roovia.co.za:/var/www/roovia/
ssh roovia-deploy@portal.roovia.co.za "sudo /var/www/restart-app.sh"
```

### Configuration Management

For environment-specific settings:

1. Use `appsettings.Production.json` for production settings
2. Store sensitive values in user secrets or environment variables
3. During deployment, ensure configuration files are properly migrated

### Database Migrations

When deploying database changes:

1. Include EF Core migration scripts in your deployment
2. Consider using a separate deployment step for database migrations
3. Take a database backup before applying migrations

```bash
# Example on-server migration command
cd /var/www/roovia
dotnet ef database update
```

### Monitoring and Health Checks

The server includes basic monitoring:

- Service status is checked via systemd
- Apache is configured to restart if it fails
- For advanced monitoring, consider setting up health check endpoints in your application

---

## Contact Information

For assistance with deployment issues, contact:
- System Administrator: [admin@roovia.co.za](mailto:admin@roovia.co.za)
- Development Team: [dev@roovia.co.za](mailto:dev@roovia.co.za)

Last Updated: April 30, 2025