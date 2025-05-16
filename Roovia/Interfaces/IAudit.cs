
using Roovia.Models.BusinessHelperModels;
using System;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface IAuditService
    {
        // Audit Trail Operations
        Task<ResponseModel> LogEntityChange(string entityType, int entityId, string userId, string action, string details, string oldValue = null, string newValue = null);
        Task<ResponseModel> GetEntityAuditTrail(string entityType, int entityId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
        Task<ResponseModel> GetUserAuditTrail(string userId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
        Task<ResponseModel> GetSystemAuditTrail(int companyId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);

        // Security Audit
        Task<ResponseModel> LogAuthentication(string userId, string ipAddress, bool success, string details = null);
        Task<ResponseModel> LogPermissionChange(string userId, string targetUserId, string permission, bool granted, string details = null);
        Task<ResponseModel> LogRoleChange(string userId, string targetUserId, string role, bool granted, string details = null);
        Task<ResponseModel> GetSecurityAuditTrail(int companyId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);

        // Financial Audit
        Task<ResponseModel> LogFinancialActivity(string entityType, int entityId, string userId, string action, decimal amount, string details, string referenceNumber = null);
        Task<ResponseModel> GetFinancialAuditTrail(int companyId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);

        // Data Management
        Task<ResponseModel> PurgeOldAuditTrail(int daysToKeep, int companyId = 0);
        Task<ResponseModel> ArchiveAuditTrail(DateTime cutoffDate, int companyId = 0);

        // Reports
        Task<ResponseModel> GetAuditSummary(int companyId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> ExportAuditTrail(string entityType, int entityId, DateTime? startDate = null, DateTime? endDate = null, string exportFormat = "csv");
    }
}