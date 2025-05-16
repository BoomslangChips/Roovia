using Microsoft.EntityFrameworkCore;
using Roovia.Data;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;

namespace Roovia.Services
{
    public class NoteService : INoteService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IAuditService _auditService;

        public NoteService(IDbContextFactory<ApplicationDbContext> contextFactory, IAuditService auditService)
        {
            _contextFactory = contextFactory;
            _auditService = auditService;
        }

        public async Task<ResponseModel> CreateNote(Note note)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set creation date and other defaults
                note.CreatedOn = DateTime.Now;

                // Add the note
                await context.Notes.AddAsync(note);
                await context.SaveChangesAsync();

                // Log the action
                await _auditService.LogEntityChange(
                    note.RelatedEntityType,
                    note.RelatedEntityId.GetValueOrDefault(),
                    note.CreatedBy,
                    "CreateNote",
                    $"Created note: {note.Title}");

                response.Response = note;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Note created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating note: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNoteById(int id)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var note = await context.Notes
                    .Include(n => n.NoteType)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (note == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Note with ID {id} not found";
                    return response;
                }

                response.Response = note;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving note: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> UpdateNote(int id, Note updatedNote)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var note = await context.Notes.FindAsync(id);

                if (note == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Note with ID {id} not found";
                    return response;
                }

                string oldContent = note.Content;

                // Update the note properties
                note.Title = updatedNote.Title;
                note.Content = updatedNote.Content;
                note.NoteTypeId = updatedNote.NoteTypeId;
                note.IsPrivate = updatedNote.IsPrivate;
                note.UpdatedDate = DateTime.Now;
                note.UpdatedBy = updatedNote.UpdatedBy;

                await context.SaveChangesAsync();

                // Log the change
                await _auditService.LogEntityChange(
                    note.RelatedEntityType,
                    note.RelatedEntityId.GetValueOrDefault(),
                    note.UpdatedBy,
                    "UpdateNote",
                    $"Updated note: {note.Title}",
                    oldContent,
                    note.Content);

                response.Response = note;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Note updated successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error updating note: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> DeleteNote(int id, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var note = await context.Notes.FindAsync(id);

                if (note == null)
                {
                    response.ResponseInfo.Success = false;
                    response.ResponseInfo.Message = $"Note with ID {id} not found";
                    return response;
                }

                // Store values for audit logging
                string entityType = note.RelatedEntityType;
                int? entityId = note.RelatedEntityId;
                string title = note.Title;

                context.Notes.Remove(note);
                await context.SaveChangesAsync();

                // Log the deletion
                if (entityId.HasValue)
                {
                    await _auditService.LogEntityChange(
                        entityType,
                        entityId.Value,
                        userId,
                        "DeleteNote",
                        $"Deleted note: {title}");
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = "Note deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting note: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNotesByEntity(string entityType, object entityId, bool includePrivate = false)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var query = context.Notes
                    .Include(n => n.NoteType)
                    .Where(n => n.RelatedEntityType == entityType)
                    .OrderByDescending(n => n.CreatedOn);

                // Handle different ID types (string vs int)
                if (entityId is int intId)
                {
                    query = (IOrderedQueryable<Note>)query.Where(n => n.RelatedEntityId == intId);
                }
                else if (entityId is string stringId)
                {
                    query = (IOrderedQueryable<Note>)query.Where(n => n.RelatedEntityStringId == stringId);
                }

                // Filter private notes if requested
                if (!includePrivate)
                {
                    query = (IOrderedQueryable<Note>)query.Where(n => !n.IsPrivate);
                }

                var notes = await query.ToListAsync();

                response.Response = notes;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving notes: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNotesByNoteType(int noteTypeId, bool includePrivate = false)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();
                var query = context.Notes
                    .Include(n => n.NoteType)
                    .Where(n => n.NoteTypeId == noteTypeId)
                    .OrderByDescending(n => n.CreatedOn);

                if (!includePrivate)
                {
                    query = (IOrderedQueryable<Note>)query.Where(n => !n.IsPrivate);
                }

                var notes = await query.ToListAsync();

                response.Response = notes;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving notes: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetRecentNotes(int companyId, int count = 10, bool includePrivate = false)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get properties associated with the company
                var propertyIds = await context.Properties
                    .Where(p => p.CompanyId == companyId)
                    .Select(p => p.Id)
                    .ToListAsync();

                // Get owners associated with the company
                var ownerIds = await context.PropertyOwners
                    .Where(o => o.CompanyId == companyId)
                    .Select(o => o.Id)
                    .ToListAsync();

                // Get tenants associated with the company
                var tenantIds = await context.PropertyTenants
                    .Where(t => t.CompanyId == companyId)
                    .Select(t => t.Id)
                    .ToListAsync();

                // Get beneficiaries associated with the company
                var beneficiaryIds = await context.PropertyBeneficiaries
                    .Where(b => b.CompanyId == companyId)
                    .Select(b => b.Id)
                    .ToListAsync();

                // Query for notes related to the company's entities
                var query = context.Notes
                    .Include(n => n.NoteType)
                    .Where(n =>
                        (n.RelatedEntityType == "Property" && propertyIds.Contains(n.RelatedEntityId.Value)) ||
                        (n.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(n.RelatedEntityId.Value)) ||
                        (n.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(n.RelatedEntityId.Value)) ||
                        (n.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(n.RelatedEntityId.Value)) ||
                        (n.RelatedEntityType == "Company" && n.RelatedEntityId == companyId)
                    )
                    .OrderByDescending(n => n.CreatedOn);

                if (!includePrivate)
                {
                    query = (IOrderedQueryable<Note>)query.Where(n => !n.IsPrivate);
                }

                var notes = await query.Take(count).ToListAsync();

                response.Response = notes;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving recent notes: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> SearchNotes(string searchTerm, int companyId, bool includePrivate = false)
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

                // Get entities associated with the company
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

                // Search for notes that match the search term
                var query = context.Notes
                    .Include(n => n.NoteType)
                    .Where(n =>
                        (n.Title.Contains(searchTerm) || n.Content.Contains(searchTerm)) &&
                        (
                            (n.RelatedEntityType == "Property" && propertyIds.Contains(n.RelatedEntityId.Value)) ||
                            (n.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(n.RelatedEntityId.Value)) ||
                            (n.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(n.RelatedEntityId.Value)) ||
                            (n.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(n.RelatedEntityId.Value)) ||
                            (n.RelatedEntityType == "Company" && n.RelatedEntityId == companyId)
                        )
                    )
                    .OrderByDescending(n => n.CreatedOn);

                if (!includePrivate)
                {
                    query = (IOrderedQueryable<Note>)query.Where(n => !n.IsPrivate);
                }

                var notes = await query.ToListAsync();

                response.Response = notes;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error searching notes: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> CreateBulkNotes(List<Note> notes)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Set creation date for all notes
                foreach (var note in notes)
                {
                    note.CreatedOn = DateTime.Now;
                }

                // Add the notes
                await context.Notes.AddRangeAsync(notes);
                await context.SaveChangesAsync();

                // Log the actions (simplified for bulk operations)
                foreach (var note in notes)
                {
                    if (note.RelatedEntityId.HasValue)
                    {
                        await _auditService.LogEntityChange(
                            note.RelatedEntityType,
                            note.RelatedEntityId.Value,
                            note.CreatedBy,
                            "CreateNote",
                            $"Bulk created note: {note.Title}");
                    }
                }

                response.Response = notes;
                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"{notes.Count} notes created successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error creating bulk notes: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> DeleteNotesByEntity(string entityType, object entityId, string userId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                IQueryable<Note> query = context.Notes.Where(n => n.RelatedEntityType == entityType);

                // Handle different ID types
                if (entityId is int intId)
                {
                    query = query.Where(n => n.RelatedEntityId == intId);
                }
                else if (entityId is string stringId)
                {
                    query = query.Where(n => n.RelatedEntityStringId == stringId);
                }

                var notesToDelete = await query.ToListAsync();

                if (!notesToDelete.Any())
                {
                    response.ResponseInfo.Success = true;
                    response.ResponseInfo.Message = "No notes found to delete";
                    return response;
                }

                context.Notes.RemoveRange(notesToDelete);
                await context.SaveChangesAsync();

                // Log the bulk deletion
                if (entityId is int intEntityId)
                {
                    await _auditService.LogEntityChange(
                        entityType,
                        intEntityId,
                        userId,
                        "DeleteNotes",
                        $"Deleted {notesToDelete.Count} notes for entity");
                }

                response.ResponseInfo.Success = true;
                response.ResponseInfo.Message = $"{notesToDelete.Count} notes deleted successfully";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error deleting notes by entity: {ex.Message}";
            }

            return response;
        }

