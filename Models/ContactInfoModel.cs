namespace lojalBackend.Models
{
    public class ContactInfoModel
    {
        public int Id { get; set; }
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

        public ContactInfoModel(int id, string? name, string email, string phone, string? position)
        {
            Id = id;
            Name = name;
            Position = position;
            Email = email;
            Phone = phone;
        }
    }
}