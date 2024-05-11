namespace lojalBackend.Models
{
    public class ClientOfferModel : OfferModel
    {
        public DiscountModel? shopDiscount { get; set; }
        public ClientOfferModel() : base() { }
        public ClientOfferModel(int? iD, string name, int price, string organization, string? category, DiscountModel? shopDiscount, bool? hasImage) : base(iD, name, price, organization, category, hasImage)
        {
            this.shopDiscount = shopDiscount;
        }
    }
}
