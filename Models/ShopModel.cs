namespace lojalBackend.Models
{
    public class ShopModel
    {
        public string Name { get; set; }
        public bool HasImage { get; set; }
        public ShopModel()
        {
            Name = string.Empty;
        }
        public ShopModel(string name, bool hasImage)
        {
            Name = name;
            HasImage = hasImage;
        }
    }
}
