namespace lojalBackend.Models
{
    public class CodeModel
    {
        public int Code { get; set; }
        public bool IsUsed { get; set; }
        public DateTime Expiry { get; set; }
        public CodeModel()
        {
            Expiry = DateTime.MinValue;
        }
        public CodeModel(int code, bool isUsed, DateTime expiry)
        {
            Code = code;
            IsUsed = isUsed;
            Expiry = expiry;
        }
    }
}
