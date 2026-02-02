# HEMS Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the Holistic Examination Management System (HEMS) to various environments including Development, Testing, Staging, and Production.

## Prerequisites

### System Requirements

- **Operating System**: Windows Server 2016 or later / Windows 10 Pro or later
- **IIS**: Version 10.0 or later with ASP.NET 4.8 support
- **Database**: SQL Server 2016 or later (Express, Standard, or Enterprise)
- **Framework**: .NET Framework 4.8
- **Memory**: Minimum 4GB RAM (8GB recommended for production)
- **Storage**: Minimum 10GB free space

### Software Dependencies

- IIS with ASP.NET 4.8
- SQL Server with appropriate edition
- PowerShell 5.1 or later
- Visual C++ Redistributable for Visual Studio 2019

## Deployment Process

### Step 1: Prepare the Environment

1. **Install IIS Features**

   ```powershell
   # Run as Administrator
   .\HEMS\Scripts\Configure-IIS.ps1 -InstallFeatures
   ```

2. **Configure IIS Settings**
   ```powershell
   # Run as Administrator
   .\HEMS\Scripts\Configure-IIS.ps1
   ```

### Step 2: Database Setup

1. **Create Database**

   ```sql
   -- Run the database migration script
   sqlcmd -S [SERVER_NAME] -i "HEMS\Scripts\Database-Migration.sql"
   ```

2. **Verify Database Schema**
   - Ensure all 8 tables are created
   - Verify indexes and relationships
   - Check default data insertion

### Step 3: Application Deployment

1. **Deploy Application Files**

   ```powershell
   # Example for Production deployment
   .\HEMS\Scripts\Deploy-Environment.ps1 `
     -Environment "Production" `
     -SiteName "HEMS_Production" `
     -ApplicationPath "C:\inetpub\wwwroot\HEMS" `
     -ConnectionString "Data Source=PROD-SQL;Initial Catalog=HEMS_Production;Integrated Security=False;User ID=hems_user;Password=SECURE_PASSWORD" `
     -Port 80 `
     -AppPoolName "HEMS_Production_Pool"
   ```

2. **Configure Environment-Specific Settings**
   - The deployment script automatically applies web.config transformations
   - Verify connection strings and app settings
   - Update machine keys for production environments

### Step 4: Post-Deployment Verification

1. **Configuration Validation**
   - Navigate to `/Configuration/Validate`
   - Review and resolve any errors or warnings
   - Export validation report for documentation

2. **Functional Testing**
   - Test coordinator login (admin@university.edu)
   - Verify student import functionality
   - Test exam creation and taking workflow
   - Validate grading and results display

## Environment-Specific Configurations

### Development Environment

```powershell
.\HEMS\Scripts\Deploy-Environment.ps1 `
  -Environment "Debug" `
  -SiteName "HEMS_Dev" `
  -ApplicationPath "C:\Dev\HEMS" `
  -ConnectionString "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=HEMS_Debug;Integrated Security=True" `
  -Port 8080
```

**Features:**

- Detailed error messages enabled
- Debug compilation enabled
- Extended session timeout (60 minutes)
- Trace logging enabled

### Testing Environment

```powershell
.\HEMS\Scripts\Deploy-Environment.ps1 `
  -Environment "Testing" `
  -SiteName "HEMS_Test" `
  -ApplicationPath "C:\inetpub\wwwroot\HEMS_Test" `
  -ConnectionString "Data Source=TEST-SQL;Initial Catalog=HEMS_Testing;User ID=hems_test;Password=TEST_PASS" `
  -Port 8081
```

**Features:**

- Test data enabled
- Detailed error messages for debugging
- Extended trace logging
- Relaxed security for testing

### Staging Environment

```powershell
.\HEMS\Scripts\Deploy-Environment.ps1 `
  -Environment "Staging" `
  -SiteName "HEMS_Staging" `
  -ApplicationPath "C:\inetpub\wwwroot\HEMS_Staging" `
  -ConnectionString "Data Source=STAGING-SQL;Initial Catalog=HEMS_Staging;User ID=hems_staging;Password=STAGING_PASS" `
  -Port 443
```

**Features:**

- Production-like configuration
- SSL enforcement
- Custom error pages
- Performance optimization

### Production Environment

```powershell
.\HEMS\Scripts\Deploy-Environment.ps1 `
  -Environment "Production" `
  -SiteName "HEMS_Production" `
  -ApplicationPath "C:\inetpub\wwwroot\HEMS" `
  -ConnectionString "Data Source=PROD-SQL;Initial Catalog=HEMS_Production;User ID=hems_prod;Password=SECURE_PROD_PASS" `
  -Port 443
```

**Features:**

- Debug disabled
- Custom error pages
- SSL required
- Security headers configured
- Request filtering enabled
- Compression enabled
- Machine key configured

## Security Configuration

### SSL/TLS Setup

1. **Obtain SSL Certificate**
   - Purchase from trusted CA or use Let's Encrypt
   - Install certificate in IIS

2. **Configure HTTPS Binding**

   ```powershell
   New-WebBinding -Name "HEMS_Production" -Protocol https -Port 443 -SslFlags 1
   ```

