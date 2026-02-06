using FibaPlus_Bank.Models;
using FibaPlus_Bank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FibaPlus_Bank.Controllers
{
    public class InvestmentsController : Controller
    {
        private readonly FibraPlusBankDbContext _context;
        private readonly MarketDataService _marketService;
        public InvestmentsController(FibraPlusBankDbContext context, MarketDataService marketService)
        {
            _context = context;
            _marketService = marketService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var portfolio = _context.Investments
              .Where(i => i.UserId == userId && i.Quantity > 0)
              .ToList();

            var stocks = (await _marketService.GetStocks()) ?? new List<MarketItem>();
            var gold = (await _marketService.GetGoldPrices()) ?? new List<MarketItem>();
            var currencies = (await _marketService.GetCurrencyRates()) ?? new List<MarketItem>();

            foreach (var item in portfolio)
            {
                decimal currentMarketPrice = item.CurrentPrice;

                if (item.InvestmentType == "STOCK")
                {
                    var data = stocks.FirstOrDefault(s => s.code == item.InstrumentCode);
                    if (data != null) currentMarketPrice = (decimal)data.selling;
                }
                else if (item.InvestmentType == "GOLD" || item.InvestmentType == "COMMODITY")
                {
                    var data = gold.FirstOrDefault(g => g.code == item.InstrumentCode);
                    if (data != null) currentMarketPrice = (decimal)data.selling;
                }
                else if (item.InvestmentType == "CURRENCY" || item.InvestmentType == "FOREX")
                {
                    var data = currencies.FirstOrDefault(c => c.code == item.InstrumentCode);
                    if (data != null) currentMarketPrice = (decimal)data.selling;
                }

                item.CurrentPrice = currentMarketPrice;
            }

            decimal totalPortfolioValue = portfolio.Sum(x => x.Quantity * x.CurrentPrice);
            decimal totalCost = portfolio.Sum(x => x.Quantity * x.PurchasePrice);

            ViewBag.TotalPortfolioValue = totalPortfolioValue;
            ViewBag.TotalProfitLoss = totalPortfolioValue - totalCost;

            var portfolioDistribution = portfolio
              .GroupBy(x => x.InvestmentType ?? "DIGER")
              .Select(g => new {
                  Type = g.Key,
                  TotalValue = g.Sum(x => x.Quantity * x.CurrentPrice)
              })
              .OrderByDescending(x => x.TotalValue)
              .ToList();

            List<string> labels = new List<string>();
            List<decimal> values = new List<decimal>();
            List<string> colors = new List<string>();
            var distributionList = new List<PortfolioItemDTO>();

            foreach (var item in portfolioDistribution)
            {
                string labelName = item.Type;
                string colorCode = "#6c757d";

                switch (item.Type)
                {
                    case "STOCK": labelName = "Hisse Senedi"; colorCode = "#6f42c1"; break;
                    case "GOLD": case "COMMODITY": labelName = "Altın & Emtia"; colorCode = "#ffc107"; break;
                    case "CURRENCY": case "FOREX": labelName = "Döviz"; colorCode = "#20c997"; break;
                    case "FUND": labelName = "Fon"; colorCode = "#198754"; break;
                }

                labels.Add(labelName);
                values.Add(item.TotalValue);
                colors.Add(colorCode);

                decimal percent = totalPortfolioValue > 0 ? (item.TotalValue / totalPortfolioValue) * 100 : 0;

                distributionList.Add(new PortfolioItemDTO
                {
                    Label = labelName,
                    Color = colorCode,
                    Value = item.TotalValue,
                    Percentage = percent
                });
            }

            ViewBag.ChartLabels = labels;
            ViewBag.ChartValues = values;
            ViewBag.ChartColors = colors;
            ViewBag.DistributionList = distributionList;

            return View(portfolio.OrderByDescending(x => x.Quantity * x.CurrentPrice).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Trade()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var accounts = _context.Accounts
              .Where(x => x.UserId == userId && x.CurrencyCode == "TRY" && x.AccountType == "Vadesiz")
              .ToList();

            var model = new TradeViewModel();
            try
            {
                model.GoldList = (await _marketService.GetGoldPrices()) ?? new List<MarketItem>();
                model.CurrencyList = (await _marketService.GetCurrencyRates()) ?? new List<MarketItem>();
                model.StockList = (await _marketService.GetStocks()) ?? new List<MarketItem>();
            }
            catch
            {
                model.GoldList = new List<MarketItem>();
                model.CurrencyList = new List<MarketItem>();
                model.StockList = new List<MarketItem>();
            }

            model.Accounts = accounts;
            model.UserBalance = accounts.FirstOrDefault()?.Balance ?? 0;

            return View(model);
        }

        [HttpPost]
        public IActionResult TradePost(InvestmentTrade model, int accountId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var account = _context.Accounts.FirstOrDefault(x => x.AccountId == accountId && x.UserId == userId);
            if (account == null)
            {
                TempData["Error"] = "Geçerli bir hesap seçilmedi.";
                return RedirectToAction("Trade");
            }

            try
            {
                var pUserId = new SqlParameter("@UserId", userId);
                var pAccountId = new SqlParameter("@AccountId", accountId);
                var pSymbol = new SqlParameter("@InstrumentCode", model.Symbol ?? "");
                var pQty = new SqlParameter("@Quantity", model.Quantity);
                var pPrice = new SqlParameter("@UnitPrice", model.UnitPrice);

                if (model.TransactionType == "BUY")
                {
                    var pInvType = new SqlParameter("@InvestmentType", model.InvestmentType ?? "STOCK");
                    var pInstType = new SqlParameter("@InstrumentType", model.InstrumentType ?? "EQUITY");
                    var pName = new SqlParameter("@InstrumentName", model.InstrumentName ?? model.Symbol);

                    _context.Database.ExecuteSqlRaw(
                      "EXEC sp_BuyInvestment @UserId, @AccountId, @InstrumentCode, @InvestmentType, @InstrumentType, @InstrumentName, @Quantity, @UnitPrice",
                      pUserId, pAccountId, pSymbol, pInvType, pInstType, pName, pQty, pPrice);

                    TempData["Success"] = $"{model.Symbol} alım işlemi başarılı!";
                }
                else if (model.TransactionType == "SELL")
                {
                    _context.Database.ExecuteSqlRaw(
                      "EXEC sp_SellInvestment @UserId, @AccountId, @InstrumentCode, @Quantity, @UnitPrice",
                      pUserId, pAccountId, pSymbol, pQty, pPrice);

                    TempData["Success"] = $"{model.Symbol} satış işlemi başarılı!";
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50001) TempData["Error"] = "Yetersiz Bakiye!";
                else if (ex.Number == 50002) TempData["Error"] = "Yetersiz Hisse!";
                else TempData["Error"] = "Hata: " + ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hata: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public class PortfolioItemDTO
        {
            public string Label { get; set; } = string.Empty;
            public decimal Value { get; set; }
            public string Color { get; set; } = "#ccc";
            public decimal Percentage { get; set; }
        }
    }
}