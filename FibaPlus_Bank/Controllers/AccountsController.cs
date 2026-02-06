using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibaPlus_Bank.Controllers
{
    public class AccountsController : Controller
    {
        private readonly FibraPlusBankDbContext _context;

        public AccountsController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var userAccounts = _context.Accounts
                                       .Where(x => x.UserId == userId)
                                       .ToList();

            return View(userAccounts);
        }

        public IActionResult Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var account = _context.Accounts
                .Include(a => a.User) 
                .FirstOrDefault(x => x.AccountId == id);

            if (account == null || account.UserId != userId) return RedirectToAction("Index");

            if (account.AccountType == "Vadeli")
            {
                DateTime openDate = account.AccountOpenDate ?? DateTime.Now;
                DateTime maturityDate = openDate.AddDays(account.TermDays ?? 32);

                int daysRemaining = (maturityDate - DateTime.Now).Days;
                if (daysRemaining < 0) daysRemaining = 0;

                decimal principal = account.Balance ?? 0;
                decimal rate = account.InterestRate ?? 0;
                int days = account.TermDays ?? 32;

                decimal grossInterest = (principal * rate * days) / 36500;

                decimal taxRate = 0.075m;
                decimal withholdingTax = grossInterest * taxRate;

                decimal netInterest = grossInterest - withholdingTax;

                ViewBag.MaturityDate = maturityDate;
                ViewBag.DaysRemaining = daysRemaining;
                ViewBag.GrossInterest = grossInterest;
                ViewBag.WithholdingTax = withholdingTax; // Stopaj
                ViewBag.NetInterest = netInterest;       // Net Kazanç
                ViewBag.TotalAtMaturity = principal + netInterest; // Vade Sonu Toplam
            }

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int productId, string selectedAccountType, int? sourceAccountId, int termDays = 32)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var product = _context.AccountProducts.Find(productId);
            if (product == null) return RedirectToAction("Index");

            if (selectedAccountType == "Vadeli" && product.CurrencyCode != "TRY")
            {
                TempData["Error"] = "Vadeli hesap sadece Türk Lirası (TL) ürünlerinde geçerlidir.";
                return RedirectToAction("Create");
            }

            using (var transactionScope = _context.Database.BeginTransaction())
            {
                try
                {
                    decimal initialBalance = 0;
                    decimal interestRate = 0;
                    int finalTermDays = 0;
                    string oldIban = "Bilinmiyor";

                    if (selectedAccountType == "Vadeli")
                    {
                        if (sourceAccountId == null)
                        {
                            TempData["Error"] = "Lütfen kaynak hesap seçin.";
                            return RedirectToAction("Create");
                        }

                        var sourceAccount = _context.Accounts.FirstOrDefault(x => x.AccountId == sourceAccountId && x.UserId == userId);

                        if (sourceAccount == null || sourceAccount.Balance <= 0)
                        {
                            TempData["Error"] = "Seçilen hesapta bakiye yok.";
                            return RedirectToAction("Create");
                        }

                        initialBalance = sourceAccount.Balance ?? 0;
                        oldIban = sourceAccount.Iban;
                        finalTermDays = termDays;
                        interestRate = GetInterestRateFromDb(initialBalance, finalTermDays);
                        if (interestRate == 0) interestRate = 1.0m;

                       
                        var linkedCards = _context.Cards.Where(c => c.AccountId == sourceAccount.AccountId).ToList();
                        if (linkedCards.Any()) _context.Cards.RemoveRange(linkedCards);

                        var linkedTrans = _context.Transactions.Where(t => t.AccountId == sourceAccount.AccountId).ToList();
                        if (linkedTrans.Any()) _context.Transactions.RemoveRange(linkedTrans);

              
                        _context.Accounts.Remove(sourceAccount);
                        _context.Entry(sourceAccount).State = EntityState.Deleted;

                        _context.SaveChanges();
                    }

                    Random rnd = new Random();
                    string accNo = "";
                    do { accNo = ""; for (int i = 0; i < 16; i++) accNo += rnd.Next(0, 10).ToString(); }
                    while (_context.Accounts.Any(a => a.AccountNumber == accNo));

                    var newAccount = new Account
                    {
                        UserId = (int)userId,
                        AccountName = $"{product.ProductName}",
                        AccountNumber = accNo,
                        Iban = $"TR{rnd.Next(10, 99)}560000{accNo}",
                        CurrencyCode = product.CurrencyCode,
                        AccountType = selectedAccountType,
                        Balance = initialBalance,
                        AccountOpenDate = DateTime.Now,
                        AccruedInterest = 0,
                        TermDays = finalTermDays,
                        InterestRate = interestRate
                    };

                    _context.Accounts.Add(newAccount);
                    _context.SaveChanges(); 

                    if (initialBalance > 0)
                    {
                        var transLog = new Transaction
                        {
                            AccountId = newAccount.AccountId,
                            TransactionType = "Transfer",
                            Amount = initialBalance,
                            TransactionDate = DateTime.Now,
                            Description = "Vadeli Hesap Açılış Transferi",
                            SenderIBAN = oldIban,
                            ReceiverIBAN = newAccount.Iban
                        };
                        _context.Transactions.Add(transLog);
                        _context.SaveChanges();
                    }

                    transactionScope.Commit();

                    TempData["Success"] = "İşlem başarılı. Eski hesap kapatıldı ve silindi.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transactionScope.Rollback();
                    var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    TempData["Error"] = "Hata oluştu (İşlem Geri Alındı): " + msg;
                    return RedirectToAction("Create");
                }
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var products = _context.AccountProducts.ToList();
            ViewBag.Products = products;

            var sourceAccounts = _context.Accounts
                .Where(x => x.UserId == userId && x.AccountType == "Vadesiz" && x.CurrencyCode == "TRY" && x.Balance > 0)
                .ToList();

            ViewBag.SourceAccounts = sourceAccounts;

            return View();
        }
        private decimal GetInterestRateFromDb(decimal amount, int days)
        {
            var tier = _context.InterestTiers
                .FirstOrDefault(x =>
                    days >= x.MinDays && days <= x.MaxDays &&
                    amount >= x.MinAmount && amount <= x.MaxAmount
                );

            return tier != null ? tier.InterestRate : 0;
        }


    }
}