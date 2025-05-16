using Roovia.Models.BusinessHelperModels;
using Roovia.Models.ProjectCdnConfigModels;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Roovia.Models.BusinessMappingModels;

namespace Roovia.Interfaces
{
    public interface IDocumentService
    {
        // CRUD Operations
        Task<ResponseModel> CreateEntityDocument(EntityDocument document);
        Task<ResponseModel> GetEntityDocumentById(int id);
        Task<ResponseModel> UpdateEntityDocument(int id, EntityDocument updatedDocument);
        Task<ResponseModel> DeleteEntityDocument(int id, string userId);

        // Document Uploads
        Task<ResponseModel> UploadDocumentForEntity(string entityType, int entityId, IFormFile file, int documentTypeId, string userId, string notes = null);
        Task<ResponseModel> UploadStreamForEntity(string entityType, int entityId, Stream fileStream, string fileName, string contentType, int documentTypeId, string userId, string notes = null);

        // Document Retrievals
        Task<ResponseModel> GetDocumentsByEntity(string entityType, int entityId);
        Task<ResponseModel> GetDocumentsByType(int documentTypeId, int companyId);
        Task<ResponseModel> GetDocumentsByStatus(int statusId, int companyId);
        Task<ResponseModel> GetRequiredMissingDocuments(string entityType, int entityId);

        // Document Status Management
        Task<ResponseModel> UpdateDocumentStatus(int documentId, int statusId, string userId, string notes = null);
        Task<ResponseModel> ApproveDocument(int documentId, string userId, string notes = null);
        Task<ResponseModel> RejectDocument(int documentId, string userId, string notes = null);

        // Document Requirements
        Task<ResponseModel> GetDocumentRequirements(string entityType, int? companyId = null);
        Task<ResponseModel> CreateDocumentRequirement(EntityDocumentRequirement requirement);
        Task<ResponseModel> UpdateDocumentRequirement(int id, EntityDocumentRequirement updatedRequirement);
        Task<ResponseModel> DeleteDocumentRequirement(int id);

        // Document Categories
        Task<ResponseModel> GetDocumentCategories();
        Task<ResponseModel> CreateDocumentCategory(DocumentCategory category);

        // Document Types
        Task<ResponseModel> GetDocumentTypes();
        Task<ResponseModel> CreateDocumentType(DocumentType documentType);

        // Search & Reporting
        Task<ResponseModel> SearchDocuments(string searchTerm, int companyId);
        Task<ResponseModel> GetDocumentStatistics(int companyId);
        Task<ResponseModel> GetDocumentsExpiringWithinDays(int days, int companyId);

        // Integration with CDN
        Task<ResponseModel> GetDocumentFileStream(int documentId);
        Task<ResponseModel> GetDocumentDownloadUrl(int documentId);
    }
}