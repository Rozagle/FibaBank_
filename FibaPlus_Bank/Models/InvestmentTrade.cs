namespace FibaPlus_Bank.Models
{
    public class InvestmentTrade
    {
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string TransactionType { get; set; } 
        public string? InvestmentType { get; set; }
        public string? InstrumentType { get; set; }
        public string? InstrumentName { get; set; }
    }
}