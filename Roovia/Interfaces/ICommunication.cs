using Roovia.Models.BusinessHelperModels;

namespace Roovia.Interfaces
{
    public interface ICommunicationService
    {
        // CRUD Operations
        Task<ResponseModel> CreateCommunication(Communication communication);

        Task<ResponseModel> GetCommunicationById(int id);

        Task<ResponseModel> UpdateCommunication(int id, Communication updatedCommunication);

        Task<ResponseModel> DeleteCommunication(int id, string userId);

        // Listing Methods
        Task<ResponseModel> GetCommunicationsByEntity(string entityType, object entityId, int page = 1, int pageSize = 20);

        Task<ResponseModel> GetCommunicationsByUser(string userId, int page = 1, int pageSize = 20);

        Task<ResponseModel> GetCommunicationsByChannel(int channelId, int companyId, int page = 1, int pageSize = 20);

        Task<ResponseModel> GetCommunicationsByDirection(int directionId, int companyId, int page = 1, int pageSize = 20);

        Task<ResponseModel> GetCommunicationsByDateRange(DateTime startDate, DateTime endDate, int companyId, int page = 1, int pageSize = 20);

        // Email Communications
        Task<ResponseModel> SendEmail(string to, string subject, string body, string from = null, string relatedEntityType = null, object relatedEntityId = null);

        Task<ResponseModel> SendBulkEmail(List<string> recipients, string subject, string body, string from = null, string relatedEntityType = null, object relatedEntityId = null);

        Task<ResponseModel> SendTemplatedEmail(string templateName, Dictionary<string, string> templateData, string to, string from = null, string relatedEntityType = null, object relatedEntityId = null);

        // SMS Communications
        Task<ResponseModel> SendSms(string to, string message, string relatedEntityType = null, object relatedEntityId = null);

        Task<ResponseModel> SendBulkSms(List<string> recipients, string message, string relatedEntityType = null, object relatedEntityId = null);

        // Communication Templates
        Task<ResponseModel> GetCommunicationTemplates(int companyId, string templateType = null);

        Task<ResponseModel> CreateCommunicationTemplate(string name, string subject, string bodyTemplate, string templateType, int companyId);

        Task<ResponseModel> UpdateCommunicationTemplate(int templateId, string subject, string bodyTemplate);

        // Import/Export Communications
        Task<ResponseModel> ImportCommunications(string fileContent, string fileType, int companyId);

        Task<ResponseModel> ExportCommunications(string entityType, object entityId, string exportFormat);

        // Statistics
        Task<ResponseModel> GetCommunicationStatistics(int companyId, DateTime? startDate = null, DateTime? endDate = null);
    }
}