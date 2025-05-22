using Dapper;
using Microsoft.Data.SqlClient;
using Roovia.Interfaces;
using Roovia.Models.BusinessHelperModels;
using static Roovia.Models.SupportModels;

namespace Roovia.Services
{
    public class SupportService : ISupportService
    {
        private readonly string _connectionString;

        public SupportService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<ResponseModel> CreateSupportTicketAsync(SupportTicket ticket)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"INSERT INTO Support_SupportTickets (TicketNumber, Subject, Description, Category, Status, Priority, CreatedDate, UserId, CompanyId)
                                        VALUES (@TicketNumber, @Subject, @Description, @Category, @Status, @Priority, @CreatedDate, @UserId, @CompanyId);
                                        SELECT CAST(SCOPE_IDENTITY() as int);";
                var id = await connection.QuerySingleAsync<int>(sql, ticket);
                ticket.Id = id;
                response.Response = ticket;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetSupportTicketAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketNumber, Subject, Description, Category, Status, Priority, CreatedDate, UserId, CompanyId FROM Support_SupportTickets WHERE Id = @Id";
                var ticket = await connection.QuerySingleOrDefaultAsync<SupportTicket>(sql, new { Id = id });
                response.Response = ticket;
                response.ResponseInfo.Success = ticket != null;
                if (ticket == null)
                    response.ResponseInfo.Message = "Ticket not found.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetAllSupportTicketsAsync()
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketNumber, Subject, Description, Category, Status, Priority, CreatedDate, UserId, CompanyId FROM Support_SupportTickets";
                var tickets = await connection.QueryAsync<SupportTicket>(sql);
                response.Response = tickets.ToList();
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }


