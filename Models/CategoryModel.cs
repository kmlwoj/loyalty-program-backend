namespace lojalBackend.Models
{
    public class CategoryModel
    {
        public string Name { get; set; }
        public bool HasImage { get; set; }
        public CategoryModel()
        {
            Name = string.Empty;
            HasImage = false;
        }
        public CategoryModel(string name, bool hasImage)
        {
            Name = name;
            HasImage = hasImage;
        }
    }
}
