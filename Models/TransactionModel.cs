namespace lojalBackend.Models
{
    public class TransactionModel
    {
        public int ID { get; set; }
        public ClientOfferModel Offer { get; set; }
        public NewCodeModel Code { get; set; }
        public DateTime Date { get; set; }
        public TransactionModel()
        {
            Offer = new();
            Code = new();
            Date = DateTime.MinValue;
        }
        public TransactionModel(int iD, ClientOfferModel offer, NewCodeModel code, DateTime date)
        {
            ID = iD;
            Offer = offer;
            Code = code;
            Date = date;
        }
    }
}
