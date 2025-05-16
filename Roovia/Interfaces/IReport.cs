using Roovia.Models.BusinessHelperModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface IReportingService
    {
        // Dashboard Reports
        Task<ResponseModel> GetDashboardSummary(int companyId, int? branchId = null);
        Task<ResponseModel> GetUserDashboard(string userId);
        Task<ResponseModel> GetFinancialDashboard(int companyId, int? branchId = null);
        Task<ResponseModel> GetMaintenanceDashboard(int companyId, int? branchId = null);
        Task<ResponseModel> GetOccupancyDashboard(int companyId, int? branchId = null);

        // Property Reports
        Task<ResponseModel> GetPropertyPerformanceReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetVacancyReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetPropertyValuationReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetPropertyMaintenanceReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // Financial Reports
        Task<ResponseModel> GetIncomeReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetExpenseReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetCashFlowReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetArrearsReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetBeneficiaryPaymentsReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // Tenant Reports
        Task<ResponseModel> GetTenantTurnoverReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetLeaseExpiryReport(int companyId, int? branchId = null, int monthsAhead = 3);
        Task<ResponseModel> GetTenantPaymentHistoryReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // Owner Reports
        Task<ResponseModel> GetOwnerPortfolioReport(int ownerId);
        Task<ResponseModel> GetOwnerFinancialReport(int ownerId, DateTime? startDate = null, DateTime? endDate = null);

        // Inspection & Maintenance Reports
        Task<ResponseModel> GetInspectionComplianceReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetMaintenanceStatusReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetVendorPerformanceReport(int companyId, int? branchId = null);

        // Export Reports
        Task<ResponseModel> ExportReport(string reportType, Dictionary<string, object> parameters, string exportFormat);

        // Custom Reports
        Task<ResponseModel> GetCustomReport(string reportName, Dictionary<string, object> parameters);
        Task<ResponseModel> SaveCustomReport(string reportName, string reportQuery, string description, Dictionary<string, object> parameters, string userId);
        Task<ResponseModel> GetSavedReports(int companyId);
    }
}