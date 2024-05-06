namespace lojalBackend.Models
{
    public class NewCodeModel
    {
        public int Code { get; set; }
        public DateTime Expiry { get; set; }
        public NewCodeModel()
        {
            Expiry = DateTime.MinValue;
        }
        public NewCodeModel(int code, DateTime expiry)
        {
            Code = code;
            Expiry = expiry;
        }
    }
}