3. **Force HTTPS Redirect**
   - Automatically configured in production web.config transformation
   - URL rewrite rules redirect HTTP to HTTPS

### Database Security

1. **Create Dedicated Database User**

   ```sql
   CREATE LOGIN hems_user WITH PASSWORD = 'SECURE_PASSWORD';
   USE HEMS_Production;
   CREATE USER hems_user FOR LOGIN hems_user;
   ALTER ROLE db_datareader ADD MEMBER hems_user;
   ALTER ROLE db_datawriter ADD MEMBER hems_user;
   ALTER ROLE db_ddladmin ADD MEMBER hems_user;
   ```

2. **Configure Connection String Encryption**
   ```powershell
   # Encrypt connection strings section
   aspnet_regiis -pe "connectionStrings" -app "/HEMS" -prov "RsaProtectedConfigurationProvider"
   ```

### Application Security

1. **Machine Key Configuration**
   - Generate unique machine keys for each environment
   - Use IIS Manager or PowerShell to configure
   - Store keys securely for multi-server deployments

2. **Request Filtering**
   - Configured automatically by deployment scripts
   - Blocks dangerous file extensions
   - Limits request sizes

## Monitoring and Maintenance

### Health Checks

1. **Application Health**
   - Monitor `/Configuration/Validate` endpoint
   - Set up automated health checks
   - Configure alerts for validation failures

2. **Database Health**
   - Monitor connection pool usage
   - Check database performance counters
   - Set up backup and maintenance plans

### Logging

1. **Application Logs**
   - Configure log levels per environment
   - Use structured logging for production
   - Set up log rotation and archival

2. **IIS Logs**
   - Enable detailed logging
   - Configure log file locations
   - Set up log analysis tools

### Performance Monitoring

1. **Application Performance**
   - Monitor response times
   - Track memory usage
   - Monitor cache hit rates

2. **Database Performance**
   - Monitor query execution times
   - Track database connections
   - Monitor index usage

## Backup and Recovery

### Database Backup

1. **Automated Backups**

   ```sql
   -- Create backup job
   BACKUP DATABASE HEMS_Production
   TO DISK = 'C:\Backups\HEMS_Production_Full.bak'
   WITH FORMAT, INIT, COMPRESSION;
   ```

2. **Transaction Log Backups**
   ```sql
   -- For point-in-time recovery
   BACKUP LOG HEMS_Production
   TO DISK = 'C:\Backups\HEMS_Production_Log.trn';
   ```

### Application Backup

1. **File System Backup**
   - Backup application files
   - Include configuration files
   - Store backups securely

2. **Configuration Backup**
   - Export configuration settings
   - Document environment-specific settings
   - Version control deployment scripts

## Troubleshooting

### Common Issues

1. **Database Connection Failures**
   - Verify connection string
   - Check SQL Server service status
   - Validate user permissions

2. **Application Pool Crashes**
   - Check event logs
   - Verify .NET Framework version
   - Review memory usage

3. **Permission Issues**
   - Verify IIS application pool identity
   - Check file system permissions
   - Validate database user permissions

### Diagnostic Tools

1. **Configuration Validation**
   - Use `/Configuration/Validate` endpoint
   - Review validation reports
   - Check system requirements

2. **IIS Diagnostics**
   - Enable failed request tracing
   - Review IIS logs
   - Use IIS diagnostic tools

## Scaling and Load Balancing

### Single Server Deployment

- Suitable for small to medium installations
- Up to 500 concurrent users
- Single point of failure

### Multi-Server Deployment

1. **Web Farm Configuration**
   - Multiple IIS servers
   - Shared session state
   - Load balancer configuration

2. **Database Scaling**
   - Read replicas for reporting
   - Database clustering
   - Connection pooling optimization

## Support and Maintenance

### Regular Maintenance Tasks

1. **Weekly Tasks**
   - Review application logs
   - Check system performance
   - Validate backups

2. **Monthly Tasks**
   - Update security patches
   - Review configuration
   - Performance optimization

3. **Quarterly Tasks**
   - Security audit
   - Disaster recovery testing
   - Capacity planning review

### Contact Information

For deployment support and troubleshooting:

- System Administrator: [admin@university.edu]
- Technical Support: [support@university.edu]
- Emergency Contact: [emergency@university.edu]

## Appendix

### File Locations

- **Application Files**: `C:\inetpub\wwwroot\HEMS`
- **Configuration Files**: `C:\inetpub\wwwroot\HEMS\web.config`
- **Log Files**: `C:\inetpub\logs\LogFiles\W3SVC1`
- **Backup Location**: `C:\Backups\HEMS`

### Port Assignments

- **Development**: 8080 (HTTP)
- **Testing**: 8081 (HTTP)
- **Staging**: 443 (HTTPS)
- **Production**: 443 (HTTPS)

### Service Accounts

- **Application Pool**: `IIS AppPool\HEMS_[Environment]_Pool`
- **Database Access**: `hems_[environment]_user`
- **File System**: Application Pool Identity

---

**Document Version**: 1.0  
**Last Updated**: February 2026  
**Next Review**: May 2026
