
using Roovia.Models.BusinessHelperModels;
using System.Threading.Tasks;

namespace Roovia.Interfaces
{
    public interface INoteService
    {
        // CRUD Operations
        Task<ResponseModel> CreateNote(Note note);
        Task<ResponseModel> GetNoteById(int id);
        Task<ResponseModel> UpdateNote(int id, Note updatedNote);
        Task<ResponseModel> DeleteNote(int id, string userId);

        // Listing Methods
        Task<ResponseModel> GetNotesByEntity(string entityType, object entityId, bool includePrivate = false);
        Task<ResponseModel> GetNotesByNoteType(int noteTypeId, bool includePrivate = false);
        Task<ResponseModel> GetRecentNotes(int companyId, int count = 10, bool includePrivate = false);

        // Search Methods
        Task<ResponseModel> SearchNotes(string searchTerm, int companyId, bool includePrivate = false);

        // Bulk Operations
        Task<ResponseModel> CreateBulkNotes(List<Note> notes);
        Task<ResponseModel> DeleteNotesByEntity(string entityType, object entityId, string userId);

        // Statistics
        Task<ResponseModel> GetNoteStatistics(int companyId);
    }
}