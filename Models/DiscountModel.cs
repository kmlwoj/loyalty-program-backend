namespace lojalBackend.Models
{
    public class DiscountModel
    {
        public enum DiscountType
        {
            Absolute,
            Percent
        }
        public int? ID { get; set; }
        public string? Name { get; set; }
        public int Amount { get; set; }
        public DiscountType Type { get; set; }
        public int NewPrice { get; set; }
        public DateTime Expiry { get; set; }
        public DiscountModel()
        {
            Expiry = DateTime.MinValue;
        }
        public DiscountModel(int? iD, string? name, string reduction, DateTime expiry, int oldPrice)
        {
            ID = iD;
            Name = name;
            Expiry = expiry;
            ParseReduction(reduction);
            CalculatePrice(oldPrice);
        }
        private void ParseReduction(string reduction)
        {
            if (string.IsNullOrWhiteSpace(reduction))
                throw new ArgumentException("Wrong reduction string given to the ParseReduction method!");

            Type = "%".Equals(reduction[^1]) ? DiscountType.Percent : DiscountType.Absolute;
            Amount = int.Parse(Type.Equals(DiscountType.Percent) ? reduction[..1] : reduction);
        }
        private void CalculatePrice(int oldPrice)
        {
            if(Type.Equals(DiscountType.Percent))
            {
                float converted = Amount / 100.0f;
                NewPrice = oldPrice - (int)Math.Round(converted * oldPrice);
            }
            else
            {
                int diff = oldPrice - Amount;
                NewPrice = diff < 0 ? 0 : diff;
            }
        }
    }
}
