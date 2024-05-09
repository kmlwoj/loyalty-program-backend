namespace lojalBackend.Models
{
    public abstract class OfferModel
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public string Organization { get; set; }
        public string? Category { get; set; }
        public bool? HasImage { get; set; }
        public OfferModel()
        {
            Name = string.Empty;
            Price = 0;
            Organization = string.Empty;
            HasImage = false;
        }
        public OfferModel(int? iD, string name, int price, string organization, string? category, bool? hasImage)
        {
            ID = iD;
            Name = name;
            Price = price;
            Organization = organization;
            Category = category;
            HasImage = hasImage;
        }
    }
}
