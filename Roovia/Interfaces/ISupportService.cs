using Roovia.Models.BusinessHelperModels;
using static Roovia.Models.SupportModels;

namespace Roovia.Interfaces
{

    public interface ISupportService
    {
        // SupportTicket CRUD
        Task<ResponseModel> CreateSupportTicketAsync(SupportTicket ticket);
        Task<ResponseModel> GetSupportTicketAsync(int id);
        Task<ResponseModel> GetAllSupportTicketsAsync();
        Task<ResponseModel> UpdateSupportTicketAsync(SupportTicket ticket);
        Task<ResponseModel> DeleteSupportTicketAsync(int id);

        // FaqItem CRUD
        Task<ResponseModel> CreateFaqItemAsync(FaqItem item);
        Task<ResponseModel> GetFaqItemAsync(int id);
        Task<ResponseModel> GetAllFaqItemsAsync();
        Task<ResponseModel> UpdateFaqItemAsync(FaqItem item);
        Task<ResponseModel> DeleteFaqItemAsync(int id);

        // UploadedFile CRUD
        Task<ResponseModel> CreateUploadedFileAsync(int ticketId, string name, string contentType, string uploadBase64);
        Task<ResponseModel> GetUploadedFileAsync(int id);
        Task<ResponseModel> GetAllUploadedFilesAsync();
        Task<ResponseModel> UpdateUploadedFileAsync(int id, string name, string contentType, string uploadBase64);
        Task<ResponseModel> DeleteUploadedFileAsync(int id);
        // TicketComment CRUD
        Task<ResponseModel> CreateTicketCommentAsync(TicketComment comment);
        Task<ResponseModel> GetTicketCommentAsync(int id);
        Task<ResponseModel> GetAllTicketCommentsAsync(int ticketId);
        Task<ResponseModel> UpdateTicketCommentAsync(TicketComment comment);
        Task<ResponseModel> DeleteTicketCommentAsync(int id);

        // SupportTicket by CompanyId
        Task<ResponseModel> GetSupportTicketsByCompanyIdAsync(int companyId);


    }

}