        public async Task<ResponseModel> UpdateSupportTicketAsync(SupportTicket ticket)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"UPDATE Support_SupportTickets SET
                                        TicketNumber = @TicketNumber,
                                        Subject = @Subject,
                                        Description = @Description,
                                        Category = @Category,
                                        Status = @Status,
                                        Priority = @Priority,
                                        CreatedDate = @CreatedDate,
                                        UserId = @UserId,
                                        CompanyId = @CompanyId
                                        WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, ticket);
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "Ticket not found or not updated.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> DeleteSupportTicketAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "DELETE FROM Support_SupportTickets WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, new { Id = id });
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "Ticket not found or not deleted.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        // FaqItem CRUD

        public async Task<ResponseModel> CreateFaqItemAsync(FaqItem item)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"INSERT INTO Support_FaqItems (Question, Answer, Category, RelatedLink)
                                    VALUES (@Question, @Answer, @Category, @RelatedLink);
                                    SELECT CAST(SCOPE_IDENTITY() as int);";
                var id = await connection.QuerySingleAsync<int>(sql, item);
                item.Id = id;
                response.Response = item;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetFaqItemAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, Question, Answer, Category, RelatedLink FROM Support_FaqItems WHERE Id = @Id";
                var item = await connection.QuerySingleOrDefaultAsync<FaqItem>(sql, new { Id = id });
                response.Response = item;
                response.ResponseInfo.Success = item != null;
                if (item == null)
                    response.ResponseInfo.Message = "FAQ item not found.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetAllFaqItemsAsync()
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, Question, Answer, Category, RelatedLink FROM Support_FaqItems";
                var items = await connection.QueryAsync<FaqItem>(sql);
                response.Response = items.ToList();
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> UpdateFaqItemAsync(FaqItem item)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"UPDATE Support_FaqItems SET
                                    Question = @Question,
                                    Answer = @Answer,
                                    Category = @Category,
                                    RelatedLink = @RelatedLink
                                    WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, item);
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "FAQ item not found or not updated.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> DeleteFaqItemAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "DELETE FROM Support_FaqItems WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, new { Id = id });
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "FAQ item not found or not deleted.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        // UploadedFile CRUD

        public async Task<ResponseModel> CreateUploadedFileAsync(int ticketId, string name, string contentType, string uploadBase64)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"INSERT INTO Support_UploadedFiles (TicketId, Name, ContentType, UploadBase64)
                                    VALUES (@TicketId, @Name, @ContentType, @UploadBase64);
                                    SELECT CAST(SCOPE_IDENTITY() as int);";
                var id = await connection.QuerySingleAsync<int>(sql, new { TicketId = ticketId, Name = name, ContentType = contentType, UploadBase64 = uploadBase64 });
                response.Response = id;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetUploadedFileAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketId, Name, ContentType, UploadBase64 FROM Support_UploadedFiles WHERE Id = @Id";
                var file = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id });
                response.Response = file;
                response.ResponseInfo.Success = file != null;
                if (file == null)
                    response.ResponseInfo.Message = "Uploaded file not found.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetAllUploadedFilesAsync()
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketId, Name, ContentType, UploadBase64 FROM Support_UploadedFiles";
                var files = await connection.QueryAsync<dynamic>(sql);
                response.Response = files.ToList();
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> UpdateUploadedFileAsync(int id, string name, string contentType, string uploadBase64)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"UPDATE Support_UploadedFiles SET
                                    Name = @Name,
                                    ContentType = @ContentType,
                                    UploadBase64 = @UploadBase64
                                    WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, new { Name = name, ContentType = contentType, UploadBase64 = uploadBase64, Id = id });
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "Uploaded file not found or not updated.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> DeleteUploadedFileAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "DELETE FROM Support_UploadedFiles WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, new { Id = id });
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "Uploaded file not found or not deleted.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        // TicketComment CRUD

        public async Task<ResponseModel> CreateTicketCommentAsync(TicketComment comment)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"INSERT INTO Support_TicketComments (TicketId, UserId, Comment, CreatedDate)
                            VALUES (@TicketId, @UserId, @Comment, @CreatedDate);
                            SELECT CAST(SCOPE_IDENTITY() as int);";
                var id = await connection.QuerySingleAsync<int>(sql, comment);
                comment.Id = id;
                response.Response = comment;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetTicketCommentAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketId, UserId, Comment, CreatedDate FROM Support_TicketComments WHERE Id = @Id";
                var comment = await connection.QuerySingleOrDefaultAsync<TicketComment>(sql, new { Id = id });
                response.Response = comment;
                response.ResponseInfo.Success = comment != null;
                if (comment == null)
                    response.ResponseInfo.Message = "Ticket comment not found.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> GetAllTicketCommentsAsync(int ticketId)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketId, UserId, Comment, CreatedDate FROM Support_TicketComments WHERE TicketId = @TicketId";
                var comments = await connection.QueryAsync<TicketComment>(sql, new { TicketId = ticketId });
                response.Response = comments as List<TicketComment>;
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> UpdateTicketCommentAsync(TicketComment comment)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = @"UPDATE Support_TicketComments SET
                                TicketId = @TicketId,
                                UserId = @UserId,
                                Comment = @Comment,
                                CreatedDate = @CreatedDate
                            WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, comment);
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "Ticket comment not found or not updated.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel> DeleteTicketCommentAsync(int id)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "DELETE FROM Support_TicketComments WHERE Id = @Id";
                var affected = await connection.ExecuteAsync(sql, new { Id = id });
                response.Response = affected > 0;
                response.ResponseInfo.Success = affected > 0;
                if (affected == 0)
                    response.ResponseInfo.Message = "Ticket comment not found or not deleted.";
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }
        public async Task<ResponseModel> GetSupportTicketsByCompanyIdAsync(int companyId)
        {
            var response = new ResponseModel();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var sql = "SELECT Id, TicketNumber, Subject, Description, Category, Status, Priority, CreatedDate, UserId, CompanyId FROM Support_SupportTickets WHERE CompanyId = @CompanyId";
                var tickets = await connection.QueryAsync<SupportTicket>(sql, new { CompanyId = companyId });
                response.Response = tickets.ToList();
                response.ResponseInfo.Success = true;
            }
            catch (Exception ex)
            {
                response.ResponseInfo.Success = false;
                response.ResponseInfo.Message = ex.Message;
            }
            return response;
        }
    }
    
}
