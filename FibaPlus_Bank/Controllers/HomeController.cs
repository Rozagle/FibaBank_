using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace FibaPlus_Bank.Controllers
{
    public class HomeController : Controller
    {
        private readonly FibraPlusBankDbContext _context;

        public HomeController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var user = _context.Users.Find(userId);
            ViewBag.UserName = user?.FullName;

            var activeCards = _context.Cards
                .Include(c => c.Account)
                .ThenInclude(a => a.User)
                .Where(c => c.Account.UserId == userId && c.Status == "Active")
                .ToList();
            ViewBag.ActiveCards = activeCards;

            var allAccounts = _context.Accounts.Where(x => x.UserId == userId).ToList();
            var investments = _context.Investments.Where(x => x.UserId == userId).ToList();

            decimal valTRY = 0;
            decimal valUSD = 0;
            decimal valEUR = 0;
            decimal valGold = 0;

            foreach (var acc in allAccounts)
            {
                if (acc.AccountType == "Kredi") continue; 

                decimal balance = acc.Balance ?? 0;

                if (acc.CurrencyCode == "TRY") valTRY += balance;
                else if (acc.CurrencyCode == "USD") valUSD += balance * 34.20m;
                else if (acc.CurrencyCode == "EUR") valEUR += balance * 37.50m;
                else if (acc.CurrencyCode == "XAU" || acc.CurrencyCode == "GAU") valGold += balance * 3050m;
            }

            decimal totalInvestmentsTRY = investments.Sum(x => x.Quantity * x.CurrentPrice);
            valTRY += totalInvestmentsTRY;
            decimal totalAssets = valTRY + valUSD + valEUR + valGold;

            ViewBag.TotalTRY = valTRY;
            ViewBag.TotalUSD = valUSD / 34.20m; 
            ViewBag.TotalEUR = valEUR / 37.50m;
            ViewBag.TotalGold = valGold / 3050m;
            ViewBag.TotalAssets = totalAssets;
            ViewBag.ChartValues = new List<decimal> { valTRY, valUSD, valEUR, valGold };

            var recentTransactions = _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(7)
                .ToList();

            return View(recentTransactions);
        }

        [HttpPost]
        public IActionResult CheckCardForTransfer(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return Json(new { success = false, message = "Kart numarası boş olamaz." });

            string cleanNumber = cardNumber.Replace(" ", "").Trim();

            var card = _context.Cards
                .Include(c => c.Account)
                .FirstOrDefault(c => c.CardNumber == cleanNumber);

            if (card == null)
            {
                return Json(new { success = false, message = "Kart bulunamadı." });
            }

            if (card.Status == "Pending")
            {
                return Json(new { success = false, message = "Bu kart henüz onaylanmamış." });
            }

            return Json(new { success = true, accountId = card.AccountId });
        }

        [HttpPost]
        public IActionResult CheckCardStatus(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return Json(new { success = false, message = "Lütfen bir kart numarası girin." });

            string cleanNumber = cardNumber.Replace(" ", "").Trim();

            var card = _context.Cards.FirstOrDefault(c => c.CardNumber == cleanNumber);

            if (card == null)
            {
                return Json(new { success = true });
            }

            if (card.Status == "Inactive")
            {
                return Json(new { success = false, message = "Bu kart aktif değil (Dondurulmuş)." });
            }

            if (card.Status == "Pending")
            {
                return Json(new { success = false, message = "Bu kart henüz onaylanmamış." });
            }
            return Json(new { success = true });
        }
    }
}