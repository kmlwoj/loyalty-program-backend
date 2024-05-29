using lojalBackend.DbContexts.MainContext;

namespace lojalBackend.Models
{
    public class ContactRequestModel
    {
        public int ContReqId { get; set; } // NOT NULL
        public DateTime ContReqDate { get; set; } // NOT NULL
        public string Subject { get; set; } // NOT NULL
        public string Body { get; set; } // NOT NULL

        public ContactRequestModel()
        {
            Subject = string.Empty;
            Body = string.Empty;
        }

        public ContactRequestModel(string subject, string body)
        {
            ContReqDate = DateTime.Now; // Initialize with the current date/time
            Subject = subject;
            Body = body;
        }

        public ContactRequestModel(int contReqId, DateTime contReqDate, string subject, string body)
        {
            ContReqId = contReqId;
            ContReqDate = contReqDate;
            Subject = subject;
            Body = body;
        }

        public ContactRequestModel(ContactRequest entity)
        {
            ContReqId = entity.ContReqId;
            ContReqDate = entity.ContReqDate;
            Subject = entity.Subject;
            Body = entity.Body;
        }

        public static ContactRequestModel FromEntity(ContactRequest entity)
        {
            return new ContactRequestModel
            {
                ContReqId = entity.ContReqId,
                ContReqDate = entity.ContReqDate,
                Subject = entity.Subject,
                Body = entity.Body
            };
        }

    }
}