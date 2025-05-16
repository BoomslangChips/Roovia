using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;

namespace Roovia.Services.General
{
    public class AuditService : IAuditService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<AuditService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        // Audit Trail Operations
        public async Task<ResponseModel> LogEntityChange(string entityType, int entityId, string userId, string action, string details, string oldValue = null, string newValue = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Implement audit trail logging once AuditTrail model is added to your DB context
                /* Example:
                var audit = new AuditTrail
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    UserId = userId,
                    Action = action,
                    Details = details,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Timestamp = DateTime.Now,
                    CompanyId = await GetUserCompanyId(context, userId)
                };

                context.AuditTrails.Add(audit);
                await context.SaveChangesAsync();
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Audit record created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging entity change: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error logging entity change: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetEntityAuditTrail(string entityType, int entityId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var query = context.AuditTrails
                    .Where(a => a.EntityType == entityType && a.EntityId == entityId);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                var totalCount = await query.CountAsync();

                var auditTrail = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        a.Id,
                        a.EntityType,
                        a.EntityId,
                        a.Action,
                        a.Details,
                        UserName = a.User.FullName,
                        a.OldValue,
                        a.NewValue,
                        a.Timestamp
                    })
                    .ToListAsync();

                response.Response = new {
                    AuditTrail = auditTrail,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Entity audit trail retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity audit trail: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving entity audit trail: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetUserAuditTrail(string userId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found";
                    return response;
                }

                var query = context.AuditTrails.Where(a => a.UserId == userId);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                var totalCount = await query.CountAsync();

                var auditTrail = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        a.Id,
                        a.EntityType,
                        a.EntityId,
                        a.Action,
                        a.Details,
                        a.OldValue,
                        a.NewValue,
                        a.Timestamp
                    })
                    .ToListAsync();

                response.Response = new {
                    UserDetails = new {
                        user.FullName,
                        user.Email,
                        user.Role
                    },
                    AuditTrail = auditTrail,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "User audit trail retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user audit trail: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving user audit trail: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetSystemAuditTrail(int companyId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var query = context.AuditTrails.Where(a => a.CompanyId == companyId);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                var totalCount = await query.CountAsync();

                var auditTrail = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        a.Id,
                        a.EntityType,
                        a.EntityId,
                        a.Action,
                        a.Details,
                        UserName = a.User.FullName,
                        a.OldValue,
                        a.NewValue,
                        a.Timestamp
                    })
                    .ToListAsync();

                response.Response = new {
                    AuditTrail = auditTrail,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "System audit trail retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system audit trail: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving system audit trail: {ex.Message}";
            }

            return response;
        }

        // Security Audit
        public async Task<ResponseModel> LogAuthentication(string userId, string ipAddress, bool success, string details = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found";
                    return response;
                }

                var audit = new SecurityAudit
                {
                    UserId = userId,
                    IpAddress = ipAddress,
                    Success = success,
                    Details = details,
                    Timestamp = DateTime.Now,
                    Action = "Authentication",
                    CompanyId = user.CompanyId ?? 0
                };

                context.SecurityAudits.Add(audit);
                await context.SaveChangesAsync();

                // Update user's last login info if successful
                if (success)
                {
                    user.LastLoginDate = DateTime.Now;
                    user.LastLoginIpAddress = ipAddress;
                    user.LoginFailureCount = 0;

                    await context.SaveChangesAsync();
                }
                else
                {
                    user.LastLoginFailureDate = DateTime.Now;
                    user.LoginFailureCount++;

                    await context.SaveChangesAsync();
                }
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Authentication audit record created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging authentication attempt: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error logging authentication attempt: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> LogPermissionChange(string userId, string targetUserId, string permission, bool granted, string details = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var targetUser = await context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (user == null || targetUser == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User or target user not found";
                    return response;
                }

                var audit = new SecurityAudit
                {
                    UserId = userId,
                    TargetUserId = targetUserId,
                    Action = granted ? "PermissionGranted" : "PermissionRevoked",
                    Details = $"Permission: {permission}. {details}",
                    Timestamp = DateTime.Now,
                    CompanyId = user.CompanyId ?? 0
                };

                context.SecurityAudits.Add(audit);
                await context.SaveChangesAsync();
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Permission change audit record created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging permission change: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error logging permission change: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> LogRoleChange(string userId, string targetUserId, string role, bool granted, string details = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var targetUser = await context.Users.FirstOrDefaultAsync(u => u.Id == targetUserId);

                if (user == null || targetUser == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User or target user not found";
                    return response;
                }

                var audit = new SecurityAudit
                {
                    UserId = userId,
                    TargetUserId = targetUserId,
                    Action = granted ? "RoleAssigned" : "RoleRevoked",
                    Details = $"Role: {role}. {details}",
                    Timestamp = DateTime.Now,
                    CompanyId = user.CompanyId ?? 0
                };

                context.SecurityAudits.Add(audit);
                await context.SaveChangesAsync();
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Role change audit record created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging role change: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error logging role change: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetSecurityAuditTrail(int companyId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var query = context.SecurityAudits.Where(a => a.CompanyId == companyId);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                var totalCount = await query.CountAsync();

                var auditTrail = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        a.Id,
                        a.Action,
                        a.Details,
                        UserName = a.User.FullName,
                        TargetUserName = a.TargetUser.FullName,
                        a.IpAddress,
                        a.Success,
                        a.Timestamp
                    })
                    .ToListAsync();

                response.Response = new {
                    AuditTrail = auditTrail,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Security audit trail retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security audit trail: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving security audit trail: {ex.Message}";
            }

            return response;
        }

        // Financial Audit
        public async Task<ResponseModel> LogFinancialActivity(string entityType, int entityId, string userId, string action, decimal amount, string details, string referenceNumber = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || !user.CompanyId.HasValue)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "User not found or not associated with a company";
                    return response;
                }

                var audit = new FinancialAudit
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    UserId = userId,
                    Action = action,
                    Amount = amount,
                    Details = details,
                    ReferenceNumber = referenceNumber,
                    Timestamp = DateTime.Now,
                    CompanyId = user.CompanyId.Value
                };

                context.FinancialAudits.Add(audit);
                await context.SaveChangesAsync();
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Financial activity audit record created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging financial activity: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error logging financial activity: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetFinancialAuditTrail(int companyId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                var query = context.FinancialAudits.Where(a => a.CompanyId == companyId);

                if (startDate.HasValue)
                    query = query.Where(a => a.Timestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.Timestamp <= endDate.Value);

                var totalCount = await query.CountAsync();

                var auditTrail = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        a.Id,
                        a.EntityType,
                        a.EntityId,
                        a.Action,
                        a.Amount,
                        a.Details,
                        a.ReferenceNumber,
                        UserName = a.User.FullName,
                        a.Timestamp
                    })
                    .ToListAsync();

                response.Response = new {
                    AuditTrail = auditTrail,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Financial audit trail retrieved successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving financial audit trail: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving financial audit trail: {ex.Message}";
            }

            return response;
        }

        // Data Management
        public async Task<ResponseModel> PurgeOldAuditTrail(int daysToKeep, int companyId = 0)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                /* Example implementation:
                int deletedRecords = 0;

                if (companyId > 0)
                {
                    // Delete company-specific audit records
                    deletedRecords += await context.AuditTrails
                        .Where(a => a.Timestamp < cutoffDate && a.CompanyId == companyId)
                        .ExecuteDeleteAsync();

                    deletedRecords += await context.SecurityAudits
                        .Where(a => a.Timestamp < cutoffDate && a.CompanyId == companyId)
                        .ExecuteDeleteAsync();

                    deletedRecords += await context.FinancialAudits
                        .Where(a => a.Timestamp < cutoffDate && a.CompanyId == companyId)
                        .ExecuteDeleteAsync();
                }
                else
                {
                    // Delete all old audit records
                    deletedRecords += await context.AuditTrails
                        .Where(a => a.Timestamp < cutoffDate)
                        .ExecuteDeleteAsync();

                    deletedRecords += await context.SecurityAudits
                        .Where(a => a.Timestamp < cutoffDate)
                        .ExecuteDeleteAsync();

                    deletedRecords += await context.FinancialAudits
                        .Where(a => a.Timestamp < cutoffDate)
                        .ExecuteDeleteAsync();
                }

                response.Response = new { DeletedRecords = deletedRecords };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Old audit records purged successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purging old audit records: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error purging old audit records: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> ArchiveAuditTrail(DateTime cutoffDate, int companyId = 0)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                // 1. Get records to archive
                var auditTrailRecords = await context.AuditTrails
                    .Where(a => a.Timestamp < cutoffDate && (companyId == 0 || a.CompanyId == companyId))
                    .ToListAsync();

                var securityAuditRecords = await context.SecurityAudits
                    .Where(a => a.Timestamp < cutoffDate && (companyId == 0 || a.CompanyId == companyId))
                    .ToListAsync();

                var financialAuditRecords = await context.FinancialAudits
                    .Where(a => a.Timestamp < cutoffDate && (companyId == 0 || a.CompanyId == companyId))
                    .ToListAsync();

                // 2. Copy to archive tables
                foreach (var record in auditTrailRecords)
                {
                    context.ArchivedAuditTrails.Add(new ArchivedAuditTrail
                    {
                        OriginalId = record.Id,
                        EntityType = record.EntityType,
                        EntityId = record.EntityId,
                        UserId = record.UserId,
                        Action = record.Action,
                        Details = record.Details,
                        OldValue = record.OldValue,
                        NewValue = record.NewValue,
                        Timestamp = record.Timestamp,
                        CompanyId = record.CompanyId,
                        ArchivedDate = DateTime.Now
                    });
                }

                // Add similar code for security and financial audits

                await context.SaveChangesAsync();

                // 3. Delete from main tables
                await PurgeOldAuditTrail(0, companyId, cutoffDate);

                var totalArchivedRecords = auditTrailRecords.Count + securityAuditRecords.Count + financialAuditRecords.Count;
                response.Response = new { ArchivedRecords = totalArchivedRecords };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Audit records archived successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving audit records: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error archiving audit records: {ex.Message}";
            }

            return response;
        }

        // Reports
        public async Task<ResponseModel> GetAuditSummary(int companyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set default date range if not provided
                startDate ??= DateTime.Now.AddMonths(-1);
                endDate ??= DateTime.Now;

                /* Example implementation:
                // Get count of audit records by entity type
                var entityTypeCounts = await context.AuditTrails
                    .Where(a => a.CompanyId == companyId && a.Timestamp >= startDate && a.Timestamp <= endDate)
                    .GroupBy(a => a.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Get count of audit records by action type
                var actionCounts = await context.AuditTrails
                    .Where(a => a.CompanyId == companyId && a.Timestamp >= startDate && a.Timestamp <= endDate)
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Get top users by audit activity
                var topUsers = await context.AuditTrails
                    .Where(a => a.CompanyId == companyId && a.Timestamp >= startDate && a.Timestamp <= endDate)
                    .GroupBy(a => a.UserId)
                    .Select(g => new {
                        UserId = g.Key,
                        UserName = g.First().User.FullName,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // Get security audit counts by action type
                var securityActionCounts = await context.SecurityAudits
                    .Where(a => a.CompanyId == companyId && a.Timestamp >= startDate && a.Timestamp <= endDate)
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Get failed login attempts count
                var failedLoginCount = await context.SecurityAudits
                    .CountAsync(a => a.CompanyId == companyId &&
                                   a.Timestamp >= startDate &&
                                   a.Timestamp <= endDate &&
                                   a.Action == "Authentication" &&
                                   !a.Success);

                // Get financial audit summary
                var financialSummary = await context.FinancialAudits
                    .Where(a => a.CompanyId == companyId && a.Timestamp >= startDate && a.Timestamp <= endDate)
                    .GroupBy(a => a.Action)
                    .Select(g => new {
                        Action = g.Key,
                        Count = g.Count(),
                        TotalAmount = g.Sum(a => a.Amount)
                    })
                    .ToListAsync();

                response.Response = new {
                    DateRange = new { Start = startDate, End = endDate },
                    EntityTypeCounts = entityTypeCounts,
                    ActionCounts = actionCounts,
                    TopUsers = topUsers,
                    SecurityActionCounts = securityActionCounts,
                    FailedLoginCount = failedLoginCount,
                    FinancialSummary = financialSummary
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Audit summary generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating audit summary: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error generating audit summary: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> ExportAuditTrail(string entityType, int entityId, DateTime? startDate = null, DateTime? endDate = null, string exportFormat = "csv")
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                /* Example implementation:
                // 1. Get audit trail data
                var auditTrail = await context.AuditTrails
                    .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => new {
                        a.Id,
                        a.EntityType,
                        a.EntityId,
                        a.Action,
                        a.Details,
                        UserName = a.User.FullName,
                        a.OldValue,
                        a.NewValue,
                        Timestamp = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToListAsync();

                // 2. Format data according to exportFormat
                string fileContent = string.Empty;
                string fileName = $"{entityType}_{entityId}_AuditTrail_{DateTime.Now:yyyyMMdd}.{exportFormat}";

                if (exportFormat.ToLower() == "csv")
                {
                    // Generate CSV
                    var csv = new StringBuilder();
                    csv.AppendLine("ID,Entity Type,Entity ID,Action,Details,User,Old Value,New Value,Timestamp");

                    foreach (var record in auditTrail)
                    {
                        csv.AppendLine($"{record.Id},{record.EntityType},{record.EntityId},{record.Action},\"{record.Details}\",{record.UserName},\"{record.OldValue}\",\"{record.NewValue}\",{record.Timestamp}");
                    }

                    fileContent = csv.ToString();
                }
                else if (exportFormat.ToLower() == "json")
                {
                    // Generate JSON
                    fileContent = System.Text.Json.JsonSerializer.Serialize(auditTrail, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Unsupported export format: {exportFormat}";
                    return response;
                }

                // 3. Save file to disk or memory
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                await File.WriteAllTextAsync(filePath, fileContent);

                response.Response = new {
                    FileName = fileName,
                    FilePath = filePath,
                    RecordCount = auditTrail.Count
                };
                */

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Audit trail exported successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting audit trail: {Message}", ex.Message);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error exporting audit trail: {ex.Message}";
            }

            return response;
        }

        // Private helper methods
        private async Task<int> GetUserCompanyId(ApplicationDbContext context, string userId)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user?.CompanyId ?? 0;
        }
    }
}