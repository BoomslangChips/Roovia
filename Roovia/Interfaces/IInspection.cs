using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Interfaces
{
    public interface IInspection
    {
        // CRUD Operations
        Task<ResponseModel> CreateInspection(PropertyInspection inspection, int companyId);

        Task<ResponseModel> GetInspectionById(int id, int companyId);

        Task<ResponseModel> UpdateInspection(int id, PropertyInspection updatedInspection, int companyId);

        Task<ResponseModel> DeleteInspection(int id, int companyId, ApplicationUser user);

        // Listing Methods
        Task<ResponseModel> GetInspectionsByProperty(int propertyId, int companyId, int page = 1, int pageSize = 20,
            string sortField = "ScheduledDate", bool sortAscending = false, DateTime? startDate = null, DateTime? endDate = null, int? statusId = null);

        Task<ResponseModel> GetUpcomingInspections(int companyId, int days = 30, int page = 1, int pageSize = 20, int? propertyId = null);

        // Inspection Items
        Task<ResponseModel> AddInspectionItem(int inspectionId, InspectionItem item);

        Task<ResponseModel> AddInspectionItems(int inspectionId, List<InspectionItem> items);

        Task<ResponseModel> UpdateInspectionItem(int itemId, InspectionItem updatedItem);

        Task<ResponseModel> DeleteInspectionItem(int itemId);

        Task<ResponseModel> UploadInspectionItemImage(int itemId, Stream imageStream, string fileName, string contentType, string userId);

        // Report Generation
        Task<ResponseModel> GenerateInspectionReport(int inspectionId, string userId);

        // Status Management
        Task<ResponseModel> CompleteInspection(int inspectionId, string userId);

        // Additional Methods
        Task<ResponseModel> GetInspectionStatistics(int companyId, int? propertyId = null);

        Task<ResponseModel> ScheduleRecurringInspections(int propertyId, int companyId, int frequencyMonths, DateTime startDate, int? count = null, DateTime? endDate = null, string userId = null);
    }
}