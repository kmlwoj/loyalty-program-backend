namespace lojalBackend.Models
{
    public class ShopDiscountModel : DiscountModel
    {
        public DateTime Expiry { get; set; }
        public ShopDiscountModel() : base()
        {
            Expiry = DateTime.MinValue;
        }
        public ShopDiscountModel(int? iD, string? name, string reduction, DateTime expiry, int oldPrice) : base(iD, name, reduction, oldPrice)
        {
            Expiry = expiry;
        }
    }
}
