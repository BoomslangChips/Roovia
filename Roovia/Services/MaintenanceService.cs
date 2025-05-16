using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using Roovia.Models.BusinessModels;
using Roovia.Models.UserCompanyModels;

namespace Roovia.Services
{
    /// <summary>
    /// Service for handling maintenance-related operations
    /// </summary>
    public class MaintenanceService : IMaintenance
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<MaintenanceService> _logger;
        private readonly ICdnService _cdnService;
        private readonly IEmailService _emailService;

        public MaintenanceService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<MaintenanceService> logger,
            ICdnService cdnService,
            IEmailService emailService)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cdnService = cdnService ?? throw new ArgumentNullException(nameof(cdnService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        /// <summary>
        /// Creates a new maintenance ticket for a property
        /// </summary>
        public async Task<ResponseModel> CreateMaintenanceTicket(MaintenanceTicket ticket, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Validate ticket data
                    if (string.IsNullOrEmpty(ticket.Title))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Ticket title is required.";
                        return response;
                    }

                    // Verify the property exists and belongs to the company
                    var property = await context.Properties
                        .FirstOrDefaultAsync(p => p.Id == ticket.PropertyId && p.CompanyId == companyId && !p.IsRemoved);

                    if (property == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Property not found or does not belong to the company.";
                        return response;
                    }

                    // Generate unique ticket number
                    ticket.TicketNumber = await GenerateUniqueTicketNumber(context);
                    ticket.CompanyId = companyId;
                    ticket.CreatedOn = DateTime.Now;

                    // Set default status if not provided
                    if (ticket.StatusId == 0)
                        ticket.StatusId = 1; // Open status

                    // Add the ticket
                    await context.MaintenanceTickets.AddAsync(ticket);
                    await context.SaveChangesAsync();

                    // Create CDN folder structure for ticket
                    try
                    {
                        var cdnFolderPath = $"company-{companyId}/maintenance/{ticket.Id}";
                        await _cdnService.CreateFolderAsync("maintenance", cdnFolderPath, "images");
                        await _cdnService.CreateFolderAsync("maintenance", cdnFolderPath, "documents");
                        await _cdnService.CreateFolderAsync("maintenance", cdnFolderPath, "invoices");
                    }
                    catch (Exception cdnEx)
                    {
                        // Log CDN error but continue with transaction
                        _logger.LogWarning(cdnEx, "Error creating CDN folders for maintenance ticket {TicketId}. Continuing with creation.", ticket.Id);
                    }

                    // Reload with related data
                    var createdTicket = await GetTicketWithDetails(context, ticket.Id);

                    // Send notification if tenant is affected
                    if (ticket.RequiresTenantAccess && ticket.TenantId.HasValue)
                    {
                        await SendTenantNotification(createdTicket);
                    }

                    await transaction.CommitAsync();

                    response.Response = createdTicket;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Maintenance ticket created successfully.";

                    _logger.LogInformation("Maintenance ticket created with ID: {TicketId} for property {PropertyId}",
                        ticket.Id, ticket.PropertyId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating maintenance ticket");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while creating the maintenance ticket: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Uploads a receipt for a maintenance expense
        /// </summary>
        public async Task<ResponseModel> UploadMaintenanceExpenseReceipt(int expenseId, IFormFile file, string userId)
        {
            ResponseModel response = new();

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No file was uploaded.";
                    return response;
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File type not allowed. Please upload a JPG, PNG, or PDF file.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var expense = await context.MaintenanceExpenses
                        .Include(e => e.MaintenanceTicket)
                        .FirstOrDefaultAsync(e => e.Id == expenseId);

                    if (expense == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Expense not found.";
                        return response;
                    }

                    // Delete old receipt if exists
                    if (expense.ReceiptDocumentId.HasValue)
                    {
                        var oldReceipt = await context.CdnFileMetadata
                            .FirstOrDefaultAsync(f => f.Id == expense.ReceiptDocumentId.Value);

                        if (oldReceipt != null)
                        {
                            await _cdnService.DeleteFileAsync(oldReceipt.Url);
                        }
                    }

                    // Upload new receipt with base64 backup
                    var cdnPath = $"company-{expense.MaintenanceTicket.CompanyId}/maintenance/{expense.MaintenanceTicketId}/invoices";
                    string cdnUrl;

                    using (var stream = file.OpenReadStream())
                    {
                        cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                            stream,
                            file.FileName,
                            file.ContentType,
                            "maintenance",
                            cdnPath
                        );
                    }

                    // Get the file metadata
                    var fileMetadata = await _cdnService.GetFileMetadataAsync(cdnUrl);
                    if (fileMetadata != null)
                    {
                        expense.ReceiptDocumentId = fileMetadata.Id;
                        expense.IsApproved = true; // Auto-approve if receipt is uploaded

                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        response.Response = new { ReceiptUrl = cdnUrl, FileId = fileMetadata.Id };
                        response.ResponseInfo.Success = true;
                        response.ResponseInfo.Message = "Receipt uploaded successfully.";
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Failed to save receipt metadata.";
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading maintenance expense receipt");
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the receipt: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets a maintenance ticket by ID
        /// </summary>
        public async Task<ResponseModel> GetMaintenanceTicketById(int id, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var ticket = await context.MaintenanceTickets
                    .Include(t => t.Property)
                        .ThenInclude(p => p.Owner)
                    .Include(t => t.Tenant)
                    .Include(t => t.Inspection)
                    .Include(t => t.Vendor)
                    .Include(t => t.Category)
                    .Include(t => t.Priority)
                    .Include(t => t.Status)
                    .Include(t => t.Comments)
                    .Include(t => t.Expenses)
                        .ThenInclude(e => e.Category)
                    .Include(t => t.Expenses)
                        .ThenInclude(e => e.Vendor)
                    .Include(t => t.Expenses)
                        .ThenInclude(e => e.ReceiptDocument)
                    .Where(t => t.Id == id && t.CompanyId == companyId)
                    .AsNoTracking() // Improves read-only query performance
                    .FirstOrDefaultAsync();

                if (ticket != null)
                {
                    response.Response = ticket;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Maintenance ticket retrieved successfully.";
                }
                else
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Maintenance ticket not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance ticket {TicketId} for company {CompanyId}", id, companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving the maintenance ticket: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Updates an existing maintenance ticket
        /// </summary>
        public async Task<ResponseModel> UpdateMaintenanceTicket(int id, MaintenanceTicket updatedTicket, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Validate ticket data
                    if (string.IsNullOrEmpty(updatedTicket.Title))
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Ticket title is required.";
                        return response;
                    }

                    var ticket = await context.MaintenanceTickets
                        .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);

                    if (ticket == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Maintenance ticket not found.";
                        return response;
                    }

                    // Track status changes for notifications
                    var oldStatusId = ticket.StatusId;

                    // Update ticket fields
                    ticket.Title = updatedTicket.Title;
                    ticket.Description = updatedTicket.Description;
                    ticket.CategoryId = updatedTicket.CategoryId;
                    ticket.PriorityId = updatedTicket.PriorityId;
                    ticket.StatusId = updatedTicket.StatusId;
                    ticket.AssignedToUserId = updatedTicket.AssignedToUserId;
                    ticket.AssignedToName = updatedTicket.AssignedToName;
                    ticket.VendorId = updatedTicket.VendorId;
                    ticket.ScheduledDate = updatedTicket.ScheduledDate;
                    ticket.CompletedDate = updatedTicket.CompletedDate;
                    ticket.EstimatedDuration = updatedTicket.EstimatedDuration;
                    ticket.ActualDuration = updatedTicket.ActualDuration;
                    ticket.EstimatedCost = updatedTicket.EstimatedCost;
                    ticket.ActualCost = updatedTicket.ActualCost;
                    ticket.TenantResponsible = updatedTicket.TenantResponsible;
                    ticket.RequiresApproval = updatedTicket.RequiresApproval;
                    ticket.IsApproved = updatedTicket.IsApproved;
                    ticket.ApprovedBy = updatedTicket.ApprovedBy;
                    ticket.ApprovalDate = updatedTicket.ApprovalDate;
                    ticket.RequiresTenantAccess = updatedTicket.RequiresTenantAccess;
                    ticket.TenantNotified = updatedTicket.TenantNotified;
                    ticket.TenantNotificationDate = updatedTicket.TenantNotificationDate;
                    ticket.AccessInstructions = updatedTicket.AccessInstructions;
                    ticket.CompletionNotes = updatedTicket.CompletionNotes;
                    ticket.IssueResolved = updatedTicket.IssueResolved;
                    ticket.UpdatedDate = DateTime.Now;
                    ticket.UpdatedBy = updatedTicket.UpdatedBy;

                    await context.SaveChangesAsync();

                    // Send notifications for status changes
                    if (oldStatusId != updatedTicket.StatusId)
                    {
                        await HandleStatusChangeNotifications(ticket, oldStatusId, updatedTicket.StatusId);
                    }

                    // Reload with related data
                    var updatedResult = await GetTicketWithDetails(context, id);

                    await transaction.CommitAsync();

                    response.Response = updatedResult;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Maintenance ticket updated successfully.";

                    _logger.LogInformation("Maintenance ticket updated: {TicketId}", id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating maintenance ticket {TicketId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while updating the maintenance ticket: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Deletes a maintenance ticket and its associated data
        /// </summary>
        public async Task<ResponseModel> DeleteMaintenanceTicket(int id, int companyId, ApplicationUser user)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var ticket = await context.MaintenanceTickets
                        .Include(t => t.Comments)
                        .Include(t => t.Expenses)
                            .ThenInclude(e => e.ReceiptDocument)
                        .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == companyId);

                    if (ticket == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Maintenance ticket not found.";
                        return response;
                    }

                    // Delete associated documents from CDN
                    foreach (var expense in ticket.Expenses.Where(e => e.ReceiptDocument != null))
                    {
                        try
                        {
                            await _cdnService.DeleteFileAsync(expense.ReceiptDocument.Url);
                        }
                        catch (Exception cdnEx)
                        {
                            // Log but continue with deletion
                            _logger.LogWarning(cdnEx, "Failed to delete CDN file for expense {ExpenseId}", expense.Id);
                        }
                    }

                    // Delete related data
                    context.MaintenanceComments.RemoveRange(ticket.Comments);
                    context.MaintenanceExpenses.RemoveRange(ticket.Expenses);
                    context.MaintenanceTickets.Remove(ticket);

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Maintenance ticket deleted successfully.";

                    _logger.LogInformation("Maintenance ticket deleted: {TicketId} by {UserId}", id, user?.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting maintenance ticket {TicketId}", id);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while deleting the maintenance ticket: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets all maintenance tickets for a property
        /// </summary>
        public async Task<ResponseModel> GetMaintenanceTicketsByProperty(int propertyId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tickets = await context.MaintenanceTickets
                    .Include(t => t.Category)
                    .Include(t => t.Priority)
                    .Include(t => t.Status)
                    .Include(t => t.Vendor)
                    .Where(t => t.PropertyId == propertyId && t.CompanyId == companyId)
                    .OrderByDescending(t => t.CreatedOn)
                    .AsNoTracking() // Improves read-only query performance
                    .ToListAsync();

                response.Response = tickets;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Maintenance tickets retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} maintenance tickets for property {PropertyId}",
                    tickets.Count, propertyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance tickets for property {PropertyId}", propertyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving maintenance tickets: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Adds a comment to a maintenance ticket
        /// </summary>
        public async Task<ResponseModel> AddComment(int ticketId, MaintenanceComment comment, string userId)
        {
            ResponseModel response = new();

            try
            {
                if (string.IsNullOrEmpty(comment.Comment))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Comment text is required.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var ticket = await context.MaintenanceTickets
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Maintenance ticket not found.";
                    return response;
                }

                comment.MaintenanceTicketId = ticketId;
                comment.CreatedOn = DateTime.Now;
                comment.CreatedBy = userId;

                await context.MaintenanceComments.AddAsync(comment);
                await context.SaveChangesAsync();

                response.Response = comment;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Comment added successfully.";

                _logger.LogInformation("Comment added to maintenance ticket {TicketId} by {UserId}", ticketId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to maintenance ticket {TicketId}", ticketId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the comment: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Adds an expense to a maintenance ticket
        /// </summary>
        public async Task<ResponseModel> AddExpense(int ticketId, MaintenanceExpense expense)
        {
            ResponseModel response = new();

            try
            {
                // Validate expense data
                if (string.IsNullOrEmpty(expense.Description))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Expense description is required.";
                    return response;
                }

                if (expense.Amount <= 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Expense amount must be greater than zero.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var ticket = await context.MaintenanceTickets
                        .FirstOrDefaultAsync(t => t.Id == ticketId);

                    if (ticket == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Maintenance ticket not found.";
                        return response;
                    }

                    expense.MaintenanceTicketId = ticketId;
                    expense.CreatedOn = DateTime.Now;

                    await context.MaintenanceExpenses.AddAsync(expense);

                    // Update ticket actual cost
                    var totalExpenses = await context.MaintenanceExpenses
                        .Where(e => e.MaintenanceTicketId == ticketId)
                        .SumAsync(e => e.Amount);

                    ticket.ActualCost = totalExpenses + expense.Amount;
                    ticket.UpdatedDate = DateTime.Now;

                    await context.SaveChangesAsync();

                    // Load complete expense with relations
                    var createdExpense = await context.MaintenanceExpenses
                        .Include(e => e.Category)
                        .Include(e => e.Vendor)
                        .Include(e => e.ReceiptDocument)
                        .FirstOrDefaultAsync(e => e.Id == expense.Id);

                    await transaction.CommitAsync();

                    response.Response = createdExpense;
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Expense added successfully.";

                    _logger.LogInformation("Expense added to maintenance ticket {TicketId}: Amount {Amount}",
                        ticketId, expense.Amount);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding expense to maintenance ticket {TicketId}", ticketId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while adding the expense: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Assigns a maintenance ticket to a vendor
        /// </summary>
        public async Task<ResponseModel> AssignTicketToVendor(int ticketId, int vendorId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var ticket = await context.MaintenanceTickets
                        .Include(t => t.Vendor)
                        .FirstOrDefaultAsync(t => t.Id == ticketId);

                    if (ticket == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Maintenance ticket not found.";
                        return response;
                    }

                    var vendor = await context.Vendors
                        .FirstOrDefaultAsync(v => v.Id == vendorId && v.IsActive);

                    if (vendor == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Vendor not found or inactive.";
                        return response;
                    }

                    ticket.VendorId = vendorId;
                    ticket.StatusId = 2; // In Progress
                    ticket.UpdatedDate = DateTime.Now;
                    ticket.UpdatedBy = userId;

                    await context.SaveChangesAsync();

                    // Send notification to vendor
                    await SendVendorAssignmentNotification(ticket, vendor);

                    await transaction.CommitAsync();

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Ticket assigned to vendor successfully.";

                    _logger.LogInformation("Maintenance ticket {TicketId} assigned to vendor {VendorId} by {UserId}",
                        ticketId, vendorId, userId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning maintenance ticket {TicketId} to vendor {VendorId}",
                    ticketId, vendorId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while assigning the ticket: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Marks a maintenance ticket as completed
        /// </summary>
        public async Task<ResponseModel> CompleteMaintenanceTicket(int ticketId, string completionNotes, bool issueResolved, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var ticket = await context.MaintenanceTickets
                        .Include(t => t.Property)
                        .Include(t => t.Tenant)
                        .FirstOrDefaultAsync(t => t.Id == ticketId);

                    if (ticket == null)
                    {
                        response.ResponseInfo.Success = false;
                        response.ResponseInfo.Message = "Maintenance ticket not found.";
                        return response;
                    }

                    ticket.StatusId = 4; // Completed
                    ticket.CompletedDate = DateTime.Now;
                    ticket.CompletionNotes = completionNotes;
                    ticket.IssueResolved = issueResolved;
                    ticket.UpdatedDate = DateTime.Now;
                    ticket.UpdatedBy = userId;

                    await context.SaveChangesAsync();

                    // Send completion notifications
                    await SendCompletionNotifications(ticket);

                    await transaction.CommitAsync();

                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "Maintenance ticket completed successfully.";

                    _logger.LogInformation("Maintenance ticket {TicketId} completed by {UserId}. Issue resolved: {IssueResolved}",
                        ticketId, userId, issueResolved);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing maintenance ticket {TicketId}", ticketId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while completing the ticket: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets all open maintenance tickets for a company
        /// </summary>
        public async Task<ResponseModel> GetOpenTicketsByCompany(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var tickets = await context.MaintenanceTickets
                    .Include(t => t.Property)
                    .Include(t => t.Category)
                    .Include(t => t.Priority)
                    .Include(t => t.Status)
                    .Include(t => t.Vendor)
                    .Where(t => t.CompanyId == companyId &&
                               (t.StatusId == 1 || t.StatusId == 2)) // Open or In Progress
                    .OrderBy(t => t.PriorityId)
                    .ThenBy(t => t.CreatedOn)
                    .AsNoTracking() // Improves performance
                    .ToListAsync();

                response.Response = tickets;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Open maintenance tickets retrieved successfully.";

                _logger.LogInformation("Retrieved {Count} open maintenance tickets for company {CompanyId}",
                    tickets.Count, companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving open maintenance tickets for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving tickets: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets maintenance statistics for a company
        /// </summary>
        public async Task<ResponseModel> GetMaintenanceStatistics(int companyId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Use optimized query approach to reduce memory usage
                var tickets = await context.MaintenanceTickets
                    .Where(t => t.CompanyId == companyId)
                    .AsNoTracking()
                    .ToListAsync();

                var statistics = new
                {
                    TotalTickets = tickets.Count,
                    OpenTickets = tickets.Count(t => t.StatusId == 1),
                    InProgressTickets = tickets.Count(t => t.StatusId == 2),
                    CompletedTickets = tickets.Count(t => t.StatusId == 4),
                    ResolvedIssues = tickets.Count(t => t.IssueResolved == true),
                    TicketsByCategory = tickets.GroupBy(t => t.CategoryId)
                        .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                        .ToList(),
                    TicketsByPriority = tickets.GroupBy(t => t.PriorityId)
                        .Select(g => new { PriorityId = g.Key, Count = g.Count() })
                        .ToList(),
                    TotalCost = tickets.Where(t => t.ActualCost.HasValue).Sum(t => t.ActualCost.Value),
                    AverageResolutionTime = CalculateAverageResolutionTime(tickets),
                    VendorPerformance = await GetVendorPerformanceStats(context, companyId)
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Maintenance statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving maintenance statistics for company {CompanyId}", companyId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving statistics: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Gets maintenance documents for a maintenance ticket
        /// </summary>
        public async Task<ResponseModel> GetMaintenanceDocuments(int ticketId, int companyId)
        {
            ResponseModel response = new();

            try
            {
                var cdnPath = $"company-{companyId}/maintenance/{ticketId}/documents";
                var files = await _cdnService.GetFilesAsync("maintenance", cdnPath);

                response.Response = files;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Maintenance documents retrieved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents for maintenance ticket {TicketId}", ticketId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while retrieving documents: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Uploads an image for a maintenance ticket
        /// </summary>
        public async Task<ResponseModel> UploadMaintenanceImage(int ticketId, IFormFile file, string category, string userId)
        {
            ResponseModel response = new();

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "No file was uploaded.";
                    return response;
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "File type not allowed. Please upload a JPG, JPEG, PNG or GIF file.";
                    return response;
                }

                using var context = await _contextFactory.CreateDbContextAsync();

                var ticket = await context.MaintenanceTickets
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Maintenance ticket not found.";
                    return response;
                }

                // Upload image with base64 backup
                var cdnPath = $"company-{ticket.CompanyId}/maintenance/{ticket.Id}/{category}";
                string cdnUrl;

                using (var stream = file.OpenReadStream())
                {
                    cdnUrl = await _cdnService.UploadFileWithBase64BackupAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        "maintenance",
                        cdnPath
                    );
                }

                response.Response = new { ImageUrl = cdnUrl };
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Image uploaded successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for maintenance ticket {TicketId}", ticketId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while uploading the image: " + ex.Message;
            }

            return response;
        }

        /// <summary>
        /// Approves a maintenance ticket
        /// </summary>
        public async Task<ResponseModel> ApproveMaintenanceTicket(int ticketId, string userId)
        {
            ResponseModel response = new();

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var ticket = await context.MaintenanceTickets
                    .FirstOrDefaultAsync(t => t.Id == ticketId);

                if (ticket == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "Maintenance ticket not found.";
                    return response;
                }

                if (!ticket.RequiresApproval)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = "This ticket does not require approval.";
                    return response;
                }

                ticket.IsApproved = true;
                ticket.ApprovedBy = userId;
                ticket.ApprovalDate = DateTime.Now;
                ticket.UpdatedDate = DateTime.Now;
                ticket.UpdatedBy = userId;

                await context.SaveChangesAsync();

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Maintenance ticket approved successfully.";

                _logger.LogInformation("Maintenance ticket {TicketId} approved by {UserId}", ticketId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving maintenance ticket {TicketId}", ticketId);
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = "An error occurred while approving the ticket: " + ex.Message;
            }

            return response;
        }

        // Helper methods
        private async Task<MaintenanceTicket> GetTicketWithDetails(ApplicationDbContext context, int ticketId)
        {
            return await context.MaintenanceTickets
                .Include(t => t.Property)
                    .ThenInclude(p => p.Owner)
                .Include(t => t.Tenant)
                .Include(t => t.Inspection)
                .Include(t => t.Vendor)
                .Include(t => t.Category)
                .Include(t => t.Priority)
                .Include(t => t.Status)
                .Include(t => t.Comments)
                .Include(t => t.Expenses)
                    .ThenInclude(e => e.Category)
                .Include(t => t.Expenses)
                    .ThenInclude(e => e.Vendor)
                .Include(t => t.Expenses)
                    .ThenInclude(e => e.ReceiptDocument)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
        }

        private async Task<string> GenerateUniqueTicketNumber(ApplicationDbContext context)
        {
            string number;
            do
            {
                number = $"MT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
            }
            while (await context.MaintenanceTickets.AnyAsync(t => t.TicketNumber == number));

            return number;
        }

        private async Task SendTenantNotification(MaintenanceTicket ticket)
        {
            try
            {
                if (ticket.Tenant?.PrimaryEmail != null)
                {
                    await _emailService.SendEmailAsync(
                        ticket.Tenant.PrimaryEmail,
                        $"Maintenance Scheduled - {ticket.Property?.PropertyName}",
                        $"A maintenance ticket has been created for your property.\n\n" +
                        $"Ticket Number: {ticket.TicketNumber}\n" +
                        $"Description: {ticket.Title}\n" +
                        $"Priority: {ticket.Priority?.Name}\n" +
                        $"Scheduled Date: {ticket.ScheduledDate?.ToString("d")}\n\n" +
                        $"We will keep you updated on the progress."
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tenant notification for ticket {TicketId}", ticket.Id);
            }
        }

        private async Task SendVendorAssignmentNotification(MaintenanceTicket ticket, Vendor vendor)
        {
            try
            {
                if (vendor.PrimaryEmail != null)
                {
                    await _emailService.SendEmailAsync(
                        vendor.PrimaryEmail,
                        $"New Maintenance Assignment - {ticket.TicketNumber}",
                        $"You have been assigned a new maintenance ticket.\n\n" +
                        $"Ticket Number: {ticket.TicketNumber}\n" +
                        $"Property: {ticket.Property?.PropertyName}\n" +
                        $"Description: {ticket.Title}\n" +
                        $"Priority: {ticket.Priority?.Name}\n" +
                        $"Scheduled Date: {ticket.ScheduledDate?.ToString("d")}\n\n" +
                        $"Please contact the property management office for access details."
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vendor notification for ticket {TicketId}", ticket.Id);
            }
        }

        private async Task SendCompletionNotifications(MaintenanceTicket ticket)
        {
            try
            {
                // Notify tenant if applicable
                if (ticket.Tenant?.PrimaryEmail != null)
                {
                    await _emailService.SendEmailAsync(
                        ticket.Tenant.PrimaryEmail,
                        $"Maintenance Completed - {ticket.Property?.PropertyName}",
                        $"The maintenance work on your property has been completed.\n\n" +
                        $"Ticket Number: {ticket.TicketNumber}\n" +
                        $"Issue Resolved: {(ticket.IssueResolved == true ? "Yes" : "No")}\n" +
                        $"Completion Notes: {ticket.CompletionNotes}\n\n" +
                        $"If you have any questions, please contact the property management office."
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending completion notifications for ticket {TicketId}", ticket.Id);
            }
        }

        private async Task HandleStatusChangeNotifications(MaintenanceTicket ticket, int oldStatusId, int newStatusId)
        {
            // Implement status change notifications based on business rules
            // For example, notify tenant when status changes to "In Progress"
            if (oldStatusId == 1 && newStatusId == 2 && ticket.TenantId.HasValue)
            {
                await SendTenantNotification(ticket);
            }
        }

        private double CalculateAverageResolutionTime(List<MaintenanceTicket> tickets)
        {
            var completedTickets = tickets
                .Where(t => t.StatusId == 4 && t.CompletedDate.HasValue)
                .ToList();

            if (!completedTickets.Any())
                return 0;

            var totalDays = completedTickets
                .Sum(t => (t.CompletedDate.Value - t.CreatedOn).TotalDays);

            return Math.Round(totalDays / completedTickets.Count, 1);
        }

        private async Task<object> GetVendorPerformanceStats(ApplicationDbContext context, int companyId)
        {
            var vendorStats = await context.MaintenanceTickets
                .Where(t => t.CompanyId == companyId && t.VendorId.HasValue)
                .GroupBy(t => t.VendorId)
                .Select(g => new
                {
                    VendorId = g.Key,
                    TotalTickets = g.Count(),
                    CompletedTickets = g.Count(t => t.StatusId == 4),
                    ResolvedIssues = g.Count(t => t.IssueResolved == true)
                })
                .AsNoTracking() // Optimize performance for this aggregation query
                .ToListAsync();

            return vendorStats;
        }
    }
}