namespace lojalBackend.Models
{
    public class CategoryModel
    {
        public string Name { get; set; }
        public CategoryModel()
        {
            Name = string.Empty;
        }
        public CategoryModel(string name)
        {
            Name = name;
        }
    }
}
