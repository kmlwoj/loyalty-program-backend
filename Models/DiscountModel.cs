using System.Text.Json.Serialization;

namespace lojalBackend.Models
{
    public class DiscountModel
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum DiscountType
        {
            Absolute,
            Percent
        }
        public int? ID { get; set; }
        public string? Name { get; set; }
        public int Amount { get; set; }
        public DiscountType Type { get; set; }
        public int? NewPrice { get; set; }
        public DiscountModel() { }
        public DiscountModel(int? iD, string? name, string reduction, int oldPrice)
        {
            ID = iD;
            Name = name;
            ParseReduction(reduction);
            CalculatePrice(oldPrice);
        }
        private void ParseReduction(string reduction)
        {
            if (string.IsNullOrWhiteSpace(reduction))
                throw new ArgumentException("Wrong reduction string given to the ParseReduction method!");

            Type = '%'.Equals(reduction[^1]) ? DiscountType.Percent : DiscountType.Absolute;
            Amount = int.Parse(Type.Equals(DiscountType.Percent) ? reduction[..(reduction.Length - 1)] : reduction);
        }
        public string GetReductionString() => string.Concat(Amount, Type.Equals(DiscountType.Percent) ? "%" : string.Empty);
        public void CalculatePrice(int oldPrice)
        {
            if(Type.Equals(DiscountType.Percent))
            {
                if (Amount > 100)
                    throw new ArgumentException("Percentage amount cannot be higher than 100!");
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
