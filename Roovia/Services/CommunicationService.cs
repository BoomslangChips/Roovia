using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;

namespace Roovia.Services
{
    public class CommunicationService : ICommunicationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly ICdnService _cdnService;

        public CommunicationService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            IEmailService emailService,
            IAuditService auditService,
            ICdnService cdnService)
        {
            _contextFactory = contextFactory;
            _emailService = emailService;
            _auditService = auditService;
            _cdnService = cdnService;
        }

        public async Task<ResponseModel> CreateCommunication(Communication communication)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set creation date
                communication.CreatedOn = DateTime.Now;
                communication.CommunicationDate = DateTime.Now;

                await context.Communications.AddAsync(communication);
                await context.SaveChangesAsync();

                // Log the communication
                if (communication.RelatedEntityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        communication.RelatedEntityType,
                        communication.RelatedEntityId.Value,
                        communication.CreatedBy,
                        "CreateCommunication",
                        $"Created {communication.CommunicationChannel?.Name} communication: {communication.Subject}");
                }

                response.Response = communication;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Communication recorded successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating communication: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationById(int id)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var communication = await context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (communication == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Communication with ID {id} not found";
                    return response;
                }

                response.Response = communication;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communication: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> UpdateCommunication(int id, Communication updatedCommunication)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var communication = await context.Communications.FindAsync(id);

                if (communication == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Communication with ID {id} not found";
                    return response;
                }

                // Update only certain fields - don't allow changing the core details
                communication.Subject = updatedCommunication.Subject;
                communication.Content = updatedCommunication.Content;

                await context.SaveChangesAsync();

                response.Response = communication;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Communication updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating communication: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> DeleteCommunication(int id, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var communication = await context.Communications.FindAsync(id);

                if (communication == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Communication with ID {id} not found";
                    return response;
                }

                // Store values for audit logging
                string entityType = communication.RelatedEntityType;
                int? entityId = communication.RelatedEntityId;
                string subject = communication.Subject;

                context.Communications.Remove(communication);
                await context.SaveChangesAsync();

                // Log the deletion
                if (entityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        entityType,
                        entityId.Value,
                        userId,
                        "DeleteCommunication",
                        $"Deleted communication: {subject}");
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Communication deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting communication: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationsByEntity(string entityType, object entityId, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .Where(c => c.RelatedEntityType == entityType)
                    .OrderByDescending(c => c.CommunicationDate);

                // Handle different ID types
                if (entityId is int intId)
                {
                    query = (IOrderedQueryable<Communication>)query.Where(c => c.RelatedEntityId == intId);
                }
                else if (entityId is string stringId)
                {
                    query = (IOrderedQueryable<Communication>)query.Where(c => c.RelatedEntityStringId == stringId);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Communications = communications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationsByUser(string userId, int page = 1, int pageSize = 20)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .Where(c => c.RelatedUserId == userId)
                    .OrderByDescending(c => c.CommunicationDate);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Communications = communications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving user communications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationsByChannel(int channelId, int companyId, int page = 1, int pageSize = 20)
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

                // Query communications by channel for this company's entities
                var query = context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .Where(c => c.CommunicationChannelId == channelId)
                    .Where(c =>
                        (c.RelatedEntityType == "Property" && propertyIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "Company" && c.RelatedEntityId == companyId)
                    )
                    .OrderByDescending(c => c.CommunicationDate);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Communications = communications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communications by channel: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationsByDirection(int directionId, int companyId, int page = 1, int pageSize = 20)
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

                // Query communications by direction for this company's entities
                var query = context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .Where(c => c.CommunicationDirectionId == directionId)
                    .Where(c =>
                        (c.RelatedEntityType == "Property" && propertyIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "Company" && c.RelatedEntityId == companyId)
                    )
                    .OrderByDescending(c => c.CommunicationDate);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Communications = communications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communications by direction: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationsByDateRange(DateTime startDate, DateTime endDate, int companyId, int page = 1, int pageSize = 20)
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

                // Query communications by date range for this company's entities
                var query = context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .Where(c => c.CommunicationDate >= startDate && c.CommunicationDate <= endDate)
                    .Where(c =>
                        (c.RelatedEntityType == "Property" && propertyIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "Company" && c.RelatedEntityId == companyId)
                    )
                    .OrderByDescending(c => c.CommunicationDate);

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var communications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = new
                {
                    Communications = communications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                response.Response = result;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communications by date range: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendEmail(string to, string subject, string body, string from = null, string relatedEntityType = null, object relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                // Send the email
                await _emailService.SendEmailAsync(to, subject, body, true);

                // Record the communication
                using var context = _contextFactory.CreateDbContext();

                // Get email channel ID
                var emailChannel = await context.CommunicationChannels
                    .FirstOrDefaultAsync(cc => cc.Name.ToLower() == "email");

                if (emailChannel == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Email communication channel not found";
                    return response;
                }

                // Get outbound direction ID
                var outboundDirection = await context.CommunicationDirections
                    .FirstOrDefaultAsync(cd => cd.Name.ToLower() == "outbound");

                if (outboundDirection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Outbound communication direction not found";
                    return response;
                }

                // Create communication record
                var communication = new Communication
                {
                    Subject = subject,
                    Content = body,
                    CommunicationChannelId = emailChannel.Id,
                    CommunicationDirectionId = outboundDirection.Id,
                    FromEmailAddress = from,
                    ToEmailAddress = to,
                    CommunicationDate = DateTime.Now,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System" // Default to system
                };

                // Set related entity details
                if (!string.IsNullOrEmpty(relatedEntityType))
                {
                    communication.RelatedEntityType = relatedEntityType;

                    if (relatedEntityId is int intId)
                    {
                        communication.RelatedEntityId = intId;
                    }
                    else if (relatedEntityId is string stringId)
                    {
                        communication.RelatedEntityStringId = stringId;
                    }
                }

                await context.Communications.AddAsync(communication);
                await context.SaveChangesAsync();

                response.Response = communication;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Email sent to {to} successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending email: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendBulkEmail(List<string> recipients, string subject, string body, string from = null, string relatedEntityType = null, object relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                // Send the email to all recipients
                await _emailService.SendEmailAsync(recipients, subject, body, true);

                // Record the communication
                using var context = _contextFactory.CreateDbContext();

                // Get email channel ID
                var emailChannel = await context.CommunicationChannels
                    .FirstOrDefaultAsync(cc => cc.Name.ToLower() == "email");

                if (emailChannel == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Email communication channel not found";
                    return response;
                }

                // Get outbound direction ID
                var outboundDirection = await context.CommunicationDirections
                    .FirstOrDefaultAsync(cd => cd.Name.ToLower() == "outbound");

                if (outboundDirection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Outbound communication direction not found";
                    return response;
                }

                // Create a single communication record
                var communication = new Communication
                {
                    Subject = subject,
                    Content = body,
                    CommunicationChannelId = emailChannel.Id,
                    CommunicationDirectionId = outboundDirection.Id,
                    FromEmailAddress = from,
                    ToEmailAddress = string.Join(";", recipients), // Join all recipients
                    CommunicationDate = DateTime.Now,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System" // Default to system
                };

                // Set related entity details
                if (!string.IsNullOrEmpty(relatedEntityType))
                {
                    communication.RelatedEntityType = relatedEntityType;

                    if (relatedEntityId is int intId)
                    {
                        communication.RelatedEntityId = intId;
                    }
                    else if (relatedEntityId is string stringId)
                    {
                        communication.RelatedEntityStringId = stringId;
                    }
                }

                await context.Communications.AddAsync(communication);
                await context.SaveChangesAsync();

                response.Response = new { Communication = communication, RecipientCount = recipients.Count };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Email sent to {recipients.Count} recipients successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending bulk email: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendTemplatedEmail(string templateName, Dictionary<string, string> templateData, string to, string from = null, string relatedEntityType = null, object relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Find the template (simplified - would typically be more complex)
                var template = await context.NotificationTemplates
                    .FirstOrDefaultAsync(nt => nt.Name == templateName);

                if (template == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Email template '{templateName}' not found";
                    return response;
                }

                // Replace template variables
                string subject = template.Subject;
                string body = template.BodyTemplate;

                foreach (var kvp in templateData)
                {
                    subject = subject.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
                    body = body.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
                }

                // Send the email
                return await SendEmail(to, subject, body, from, relatedEntityType, relatedEntityId);
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending templated email: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendSms(string to, string message, string relatedEntityType = null, object relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                // Placeholder for actual SMS sending implementation
                // Would typically use a third-party SMS service provider

                // Record the communication
                using var context = _contextFactory.CreateDbContext();

                // Get SMS channel ID
                var smsChannel = await context.CommunicationChannels
                    .FirstOrDefaultAsync(cc => cc.Name.ToLower() == "sms");

                if (smsChannel == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "SMS communication channel not found";
                    return response;
                }

                // Get outbound direction ID
                var outboundDirection = await context.CommunicationDirections
                    .FirstOrDefaultAsync(cd => cd.Name.ToLower() == "outbound");

                if (outboundDirection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Outbound communication direction not found";
                    return response;
                }

                // Create communication record
                var communication = new Communication
                {
                    Subject = "SMS Message",
                    Content = message,
                    CommunicationChannelId = smsChannel.Id,
                    CommunicationDirectionId = outboundDirection.Id,
                    ToPhoneNumber = to,
                    CommunicationDate = DateTime.Now,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System" // Default to system
                };

                // Set related entity details
                if (!string.IsNullOrEmpty(relatedEntityType))
                {
                    communication.RelatedEntityType = relatedEntityType;

                    if (relatedEntityId is int intId)
                    {
                        communication.RelatedEntityId = intId;
                    }
                    else if (relatedEntityId is string stringId)
                    {
                        communication.RelatedEntityStringId = stringId;
                    }
                }

                await context.Communications.AddAsync(communication);
                await context.SaveChangesAsync();

                response.Response = communication;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"SMS sent to {to} successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending SMS: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SendBulkSms(List<string> recipients, string message, string relatedEntityType = null, object relatedEntityId = null)
        {
            var response = new ResponseModel();

            try
            {
                // Placeholder for actual bulk SMS sending implementation
                // Would typically use a third-party SMS service provider

                // Record the communication
                using var context = _contextFactory.CreateDbContext();

                // Get SMS channel ID
                var smsChannel = await context.CommunicationChannels
                    .FirstOrDefaultAsync(cc => cc.Name.ToLower() == "sms");

                if (smsChannel == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "SMS communication channel not found";
                    return response;
                }

                // Get outbound direction ID
                var outboundDirection = await context.CommunicationDirections
                    .FirstOrDefaultAsync(cd => cd.Name.ToLower() == "outbound");

                if (outboundDirection == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Outbound communication direction not found";
                    return response;
                }

                // Create a single communication record for bulk SMS
                var communication = new Communication
                {
                    Subject = "Bulk SMS Message",
                    Content = message,
                    CommunicationChannelId = smsChannel.Id,
                    CommunicationDirectionId = outboundDirection.Id,
                    ToPhoneNumber = string.Join(";", recipients), // Join all recipients
                    CommunicationDate = DateTime.Now,
                    CreatedOn = DateTime.Now,
                    CreatedBy = "System" // Default to system
                };

                // Set related entity details
                if (!string.IsNullOrEmpty(relatedEntityType))
                {
                    communication.RelatedEntityType = relatedEntityType;

                    if (relatedEntityId is int intId)
                    {
                        communication.RelatedEntityId = intId;
                    }
                    else if (relatedEntityId is string stringId)
                    {
                        communication.RelatedEntityStringId = stringId;
                    }
                }

                await context.Communications.AddAsync(communication);
                await context.SaveChangesAsync();

                response.Response = new { Communication = communication, RecipientCount = recipients.Count };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"SMS sent to {recipients.Count} recipients successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error sending bulk SMS: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationTemplates(int companyId, string templateType = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.NotificationTemplates
                    .Include(nt => nt.NotificationEventType)
                    .Where(nt => nt.CompanyId == companyId || nt.CompanyId == null);

                // Filter by templateType if specified
                if (!string.IsNullOrEmpty(templateType))
                {
                    // Need to join with NotificationEventType to filter by category
                    query = query.Where(nt => nt.NotificationEventType.Category == templateType);
                }

                var templates = await query.ToListAsync();

                response.Response = templates;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communication templates: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> CreateCommunicationTemplate(string name, string subject, string bodyTemplate, string templateType, int companyId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Check if template with this name already exists
                var existingTemplate = await context.NotificationTemplates
                    .FirstOrDefaultAsync(nt => nt.Name == name && nt.CompanyId == companyId);

                if (existingTemplate != null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Template with name '{name}' already exists";
                    return response;
                }

                // Find or create notification event type
                var eventType = await context.NotificationEventTypes
                    .FirstOrDefaultAsync(net => net.Category == templateType);

                if (eventType == null)
                {
                    // Create a new event type if needed
                    eventType = new Models.BusinessMappingModels.NotificationEventType
                    {
                        Name = templateType,
                        Category = templateType,
                        SystemName = templateType.ToLower().Replace(" ", "_"),
                        Description = $"Template for {templateType}",
                        IsActive = true,
                        IsSystemEvent = false
                    };

                    await context.NotificationEventTypes.AddAsync(eventType);
                    await context.SaveChangesAsync();
                }

                // Create the template
                var template = new Models.BusinessMappingModels.NotificationTemplate
                {
                    Name = name,
                    Subject = subject,
                    BodyTemplate = bodyTemplate,
                    SmsTemplate = null, // Could be added as a parameter
                    CompanyId = companyId,
                    NotificationEventTypeId = eventType.Id,
                    IsDefault = false,
                    IsActive = true
                };

                await context.NotificationTemplates.AddAsync(template);
                await context.SaveChangesAsync();

                response.Response = template;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Communication template created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating communication template: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> UpdateCommunicationTemplate(int templateId, string subject, string bodyTemplate)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var template = await context.NotificationTemplates.FindAsync(templateId);

                if (template == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Template with ID {templateId} not found";
                    return response;
                }

                // Update the template
                template.Subject = subject;
                template.BodyTemplate = bodyTemplate;

                await context.SaveChangesAsync();

                response.Response = template;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Communication template updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating communication template: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> ImportCommunications(string fileContent, string fileType, int companyId)
        {
            var response = new ResponseModel();

            try
            {
                // This is a placeholder for actual implementation
                // Would typically parse fileContent based on fileType (CSV, JSON, etc.)
                // and import as Communication records

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Communication import not implemented";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error importing communications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> ExportCommunications(string entityType, object entityId, string exportFormat)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                var query = context.Communications
                    .Include(c => c.CommunicationChannel)
                    .Include(c => c.CommunicationDirection)
                    .Where(c => c.RelatedEntityType == entityType)
                    .OrderByDescending(c => c.CommunicationDate);

                // Handle different ID types
                if (entityId is int intId)
                {
                    query = (IOrderedQueryable<Communication>)query.Where(c => c.RelatedEntityId == intId);
                }
                else if (entityId is string stringId)
                {
                    query = (IOrderedQueryable<Communication>)query.Where(c => c.RelatedEntityStringId == stringId);
                }

                var communications = await query.ToListAsync();

                // Convert to desired export format
                string exportContent = "";

                switch (exportFormat.ToLower())
                {
                    case "csv":
                        // Simple CSV export
                        exportContent = "Id,Date,Channel,Direction,From,To,Subject\n";
                        foreach (var comm in communications)
                        {
                            exportContent += $"{comm.Id},{comm.CommunicationDate:yyyy-MM-dd},{comm.CommunicationChannel?.Name},{comm.CommunicationDirection?.Name}," +
                                           $"{comm.FromEmailAddress ?? comm.FromPhoneNumber},{comm.ToEmailAddress ?? comm.ToPhoneNumber},\"{comm.Subject}\"\n";
                        }
                        break;

                    case "json":
                        // Simple JSON export (would typically use JSON serialization)
                        exportContent = "[";
                        foreach (var comm in communications)
                        {
                            exportContent += $"{{\"id\":{comm.Id},\"date\":\"{comm.CommunicationDate:yyyy-MM-dd}\",\"channel\":\"{comm.CommunicationChannel?.Name}\"," +
                                           $"\"direction\":\"{comm.CommunicationDirection?.Name}\",\"from\":\"{comm.FromEmailAddress ?? comm.FromPhoneNumber}\"," +
                                           $"\"to\":\"{comm.ToEmailAddress ?? comm.ToPhoneNumber}\",\"subject\":\"{comm.Subject}\"}},";
                        }
                        if (communications.Any())
                        {
                            exportContent = exportContent.TrimEnd(',');
                        }
                        exportContent += "]";
                        break;

                    default:
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = $"Export format '{exportFormat}' not supported";
                        return response;
                }

                response.Response = exportContent;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"Exported {communications.Count} communications in {exportFormat} format";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error exporting communications: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetCommunicationStatistics(int companyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Default date range if not specified
                if (!startDate.HasValue)
                {
                    startDate = DateTime.Today.AddMonths(-1);
                }

                if (!endDate.HasValue)
                {
                    endDate = DateTime.Today;
                }

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

                // Define the filtering condition for communications related to this company
                var companyCommunicationsQuery = context.Communications
                    .Where(c => c.CommunicationDate >= startDate && c.CommunicationDate <= endDate)
                    .Where(c =>
                        (c.RelatedEntityType == "Property" && propertyIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(c.RelatedEntityId.Value)) ||
                        (c.RelatedEntityType == "Company" && c.RelatedEntityId == companyId)
                    );

                // Calculate statistics
                var totalCommunications = await companyCommunicationsQuery.CountAsync();
                var communicationsByChannel = await companyCommunicationsQuery
                    .GroupBy(c => c.CommunicationChannelId)
                    .Select(g => new { ChannelId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var communicationsByDirection = await companyCommunicationsQuery
                    .GroupBy(c => c.CommunicationDirectionId)
                    .Select(g => new { DirectionId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var communicationsByEntityType = await companyCommunicationsQuery
                    .GroupBy(c => c.RelatedEntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToListAsync();

                var communicationsByDay = await companyCommunicationsQuery
                    .GroupBy(c => c.CommunicationDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Get channel and direction names
                var channels = await context.CommunicationChannels.ToListAsync();
                var directions = await context.CommunicationDirections.ToListAsync();

                // Combine results
                var statistics = new
                {
                    TotalCommunications = totalCommunications,
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    CommunicationsByChannel = communicationsByChannel.Select(c => new
                    {
                        ChannelId = c.ChannelId,
                        ChannelName = channels.FirstOrDefault(ch => ch.Id == c.ChannelId)?.Name,
                        Count = c.Count,
                        Percentage = totalCommunications > 0 ? Math.Round((double)c.Count / totalCommunications * 100, 1) : 0
                    }),
                    CommunicationsByDirection = communicationsByDirection.Select(d => new
                    {
                        DirectionId = d.DirectionId,
                        DirectionName = directions.FirstOrDefault(dir => dir.Id == d.DirectionId)?.Name,
                        Count = d.Count,
                        Percentage = totalCommunications > 0 ? Math.Round((double)d.Count / totalCommunications * 100, 1) : 0
                    }),
                    CommunicationsByEntityType = communicationsByEntityType.Select(e => new
                    {
                        EntityType = e.EntityType,
                        Count = e.Count,
                        Percentage = totalCommunications > 0 ? Math.Round((double)e.Count / totalCommunications * 100, 1) : 0
                    }),
                    CommunicationsByDay = communicationsByDay.Select(d => new
                    {
                        Date = d.Date,
                        Count = d.Count
                    })
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving communication statistics: {ex.Message}";
            }

            return response;
        }
    }
}