namespace lojalBackend.Models
{
    public class OfferModel
    {
        public string Name { get; set; }
        public int Price { get; set; }
        public string Organization { get; set; }
        public bool IsActive { get; set; }
        public string? Category { get; set; }
        public DiscountModel? Discount { get; set; }
        public OfferModel()
        {
            Name = string.Empty;
            Price = 0;
            Organization = string.Empty;
            IsActive = false;
        }
        public OfferModel(string name, int price, string organization, bool isActive, string? category, DiscountModel? discount)
        {
            Name = name;
            Price = price;
            Organization = organization;
            IsActive = isActive;
            Category = category;
            Discount = discount;
        }
    }
}
