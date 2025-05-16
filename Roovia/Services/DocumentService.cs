using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.ProjectCdnConfigModels;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Roovia.Models.BusinessMappingModels;

namespace Roovia.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ICdnService _cdnService;
        private readonly IAuditService _auditService;

        public DocumentService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ICdnService cdnService,
            IAuditService auditService)
        {
            _contextFactory = contextFactory;
            _cdnService = cdnService;
            _auditService = auditService;
        }

        public async Task<ResponseModel> CreateEntityDocument(EntityDocument document)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Set creation date
                document.CreatedOn = DateTime.Now;
                
                await context.EntityDocuments.AddAsync(document);
                await context.SaveChangesAsync();
                
                // Log the document creation
                await _auditService.LogEntityChange(
                    document.EntityType,
                    document.EntityId,
                    document.CreatedBy,
                    "CreateDocument",
                    $"Created document: {document.DocumentType?.Name}");
                
                response.Response = document;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetEntityDocumentById(int id)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var document = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .FirstOrDefaultAsync(d => d.Id == id);
                
                if (document == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document with ID {id} not found";
                    return response;
                }
                
                response.Response = document;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> UpdateEntityDocument(int id, EntityDocument updatedDocument)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var document = await context.EntityDocuments.FindAsync(id);
                
                if (document == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document with ID {id} not found";
                    return response;
                }
                
                // Update document properties
                document.DocumentTypeId = updatedDocument.DocumentTypeId;
                document.DocumentStatusId = updatedDocument.DocumentStatusId;
                document.IsRequired = updatedDocument.IsRequired;
                document.Notes = updatedDocument.Notes;
                document.UpdatedDate = DateTime.Now;
                document.UpdatedBy = updatedDocument.UpdatedBy;
                
                await context.SaveChangesAsync();
                
                // Log the update
                await _auditService.LogEntityChange(
                    document.EntityType,
                    document.EntityId,
                    document.UpdatedBy,
                    "UpdateDocument",
                    $"Updated document: {document.DocumentType?.Name}");
                
                response.Response = document;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> DeleteEntityDocument(int id, string userId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                var document = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .FirstOrDefaultAsync(d => d.Id == id);
                
                if (document == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document with ID {id} not found";
                    return response;
                }
                
                // Store values for audit logging
                string entityType = document.EntityType;
                int entityId = document.EntityId;
                string documentType = document.DocumentType?.Name ?? "Unknown";
                
                // Delete actual file from CDN if it exists
                if (document.CdnFileMetadataId.HasValue)
                {
                    var fileMetadata = await context.CdnFileMetadata.FindAsync(document.CdnFileMetadataId.Value);
                    if (fileMetadata != null)
                    {
                        await _cdnService.DeleteFileAsync(fileMetadata.Url);
                    }
                }
                
                // Delete the document record
                context.EntityDocuments.Remove(document);
                await context.SaveChangesAsync();
                
                // Log the deletion
                await _auditService.LogEntityChange(
                    entityType,
                    entityId,
                    userId,
                    "DeleteDocument",
                    $"Deleted document: {documentType}");
                
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> UploadDocumentForEntity(string entityType, int entityId, IFormFile file, int documentTypeId, string userId, string notes = null)
        {
            var response = new ResponseModel();
            
            try
            {
                if (file == null || file.Length == 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File is empty or not provided";
                    return response;
                }
                
                using var context = _contextFactory.CreateDbContext();
                
                // Get document type
                var documentType = await context.DocumentTypes.FindAsync(documentTypeId);
                if (documentType == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document type with ID {documentTypeId} not found";
                    return response;
                }
                
                // Get default document status (e.g., "Pending Review")
                var pendingStatus = await context.DocumentStatuses
                    .FirstOrDefaultAsync(ds => ds.Name.ToLower() == "pending review");
                
                if (pendingStatus == null)
                {
                    // If status not found, get the first active status
                    pendingStatus = await context.DocumentStatuses.FirstOrDefaultAsync(ds => ds.IsActive);
                    
                    if (pendingStatus == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "No document status found";
                        return response;
                    }
                }
                
                // Determine folder path based on entity type
                string folderPath = $"{entityType.ToLower()}/{entityId}";
                
                // Upload the file to CDN
                using var stream = file.OpenReadStream();
                string cdnUrl = await _cdnService.UploadFileAsync(stream, file.FileName, file.ContentType, "documents", folderPath);
                
                // Get file metadata
                var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                
                // Create entity document
                var document = new EntityDocument
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    DocumentTypeId = documentTypeId,
                    CdnFileMetadataId = fileMetadata?.Id,
                    DocumentStatusId = pendingStatus.Id,
                    IsRequired = false, // Default to false unless specified
                    Notes = notes,
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };
                
                await context.EntityDocuments.AddAsync(document);
                await context.SaveChangesAsync();
                
                // Log the document upload
                await _auditService.LogEntityChange(
                    entityType,
                    entityId,
                    userId,
                    "UploadDocument",
                    $"Uploaded document: {documentType.Name}, File: {file.FileName}");
                
                response.Response = document;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document uploaded successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error uploading document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> UploadStreamForEntity(string entityType, int entityId, Stream fileStream, string fileName, string contentType, int documentTypeId, string userId, string notes = null)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get document type
                var documentType = await context.DocumentTypes.FindAsync(documentTypeId);
                if (documentType == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document type with ID {documentTypeId} not found";
                    return response;
                }
                
                // Get default document status (e.g., "Pending Review")
                var pendingStatus = await context.DocumentStatuses
                    .FirstOrDefaultAsync(ds => ds.Name.ToLower() == "pending review");
                
                if (pendingStatus == null)
                {
                    // If status not found, get the first active status
                    pendingStatus = await context.DocumentStatuses.FirstOrDefaultAsync(ds => ds.IsActive);
                    
                    if (pendingStatus == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "No document status found";
                        return response;
                    }
                }
                
                // Determine folder path based on entity type
                string folderPath = $"{entityType.ToLower()}/{entityId}";
                
                // Upload the file to CDN
                string cdnUrl = await _cdnService.UploadFileAsync(fileStream, fileName, contentType, "documents", folderPath);
                
                // Get file metadata
                var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                
                // Create entity document
                var document = new EntityDocument
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    DocumentTypeId = documentTypeId,
                    CdnFileMetadataId = fileMetadata?.Id,
                    DocumentStatusId = pendingStatus.Id,
                    IsRequired = false, // Default to false unless specified
                    Notes = notes,
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };
                
                await context.EntityDocuments.AddAsync(document);
                await context.SaveChangesAsync();
                
                // Log the document upload
                await _auditService.LogEntityChange(
                    entityType,
                    entityId,
                    userId,
                    "UploadDocument",
                    $"Uploaded document: {documentType.Name}, File: {fileName}");
                
                response.Response = document;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document uploaded successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error uploading document stream: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentsByEntity(string entityType, int entityId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                var documents = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .Where(d => d.EntityType == entityType && d.EntityId == entityId)
                    .OrderBy(d => d.CreatedOn)
                    .ToListAsync();
                
                // Get CDN file metadata for each document
                var documentIds = documents.Where(d => d.CdnFileMetadataId.HasValue)
                    .Select(d => d.CdnFileMetadataId.Value)
                    .ToList();
                
                var fileMetadata = await context.CdnFileMetadata
                    .Where(f => documentIds.Contains(f.Id))
                    .ToListAsync();
                
                // Combine results
                var result = documents.Select(d => new
                {
                    Document = d,
                    CdnFileMetadata = d.CdnFileMetadataId.HasValue ? 
                        fileMetadata.FirstOrDefault(f => f.Id == d.CdnFileMetadataId) : null
                }).ToList();
                
                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving documents: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentsByType(int documentTypeId, int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get entity IDs for this company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();
                
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();
                
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                // Query for documents of this type related to company entities
                var documents = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .Where(d => d.DocumentTypeId == documentTypeId)
                    .Where(d => 
                        (d.EntityType == "Property" && propertyIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyOwner" && ownerIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyTenant" && tenantIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(d.EntityId)) ||
                        (d.EntityType == "Company" && d.EntityId == companyId)
                    )
                    .OrderByDescending(d => d.CreatedOn)
                    .ToListAsync();
                
                // Get CDN file metadata for each document
                var documentIds = documents.Where(d => d.CdnFileMetadataId.HasValue)
                    .Select(d => d.CdnFileMetadataId.Value)
                    .ToList();
                
                var fileMetadata = await context.CdnFileMetadata
                    .Where(f => documentIds.Contains(f.Id))
                    .ToListAsync();
                
                // Combine results
                var result = documents.Select(d => new
                {
                    Document = d,
                    CdnFileMetadata = d.CdnFileMetadataId.HasValue ? 
                        fileMetadata.FirstOrDefault(f => f.Id == d.CdnFileMetadataId) : null
                }).ToList();
                
                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving documents by type: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentsByStatus(int statusId, int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get entity IDs for this company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();
                
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();
                
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                // Query for documents with this status related to company entities
                var documents = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .Where(d => d.DocumentStatusId == statusId)
                    .Where(d => 
                        (d.EntityType == "Property" && propertyIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyOwner" && ownerIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyTenant" && tenantIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(d.EntityId)) ||
                        (d.EntityType == "Company" && d.EntityId == companyId)
                    )
                    .OrderByDescending(d => d.CreatedOn)
                    .ToListAsync();
                
                // Get CDN file metadata for each document
                var documentIds = documents.Where(d => d.CdnFileMetadataId.HasValue)
                    .Select(d => d.CdnFileMetadataId.Value)
                    .ToList();
                
                var fileMetadata = await context.CdnFileMetadata
                    .Where(f => documentIds.Contains(f.Id))
                    .ToListAsync();
                
                // Combine results
                var result = documents.Select(d => new
                {
                    Document = d,
                    CdnFileMetadata = d.CdnFileMetadataId.HasValue ? 
                        fileMetadata.FirstOrDefault(f => f.Id == d.CdnFileMetadataId) : null
                }).ToList();
                
                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving documents by status: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetRequiredMissingDocuments(string entityType, int entityId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get company ID for this entity
                int? companyId = null;
                
                switch (entityType.ToLower())
                {
                    case "property":
                        companyId = await context.Properties
                            .Where(p => p.Id == entityId)
                            .Select(p => p.CompanyId)
                            .FirstOrDefaultAsync();
                        break;
                    
                    case "propertyowner":
                        companyId = await context.PropertyOwners
                            .Where(o => o.Id == entityId)
                            .Select(o => o.CompanyId)
                            .FirstOrDefaultAsync();
                        break;
                    
                    case "propertytenant":
                        companyId = await context.PropertyTenants
                            .Where(t => t.Id == entityId)
                            .Select(t => t.CompanyId)
                            .FirstOrDefaultAsync();
                        break;
                    
                    case "propertybeneficiary":
                        companyId = await context.PropertyBeneficiaries
                            .Where(b => b.Id == entityId)
                            .Select(b => b.CompanyId)
                            .FirstOrDefaultAsync();
                        break;
                    
                    case "company":
                        companyId = entityId;
                        break;
                }
                
                if (!companyId.HasValue)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Could not determine company for this entity";
                    return response;
                }
                
                // Find entity type ID
                var entityTypeObj = await context.EntityTypes
                    .FirstOrDefaultAsync(et => et.Name.ToLower() == entityType.ToLower());
                
                if (entityTypeObj == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Entity type '{entityType}' not found";
                    return response;
                }
                
                // Get required document requirements for this entity type
                var requirements = await context.EntityDocumentRequirements
                    .Include(r => r.DocumentType)
                    .Include(r => r.DocumentRequirementType)
                    .Where(r => r.EntityTypeId == entityTypeObj.Id)
                    .Where(r => r.DocumentRequirementType.Name.ToLower() == "required")
                    .Where(r => r.CompanyId == null || r.CompanyId == companyId.Value)
                    .Where(r => r.IsActive)
                    .ToListAsync();
                
                // Get existing documents for this entity
                var existingDocuments = await context.EntityDocuments
                    .Where(d => d.EntityType == entityType && d.EntityId == entityId)
                    .ToListAsync();
                
                // Find missing required documents
                var missingDocuments = requirements
                    .Where(r => !existingDocuments.Any(d => d.DocumentTypeId == r.DocumentTypeId))
                    .Select(r => new
                    {
                        Requirement = r,
                        DocumentType = r.DocumentType
                    })
                    .ToList();
                
                response.Response = missingDocuments;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving required missing documents: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> UpdateDocumentStatus(int documentId, int statusId, string userId, string notes = null)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                var document = await context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                
                if (document == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document with ID {documentId} not found";
                    return response;
                }
                
                var newStatus = await context.DocumentStatuses.FindAsync(statusId);
                if (newStatus == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document status with ID {statusId} not found";
                    return response;
                }
                
                // Update document status
                int oldStatusId = document.DocumentStatusId;
                string oldStatusName = document.DocumentStatus?.Name ?? "Unknown";
                
                document.DocumentStatusId = statusId;
                document.UpdatedDate = DateTime.Now;
                document.UpdatedBy = userId;
                
                if (!string.IsNullOrEmpty(notes))
                {
                    // Append new notes if provided
                    if (string.IsNullOrEmpty(document.Notes))
                    {
                        document.Notes = notes;
                    }
                    else
                    {
                        document.Notes += $"\n\n{DateTime.Now:yyyy-MM-dd HH:mm} - {userId}:\n{notes}";
                    }
                }
                
                await context.SaveChangesAsync();
                
                // Log the status change
                await _auditService.LogEntityChange(
                    document.EntityType,
                    document.EntityId,
                    userId,
                    "UpdateDocumentStatus",
                    $"Changed document status from '{oldStatusName}' to '{newStatus.Name}' for document: {document.DocumentType?.Name}");
                
                response.Response = document;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Document status updated to '{newStatus.Name}'";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating document status: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> ApproveDocument(int documentId, string userId, string notes = null)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Find approved status
                var approvedStatus = await context.DocumentStatuses
                    .FirstOrDefaultAsync(ds => ds.Name.ToLower() == "approved");
                
                if (approvedStatus == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Approved status not found";
                    return response;
                }
                
                return await UpdateDocumentStatus(documentId, approvedStatus.Id, userId, notes);
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error approving document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> RejectDocument(int documentId, string userId, string notes = null)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Find rejected status
                var rejectedStatus = await context.DocumentStatuses
                    .FirstOrDefaultAsync(ds => ds.Name.ToLower() == "rejected");
                
                if (rejectedStatus == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Rejected status not found";
                    return response;
                }
                
                if (string.IsNullOrEmpty(notes))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Rejection reason (notes) is required";
                    return response;
                }
                
                return await UpdateDocumentStatus(documentId, rejectedStatus.Id, userId, notes);
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error rejecting document: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentRequirements(string entityType, int? companyId = null)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Find entity type ID
                var entityTypeObj = await context.EntityTypes
                    .FirstOrDefaultAsync(et => et.Name.ToLower() == entityType.ToLower());
                
                if (entityTypeObj == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Entity type '{entityType}' not found";
                    return response;
                }
                
                // Get document requirements for this entity type
                var query = context.EntityDocumentRequirements
                    .Include(r => r.DocumentType)
                    .Include(r => r.DocumentRequirementType)
                    .Where(r => r.EntityTypeId == entityTypeObj.Id)
                    .Where(r => r.IsActive);
                
                if (companyId.HasValue)
                {
                    // If company ID provided, include both company-specific and system-wide requirements
                    query = query.Where(r => r.CompanyId == null || r.CompanyId == companyId.Value);
                }
                else
                {
                    // If no company ID, only include system-wide requirements
                    query = query.Where(r => r.CompanyId == null);
                }
                
                var requirements = await query
                    .OrderBy(r => r.DocumentType.DisplayOrder)
                    .ThenBy(r => r.DocumentType.Name)
                    .ToListAsync();
                
                response.Response = requirements;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document requirements: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> CreateDocumentRequirement(EntityDocumentRequirement requirement)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Check if requirement already exists
                var existingRequirement = await context.EntityDocumentRequirements
                    .FirstOrDefaultAsync(r => 
                        r.EntityTypeId == requirement.EntityTypeId && 
                        r.DocumentTypeId == requirement.DocumentTypeId && 
                        ((r.CompanyId == null && requirement.CompanyId == null) || 
                         (r.CompanyId == requirement.CompanyId)));
                
                if (existingRequirement != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Document requirement already exists";
                    return response;
                }
                
                await context.EntityDocumentRequirements.AddAsync(requirement);
                await context.SaveChangesAsync();
                
                response.Response = requirement;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document requirement created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating document requirement: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> UpdateDocumentRequirement(int id, EntityDocumentRequirement updatedRequirement)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                var requirement = await context.EntityDocumentRequirements.FindAsync(id);
                
                if (requirement == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document requirement with ID {id} not found";
                    return response;
                }
                
                // Update fields
                requirement.DocumentRequirementTypeId = updatedRequirement.DocumentRequirementTypeId;
                requirement.IsDefault = updatedRequirement.IsDefault;
                requirement.Description = updatedRequirement.Description;
                requirement.IsActive = updatedRequirement.IsActive;
                
                await context.SaveChangesAsync();
                
                response.Response = requirement;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document requirement updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating document requirement: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> DeleteDocumentRequirement(int id)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                var requirement = await context.EntityDocumentRequirements.FindAsync(id);
                
                if (requirement == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document requirement with ID {id} not found";
                    return response;
                }
                
                context.EntityDocumentRequirements.Remove(requirement);
                await context.SaveChangesAsync();
                
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document requirement deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting document requirement: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentCategories()
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                var categories = await context.DocumentCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();
                
                response.Response = categories;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document categories: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> CreateDocumentCategory(DocumentCategory category)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Check if category already exists
                var existingCategory = await context.DocumentCategories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == category.Name.ToLower());
                
                if (existingCategory != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document category '{category.Name}' already exists";
                    return response;
                }
                
                await context.DocumentCategories.AddAsync(category);
                await context.SaveChangesAsync();
                
                response.Response = category;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document category created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating document category: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentTypes()
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                var types = await context.DocumentTypes
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
                
                response.Response = types;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document types: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> CreateDocumentType(DocumentType documentType)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Check if document type already exists
                var existingType = await context.DocumentTypes
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == documentType.Name.ToLower());
                
                if (existingType != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Document type '{documentType.Name}' already exists";
                    return response;
                }
                
                await context.DocumentTypes.AddAsync(documentType);
                await context.SaveChangesAsync();
                
                response.Response = documentType;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document type created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating document type: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> SearchDocuments(string searchTerm, int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Search term cannot be empty";
                    return response;
                }
                
                using var context = _contextFactory.CreateDbContext();
                
                // Get entity IDs for this company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();
                
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();
                
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                // First get document IDs that match from entity documents
                var documentQuery = context.EntityDocuments
                    .Include(d => d.DocumentType)
                    .Include(d => d.DocumentStatus)
                    .Where(d => 
                        (d.EntityType == "Property" && propertyIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyOwner" && ownerIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyTenant" && tenantIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(d.EntityId)) ||
                        (d.EntityType == "Company" && d.EntityId == companyId)
                    );
                
                // Get the document type IDs that match the search term
                var docTypeIds = await context.DocumentTypes
                    .Where(dt => dt.Name.Contains(searchTerm) || dt.Description.Contains(searchTerm))
                    .Select(dt => dt.Id)
                    .ToListAsync();
                
                if (docTypeIds.Any())
                {
                    documentQuery = documentQuery.Where(d => docTypeIds.Contains(d.DocumentTypeId) || d.Notes.Contains(searchTerm));
                }
                else
                {
                    documentQuery = documentQuery.Where(d => d.Notes.Contains(searchTerm));
                }
                
                var documents = await documentQuery
                    .OrderByDescending(d => d.CreatedOn)
                    .ToListAsync();
                
                // Get CDN file metadata that matches file name
                var cdnIds = documents
                    .Where(d => d.CdnFileMetadataId.HasValue)
                    .Select(d => d.CdnFileMetadataId.Value)
                    .ToList();
                
                // Also search in CdnFileMetadata for file names that match
                var fileMetadataIds = await context.CdnFileMetadata
                    .Where(f => f.FileName.Contains(searchTerm))
                    .Select(f => f.Id)
                    .ToListAsync();
                
                // Add any documents that have matching file names to the result
                if (fileMetadataIds.Any())
                {
                    var additionalDocs = await context.EntityDocuments
                        .Include(d => d.DocumentType)
                        .Include(d => d.DocumentStatus)
                        .Where(d => fileMetadataIds.Contains(d.CdnFileMetadataId ?? 0))
                        .Where(d => 
                            (d.EntityType == "Property" && propertyIds.Contains(d.EntityId)) ||
                            (d.EntityType == "PropertyOwner" && ownerIds.Contains(d.EntityId)) ||
                            (d.EntityType == "PropertyTenant" && tenantIds.Contains(d.EntityId)) ||
                            (d.EntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(d.EntityId)) ||
                            (d.EntityType == "Company" && d.EntityId == companyId)
                        )
                        .OrderByDescending(d => d.CreatedOn)
                        .ToListAsync();
                    
                    // Merge and de-duplicate
                    var allDocIds = documents.Select(d => d.Id).ToList();
                    foreach (var doc in additionalDocs)
                    {
                        if (!allDocIds.Contains(doc.Id))
                        {
                            documents.Add(doc);
                            allDocIds.Add(doc.Id);
                            
                            if (doc.CdnFileMetadataId.HasValue)
                            {
                                cdnIds.Add(doc.CdnFileMetadataId.Value);
                            }
                        }
                    }
                }
                
                // Get all file metadata for the documents
                var fileMetadata = await context.CdnFileMetadata
                    .Where(f => cdnIds.Contains(f.Id))
                    .ToListAsync();
                
                // Combine results
                var result = documents.Select(d => new
                {
                    Document = d,
                    CdnFileMetadata = d.CdnFileMetadataId.HasValue ? 
                        fileMetadata.FirstOrDefault(f => f.Id == d.CdnFileMetadataId) : null,
                    EntityType = d.EntityType,
                    EntityId = d.EntityId,
                    // Add entity details based on type
                    EntityDetails = GetEntityDetails(d.EntityType, d.EntityId, context)
                }).ToList();
                
                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error searching documents: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentStatistics(int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get entity IDs for this company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();
                
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();
                
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();
                
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();
                
                // Define the filtering condition for documents related to this company
                var companyDocumentsQuery = context.EntityDocuments
                    .Where(d => 
                        (d.EntityType == "Property" && propertyIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyOwner" && ownerIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyTenant" && tenantIds.Contains(d.EntityId)) ||
                        (d.EntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(d.EntityId)) ||
                        (d.EntityType == "Company" && d.EntityId == companyId)
                    );
                
                // Calculate statistics
                var totalDocuments = await companyDocumentsQuery.CountAsync();
                
                var documentsByType = await companyDocumentsQuery
                    .GroupBy(d => d.DocumentTypeId)
                    .Select(g => new { TypeId = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var documentsByStatus = await companyDocumentsQuery
                    .GroupBy(d => d.DocumentStatusId)
                    .Select(g => new { StatusId = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var documentsByEntityType = await companyDocumentsQuery
                    .GroupBy(d => d.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var documentsByMonth = await companyDocumentsQuery
                    .GroupBy(d => new { Year = d.CreatedOn.Year, Month = d.CreatedOn.Month })
                    .Select(g => new 
                    { 
                        Year = g.Key.Year, 
                        Month = g.Key.Month, 
                        YearMonth = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Count = g.Count() 
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();
                
                // Get names for types and statuses
                var documentTypes = await context.DocumentTypes.ToListAsync();
                var documentStatuses = await context.DocumentStatuses.ToListAsync();
                
                // Find missing required documents
                var entityTypes = await context.EntityTypes
                    .Where(et => new[] { "property", "propertyowner", "propertytenant", "propertybeneficiary" }
                        .Contains(et.Name.ToLower()))
                    .ToListAsync();
                
                var requiredDocumentCounts = new List<object>();
                
                foreach (var entityType in entityTypes)
                {
                    // Get required document types for this entity type
                    var requiredDocTypes = await context.EntityDocumentRequirements
                        .Where(r => r.EntityTypeId == entityType.Id)
                        .Where(r => r.DocumentRequirementType.Name.ToLower() == "required")
                        .Where(r => r.CompanyId == null || r.CompanyId == companyId)
                        .Where(r => r.IsActive)
                        .Select(r => r.DocumentTypeId)
                        .ToListAsync();
                    
                    if (!requiredDocTypes.Any())
                        continue;
                    
                    // Count entities missing required documents
                    int missingCount = 0;
                    
                    switch (entityType.Name.ToLower())
                    {
                        case "property":
                            foreach (var propertyId in propertyIds)
                            {
                                var existingDocs = await context.EntityDocuments
                                    .Where(d => d.EntityType == "Property" && d.EntityId == propertyId)
                                    .Select(d => d.DocumentTypeId)
                                    .ToListAsync();
                                
                                bool hasMissing = requiredDocTypes.Any(rt => !existingDocs.Contains(rt));
                                if (hasMissing)
                                    missingCount++;
                            }
                            break;
                        
                        case "propertyowner":
                            foreach (var ownerId in ownerIds)
                            {
                                var existingDocs = await context.EntityDocuments
                                    .Where(d => d.EntityType == "PropertyOwner" && d.EntityId == ownerId)
                                    .Select(d => d.DocumentTypeId)
                                    .ToListAsync();
                                
                                bool hasMissing = requiredDocTypes.Any(rt => !existingDocs.Contains(rt));
                                if (hasMissing)
                                    missingCount++;
                            }
                            break;
                        
                        case "propertytenant":
                            foreach (var tenantId in tenantIds)
                            {
                                var existingDocs = await context.EntityDocuments
                                    .Where(d => d.EntityType == "PropertyTenant" && d.EntityId == tenantId)
                                    .Select(d => d.DocumentTypeId)
                                    .ToListAsync();
                                
                                bool hasMissing = requiredDocTypes.Any(rt => !existingDocs.Contains(rt));
                                if (hasMissing)
                                    missingCount++;
                            }
                            break;
                        
                        case "propertybeneficiary":
                            foreach (var beneficiaryId in beneficiaryIds)
                            {
                                var existingDocs = await context.EntityDocuments
                                    .Where(d => d.EntityType == "PropertyBeneficiary" && d.EntityId == beneficiaryId)
                                    .Select(d => d.DocumentTypeId)
                                    .ToListAsync();
                                
                                bool hasMissing = requiredDocTypes.Any(rt => !existingDocs.Contains(rt));
                                if (hasMissing)
                                    missingCount++;
                            }
                            break;
                    }
                    
                    requiredDocumentCounts.Add(new
                    {
                        EntityType = entityType.Name,
                        TotalCount = entityType.Name.ToLower() switch
                        {
                            "property" => propertyIds.Count,
                            "propertyowner" => ownerIds.Count,
                            "propertytenant" => tenantIds.Count,
                            "propertybeneficiary" => beneficiaryIds.Count,
                            _ => 0
                        },
                        MissingRequiredDocsCount = missingCount,
                        CompliancePercentage = entityType.Name.ToLower() switch
                        {
                            "property" => propertyIds.Count > 0 ? 
                                Math.Round(100 - ((double)missingCount / propertyIds.Count * 100), 1) : 0,
                            "propertyowner" => ownerIds.Count > 0 ? 
                                Math.Round(100 - ((double)missingCount / ownerIds.Count * 100), 1) : 0,
                            "propertytenant" => tenantIds.Count > 0 ? 
                                Math.Round(100 - ((double)missingCount / tenantIds.Count * 100), 1) : 0,
                            "propertybeneficiary" => beneficiaryIds.Count > 0 ? 
                                Math.Round(100 - ((double)missingCount / beneficiaryIds.Count * 100), 1) : 0,
                            _ => 0
                        }
                    });
                }
                
                // Combine results
                var statistics = new
                {
                    TotalDocuments = totalDocuments,
                    DocumentsByType = documentsByType.Select(d => new 
                    {
                        TypeId = d.TypeId,
                        TypeName = documentTypes.FirstOrDefault(t => t.Id == d.TypeId)?.Name,
                        Count = d.Count,
                        Percentage = totalDocuments > 0 ? Math.Round((double)d.Count / totalDocuments * 100, 1) : 0
                    }),
                    DocumentsByStatus = documentsByStatus.Select(d => new 
                    {
                        StatusId = d.StatusId,
                        StatusName = documentStatuses.FirstOrDefault(s => s.Id == d.StatusId)?.Name,
                        Count = d.Count,
                        Percentage = totalDocuments > 0 ? Math.Round((double)d.Count / totalDocuments * 100, 1) : 0
                    }),
                    DocumentsByEntityType = documentsByEntityType,
                    DocumentsByMonth = documentsByMonth,
                    RequiredDocumentCompliance = requiredDocumentCounts
                };
                
                response.Response = statistics;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document statistics: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentsExpiringWithinDays(int days, int companyId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // This method would need a way to track document expiry dates
                // Since the current data model doesn't have an expiry date field,
                // we'll just return a message

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Document expiry tracking is not implemented in the current data model";
                response.Response = new List<object>();
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving expiring documents: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentFileStream(int documentId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get the document
                var document = await context.EntityDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                
                if (document == null || !document.CdnFileMetadataId.HasValue)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Document not found or has no file associated";
                    return response;
                }
                
                // Get file metadata
                var fileMetadata = await context.CdnFileMetadata
                    .FirstOrDefaultAsync(f => f.Id == document.CdnFileMetadataId.Value);
                
                if (fileMetadata == null || string.IsNullOrEmpty(fileMetadata.Url))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File metadata not found";
                    return response;
                }
                
                // Get file stream from CDN
                var (stream, isFromBase64) = await _cdnService.GetFileStreamAsync(fileMetadata.Url);
                
                response.Response = new 
                {
                    Stream = stream,
                    IsFromBase64 = isFromBase64,
                    FileName = fileMetadata.FileName,
                    ContentType = fileMetadata.ContentType,
                    FileSize = fileMetadata.FileSize
                };
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document file stream: {ex.Message}";
            }
            
            return response;
        }

        public async Task<ResponseModel> GetDocumentDownloadUrl(int documentId)
        {
            var response = new ResponseModel();
            
            try
            {
                using var context = _contextFactory.CreateDbContext();
                
                // Get the document
                var document = await context.EntityDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                
                if (document == null || !document.CdnFileMetadataId.HasValue)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Document not found or has no file associated";
                    return response;
                }
                
                // Get file metadata
                var fileMetadata = await context.CdnFileMetadata
                    .FirstOrDefaultAsync(f => f.Id == document.CdnFileMetadataId.Value);
                
                if (fileMetadata == null || string.IsNullOrEmpty(fileMetadata.Url))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File metadata not found";
                    return response;
                }
                
                // Update access stats
                fileMetadata.AccessCount++;
                fileMetadata.LastAccessDate = DateTime.Now;
                await context.SaveChangesAsync();
                
                // Log the access
                await _cdnService.LogAccessAsync(
                    "Download", 
                    fileMetadata.FilePath, 
                    "System", // Placeholder for user
                    true, 
                    null, 
                    fileMetadata.FileSize);
                
                response.Response = fileMetadata.Url;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving document download URL: {ex.Message}";
            }
            
            return response;
        }

        #region Helper Methods
        
        private object GetEntityDetails(string entityType, int entityId, ApplicationDbContext context)
        {
            // This method would normally be more comprehensive and async,
            // but for brevity, we're using a simplified approach
            
            switch (entityType.ToLower())
            {
                case "property":
                    var property = context.Properties
                        .FirstOrDefault(p => p.Id == entityId);
                    
                    return property != null ? new { Name = property.PropertyName, Code = property.PropertyCode } : null;
                
                case "propertyowner":
                    var owner = context.PropertyOwners
                        .FirstOrDefault(o => o.Id == entityId);
                    
                    return owner != null ? new { Name = owner.DisplayName } : null;
                
                case "propertytenant":
                    var tenant = context.PropertyTenants
                        .FirstOrDefault(t => t.Id == entityId);
                    
                    return tenant != null ? new { Name = tenant.DisplayName } : null;
                
                case "propertybeneficiary":
                    var beneficiary = context.PropertyBeneficiaries
                        .FirstOrDefault(b => b.Id == entityId);
                    
                    return beneficiary != null ? new { Name = beneficiary.Name } : null;
                
                case "company":
                    var company = context.Companies
                        .FirstOrDefault(c => c.Id == entityId);
                    
                    return company != null ? new { Name = company.Name } : null;
                
                default:
                    return null;
            }
        }
        
        #endregion
    }
}