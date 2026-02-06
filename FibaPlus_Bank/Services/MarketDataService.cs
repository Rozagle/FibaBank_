using FibaPlus_Bank.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FibaPlus_Bank.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _client;

        private const string ApiKey = "apikey 4ai2q07J5xycZh49kab3a0:2USIo7tdZJiXlBjjLwrtGd";

        public MarketDataService(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri("https://api.collectapi.com/economy/");

            if (!_client.DefaultRequestHeaders.Contains("authorization"))
            {
                _client.DefaultRequestHeaders.Add("authorization", ApiKey);
            }
        }

        private async Task<List<T>> FetchDataAsync<T>(string endpoint)
        {
            try
            {
                var response = await _client.GetStringAsync(endpoint);
                var data = JsonConvert.DeserializeObject<CollectApiResponse<T>>(response);

                if (data != null && data.success)
                {
                    return data.result;
                }
            }
            catch (Exception)
            {
              
            }
            return new List<T>();
        }

        public async Task<List<MarketItem>> GetGoldPrices()
        {
            var rawData = await FetchDataAsync<MarketItem>("goldPrice");
            var filter = new[] { "Gram Altın", "Çeyrek Altın", "Yarım Altın", "Tam Altın" };

            if (!rawData.Any()) return GetDummyGold();

            return rawData
                .Where(x => filter.Contains(x.name))
                .Select(x => {
                    x.code = GetGoldCode(x.name);
                    x.time = DateTime.Now.ToString("HH:mm");
                    return x;
                })
                .ToList();
        }

        public async Task<List<MarketItem>> GetCurrencyRates()
        {
            var rawData = await FetchDataAsync<MarketItem>("allCurrency");
            var filter = new[] { "Dolar", "Euro", "Sterlin" };

            if (!rawData.Any()) return GetDummyCurrency();

            return rawData
                .Where(x => filter.Contains(x.name))
                .Select(x => {
                    x.time = DateTime.Now.ToString("HH:mm");
                    return x;
                })
                .ToList();
        }

        public async Task<List<MarketItem>> GetStocks()
        {
            var rawData = await FetchDataAsync<MarketItem>("liveBorsa");
            var myStocks = new[] { "THYAO", "GARAN", "ASELS", "EREGL", "KCHOL", "SISE" };

            if (!rawData.Any()) return GetDummyStocks();

            return rawData
                .Where(x => myStocks.Contains(x.name))
                .Select(x => new MarketItem
                {
                    name = GetStockName(x.name),
                    code = x.name,
                    buying = x.price,
                    selling = x.price,
                    rate = x.rate.ToString(),
                    time = DateTime.Now.ToString("HH:mm")
                }).ToList();
        }

        private string GetGoldCode(string name) => name switch
        {
            var n when n.Contains("Gram") => "GRAM-ALTIN",
            var n when n.Contains("Çeyrek") => "CEYREK",
            var n when n.Contains("Yarım") => "YARIM",
            var n when n.Contains("Tam") => "TAM",
            _ => "GOLD"
        };

        private string GetStockName(string code) => code switch
        {
            "THYAO" => "Türk Hava Yolları",
            "GARAN" => "Garanti Bankası",
            "ASELS" => "Aselsan",
            "EREGL" => "Ereğli Demir Çelik",
            "KCHOL" => "Koç Holding",
            "SISE" => "Şişecam",
            _ => code
        };

        // --- YEDEK (DUMMY) VERİLER ---
        // API limiti biterse veya hata verirse devreye girer
        private List<MarketItem> GetDummyGold() => new() {
            new() { name = "Gram Altın", code = "GRAM-ALTIN", buying = 3050.50, selling = 3065.20, rate = "%1.2" },
            new() { name = "Çeyrek Altın", code = "CEYREK", buying = 4980.00, selling = 5050.00, rate = "%0.5" },
            new() { name = "Tam Altın", code = "TAM", buying = 19900.00, selling = 20100.00, rate = "%0.8" }
        };

        private List<MarketItem> GetDummyCurrency() => new() {
            new() { name = "ABD Doları", code = "USD", buying = 34.20, selling = 34.50, rate = "%0.10" },
            new() { name = "Euro", code = "EUR", buying = 37.15, selling = 37.60, rate = "-%0.20" },
            new() { name = "Sterlin", code = "GBP", buying = 43.10, selling = 43.80, rate = "%0.05" }
        };

        private List<MarketItem> GetDummyStocks() => new() {
            new() { name = "Türk Hava Yolları", code = "THYAO", buying = 292.50, selling = 292.50, rate = "%1.50" },
            new() { name = "Garanti Bankası", code = "GARAN", buying = 112.40, selling = 112.40, rate = "-%0.30" },
            new() { name = "Aselsan", code = "ASELS", buying = 64.15, selling = 64.15, rate = "%0.80" },
            new() { name = "Ereğli Demir Çelik", code = "EREGL", buying = 52.80, selling = 52.80, rate = "%0.00" }
        };
    }
}