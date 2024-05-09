namespace lojalBackend.Models
{
    public class ShopOfferModel : OfferModel
    {
        public bool IsActive { get; set; }
        public ShopDiscountModel? Discount { get; set; }
        public ShopOfferModel() : base()
        {
            IsActive = false;
        }
        public ShopOfferModel(int? iD, string name, int price, string organization, bool isActive, string? category, ShopDiscountModel? discount, bool? hasImage) : base(iD, name, price, organization, category, hasImage)
        {
            IsActive = isActive;
            Discount = discount;
        }
    }
}