        public async Task<ResponseModel> GetNoteStatistics(int companyId)
        {
            var response = new ResponseModel();

            try
            {
                using var context = _contextFactory.CreateDbContext();

                // Get entities associated with the company
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

                // Define the filtering condition for notes related to this company
                var companyNotesQuery = context.Notes.Where(n =>
                    (n.RelatedEntityType == "Property" && propertyIds.Contains(n.RelatedEntityId.Value)) ||
                    (n.RelatedEntityType == "PropertyOwner" && ownerIds.Contains(n.RelatedEntityId.Value)) ||
                    (n.RelatedEntityType == "PropertyTenant" && tenantIds.Contains(n.RelatedEntityId.Value)) ||
                    (n.RelatedEntityType == "PropertyBeneficiary" && beneficiaryIds.Contains(n.RelatedEntityId.Value)) ||
                    (n.RelatedEntityType == "Company" && n.RelatedEntityId == companyId)
                );

                // Get statistics
                var totalNotes = await companyNotesQuery.CountAsync();
                var privateNotes = await companyNotesQuery.CountAsync(n => n.IsPrivate);
                var publicNotes = totalNotes - privateNotes;

                var notesByType = await companyNotesQuery
                    .GroupBy(n => n.NoteTypeId)
                    .Select(g => new { TypeId = g.Key, Count = g.Count() })
                    .ToListAsync();

                var noteTypes = await context.NoteTypes.ToListAsync();

                var notesByEntityType = await companyNotesQuery
                    .GroupBy(n => n.RelatedEntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToListAsync();

                var recentNotes = await companyNotesQuery
                    .OrderByDescending(n => n.CreatedOn)
                    .Take(10)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.CreatedOn,
                        n.CreatedBy,
                        n.RelatedEntityType,
                        n.RelatedEntityId,
                        n.RelatedEntityStringId
                    })
                    .ToListAsync();

                // Combine results
                var statistics = new
                {
                    TotalNotes = totalNotes,
                    PublicNotes = publicNotes,
                    PrivateNotes = privateNotes,
                    NotesByType = notesByType.Select(n => new
                    {
                        TypeId = n.TypeId,
                        TypeName = noteTypes.FirstOrDefault(t => t.Id == n.TypeId)?.Name,
                        Count = n.Count
                    }),
                    NotesByEntityType = notesByEntityType,
                    RecentNotes = recentNotes
                };

                response.Response = statistics;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = $"Error retrieving note statistics: {ex.Message}";
            }

            return response;
        }
    }
}