using FibaPlus_Bank.Models; 
using System.Collections.Generic;

namespace FibaPlus_Bank.Models
{
    // CollectAPI'den dönen res yapısı
    public class CollectApiResponse<T>
    {
        public bool success { get; set; }
        public List<T> result { get; set; }
    }

    public class MarketItem
    {
        public string name { get; set; }
        public string code { get; set; }
        public double buying { get; set; }
        public double selling { get; set; }
        public string rate { get; set; }
        public string time { get; set; }
        public double price { get; set; }
    }

    public class TradeViewModel
    {
        public List<MarketItem> GoldList { get; set; }
        public List<MarketItem> CurrencyList { get; set; }
        public List<MarketItem> StockList { get; set; }
        public List<Account> Accounts { get; set; }
        public decimal UserBalance { get; set; }
    }
}