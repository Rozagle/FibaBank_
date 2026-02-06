using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;

namespace FibaPlus_Bank.Controllers
{
    public class CardsController : Controller
    {
        private readonly FibraPlusBankDbContext _context;

        public CardsController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var myCards = _context.Cards
                .Include(c => c.Account)
                .ThenInclude(a => a.User)
                .Where(c => c.Account.UserId == userId)
                .OrderByDescending(c => c.CardId)
                .ToList();

            return View(myCards);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var user = _context.Users.FirstOrDefault(x => x.UserId == userId);
            bool isVip = false;

            if (user != null && !string.IsNullOrEmpty(user.Segment) && user.Segment == "VIP")
            {
                isVip = true;
            }

            var vadeliAccounts = _context.Accounts
                .Where(x => x.UserId == userId && x.AccountType == "Vadeli" && x.CurrencyCode == "TRY")
                .ToList();

            decimal totalVadeli = vadeliAccounts.Sum(x => x.Balance ?? 0);
            bool isEligible = totalVadeli >= 1000000;

            ViewBag.IsVip = isVip;
            ViewBag.IsEligible = isEligible;
            ViewBag.VadeliTotal = totalVadeli;
            ViewBag.CreditLimit = isEligible ? 200000 : 0;

            ViewBag.VadesizAccounts = _context.Accounts
                .Where(x => x.UserId == userId && x.AccountType == "Vadesiz")
                .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult Create(string cardType, int? linkedAccountId, string brand)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            Random rnd = new Random();

            string binCode = "4543";
            if (brand == "Mastercard") binCode = "5400";
            else if (brand == "Troy") binCode = "9792";

            string generatedCardNumber = binCode +
                                         rnd.Next(1000, 9999).ToString() +
                                         rnd.Next(1000, 9999).ToString() +
                                         rnd.Next(1000, 9999).ToString();

            int finalAccountId;

            if (cardType == "Credit")
            {

                string accountPart1 = rnd.Next(10000000, 99999999).ToString(); // 8 hane
                string accountPart2 = rnd.Next(10000000, 99999999).ToString(); // 8 hane
                string fullIban = "TR56000990" + accountPart1 + accountPart2;

                string accNo = accountPart1 + accountPart2;

                var newCreditAccount = new Account
                {
                    UserId = userId.Value,
                    AccountName = "Kredi Kartı Hesabı",
                    AccountNumber = accNo,
                    Iban = fullIban, 
                    Balance = 0,
                    CurrencyCode = "TRY",
                    AccountType = "Kredi",
                    AccountOpenDate = DateTime.Now,
                };

                _context.Accounts.Add(newCreditAccount);
                _context.SaveChanges();

                finalAccountId = newCreditAccount.AccountId;
            }
            else
            {
               
                finalAccountId = linkedAccountId ?? _context.Accounts.FirstOrDefault(u => u.UserId == userId && u.AccountType == "Vadesiz")?.AccountId ?? 0;

                if (finalAccountId == 0)
                {
                    return RedirectToAction("Index");
                }
            }

            var newCard = new Card
            {
                AccountId = finalAccountId,
                CardNumber = generatedCardNumber, 
                CardType = (cardType == "Credit") ? brand : "Debit",
                ExpiryDate = DateTime.Now.AddYears(4).ToString("MM/yy"),
                Cvv = rnd.Next(100, 999).ToString(),
                CardLimit = (cardType == "Credit") ? 200000 : 0,
                Debt = 0,
                IsInternetEnabled = true,
                Status = (cardType == "Credit") ? "Pending" : "Active"
            };

            _context.Cards.Add(newCard);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult ToggleCardStatus(int cardId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var card = _context.Cards
                .FirstOrDefault(c => c.CardId == cardId && c.Account.UserId == userId);

            if (card == null) return Json(new { success = false, message = "Kart bulunamadı." });

            if (card.Status == "Pending")
            {
                return Json(new { success = false, message = "Onay bekleyen kart işlem göremez." });
            }

            if (card.Status == "Active")
            {
                card.Status = "Inactive"; 
                card.IsInternetEnabled = false; 
            }
            else
            {
                card.Status = "Active"; 
            }

            _context.SaveChanges();

            return Json(new { success = true, newStatus = card.Status });
        }
    }
}