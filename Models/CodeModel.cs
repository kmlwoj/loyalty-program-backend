namespace lojalBackend.Models
{
    public class CodeModel : NewCodeModel
    {
        public bool IsUsed { get; set; }
        public CodeModel() : base() { }
        public CodeModel(int code, bool isUsed, DateTime expiry) : base(code, expiry)
        {
            IsUsed = isUsed;
        }
    }
}
