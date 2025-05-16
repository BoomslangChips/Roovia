using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IMaintenance
    {
        // CRUD Operations
        Task<ResponseModel> CreateMaintenanceTicket(MaintenanceTicket ticket, int companyId);

        Task<ResponseModel> GetMaintenanceTicketById(int id, int companyId);

        Task<ResponseModel> UpdateMaintenanceTicket(int id, MaintenanceTicket updatedTicket, int companyId);

        Task<ResponseModel> DeleteMaintenanceTicket(int id, int companyId, ApplicationUser user);

        // Listing Methods
        Task<ResponseModel> GetMaintenanceTicketsByProperty(int propertyId, int companyId);

        Task<ResponseModel> GetOpenTicketsByCompany(int companyId);

        // Ticket Management
        Task<ResponseModel> AssignTicketToVendor(int ticketId, int vendorId, string userId);

        Task<ResponseModel> CompleteMaintenanceTicket(int ticketId, string completionNotes, bool issueResolved, string userId);

        Task<ResponseModel> ApproveMaintenanceTicket(int ticketId, string userId);

        // Comments and Expenses
        Task<ResponseModel> AddComment(int ticketId, MaintenanceComment comment, string userId);

        Task<ResponseModel> AddExpense(int ticketId, MaintenanceExpense expense);

        // Document Management
        Task<ResponseModel> UploadMaintenanceExpenseReceipt(int expenseId, IFormFile file, string userId);

        Task<ResponseModel> GetMaintenanceDocuments(int ticketId, int companyId);

        Task<ResponseModel> UploadMaintenanceImage(int ticketId, IFormFile file, string category, string userId);

        // Statistics
        Task<ResponseModel> GetMaintenanceStatistics(int companyId);
    }
}