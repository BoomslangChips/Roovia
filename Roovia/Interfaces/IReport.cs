using Roovia.Models.BusinessHelperModels;
using Roovia.Models.ReportingModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface IReportingService
    {
        // Existing dashboard reports methods
        Task<ResponseModel> GetDashboardSummary(int companyId, int? branchId = null);
        Task<ResponseModel> GetUserDashboard(string userId);
        Task<ResponseModel> GetFinancialDashboard(int companyId, int? branchId = null);
        Task<ResponseModel> GetMaintenanceDashboard(int companyId, int? branchId = null);
        Task<ResponseModel> GetOccupancyDashboard(int companyId, int? branchId = null);

        // Existing property reports methods
        Task<ResponseModel> GetPropertyPerformanceReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetVacancyReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetPropertyValuationReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetPropertyMaintenanceReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // Existing financial reports methods
        Task<ResponseModel> GetIncomeReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetExpenseReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetCashFlowReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetArrearsReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetBeneficiaryPaymentsReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // Existing tenant reports methods
        Task<ResponseModel> GetTenantTurnoverReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<ResponseModel> GetLeaseExpiryReport(int companyId, int? branchId = null, int monthsAhead = 3);
        Task<ResponseModel> GetTenantPaymentHistoryReport(int companyId, int? branchId = null, DateTime? startDate = null, DateTime? endDate = null);

        // Existing owner reports methods
        Task<ResponseModel> GetOwnerPortfolioReport(int ownerId);
        Task<ResponseModel> GetOwnerFinancialReport(int ownerId, DateTime? startDate = null, DateTime? endDate = null);

        // Existing inspection & maintenance reports methods
        Task<ResponseModel> GetInspectionComplianceReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetMaintenanceStatusReport(int companyId, int? branchId = null);
        Task<ResponseModel> GetVendorPerformanceReport(int companyId, int? branchId = null);

        // Existing export reports method
        Task<ResponseModel> ExportReport(string reportType, Dictionary<string, object> parameters, string exportFormat);

        // Existing custom reports methods
        Task<ResponseModel> GetCustomReport(string reportName, Dictionary<string, object> parameters);
        Task<ResponseModel> SaveCustomReport(string reportName, string reportQuery, string description, Dictionary<string, object> parameters, string userId);
        Task<ResponseModel> GetSavedReports(int companyId);

        // New report scheduling methods
        Task<ResponseModel> CreateReportSchedule(ReportSchedule schedule);
        Task<ResponseModel> UpdateReportSchedule(int scheduleId, ReportSchedule updatedSchedule);
        Task<ResponseModel> DeleteReportSchedule(int scheduleId);
        Task<ResponseModel> GetReportSchedules(int companyId);
        Task<ResponseModel> GetReportSchedule(int scheduleId);
        Task<ResponseModel> ExecuteScheduledReport(int scheduleId, string userId);
        Task<ResponseModel> ProcessDueReports(string systemUserId);
        Task<ResponseModel> GetReportExecutionLogs(int companyId, int? scheduleId = null,
            DateTime? startDate = null, DateTime? endDate = null, bool? isSuccess = null,
            int page = 1, int pageSize = 20);

        // New dashboard methods
        Task<ResponseModel> CreateDashboard(ReportDashboard dashboard);
        Task<ResponseModel> UpdateDashboard(int dashboardId, ReportDashboard updatedDashboard);
        Task<ResponseModel> DeleteDashboard(int dashboardId);
        Task<ResponseModel> GetDashboards(int companyId, string userId = null);
        Task<ResponseModel> GetDashboard(int dashboardId);
        Task<ResponseModel> GetDefaultDashboard(int companyId, string userId = null);

        // New dashboard widget methods
        Task<ResponseModel> AddWidgetToDashboard(ReportDashboardWidget widget);
        Task<ResponseModel> UpdateWidget(int widgetId, ReportDashboardWidget updatedWidget);
        Task<ResponseModel> DeleteWidget(int widgetId);
        Task<ResponseModel> GetWidgetData(int widgetId);
    }
}