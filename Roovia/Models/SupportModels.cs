using Microsoft.AspNetCore.Components.Forms;

namespace Roovia.Models
{
    public class SupportModels
    {
        // Model classes
        public class SupportTicket
        {
            public int Id { get; set; }

            public Guid? UserId { get; set; }

            public int CompanyId { get; set; }
            public string TicketNumber { get; set; }
            public string Subject { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public string Priority { get; set; }
            public DateTime CreatedDate { get; set; }

            List<UploadedFile> Files { get; set; }

            public TicketStatuses TicketStatus { get; set; }
        }



        public class FaqItem
        {
            public int Id { get; set; }
            public string Question { get; set; }
            public string Answer { get; set; }
            public string Category { get; set; }
            public string RelatedLink { get; set; }
        }

        public class UploadedFile
        {
            public string Name { get; set; }
            public string ContentType { get; set; }

            public string UploadBase64 { get; set; }
        }
        public class TicketComment
        {
            public int Id { get; set; }
            public int TicketId { get; set; }
            public Guid? UserId { get; set; }
            public string Comment { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public enum TicketStatuses
        {
            New, 
            Pending, 
            Closed
        }
    }
}
