namespace lojalBackend.Models
{
    public class ContactInfoModel
    {
        public string? Name { get; set; }
        public string? Position { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public ContactInfoModel()
        {
            Name = null;
            Position = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
        }

        public ContactInfoModel(string? name, string email, string phone, string? position)
        {
            Name = name;
            Position = position;
            Email = email;
            Phone = phone;
        }
    }
}