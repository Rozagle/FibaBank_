using FibaPlus_Bank.Models;
using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using MassTransit; 
using FibaPlus_Bank.Events;

namespace FibaPlus_Bank.Controllers
{
    public class AdminController : Controller
    {
        private readonly FibraPlusBankDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        public AdminController(FibraPlusBankDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");

            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            decimal volToday = _context.Transactions
                .Where(t => t.TransactionDate >= today)
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal volYesterday = _context.Transactions
                .Where(t => t.TransactionDate >= yesterday && t.TransactionDate < today)
                .Sum(t => (decimal?)t.Amount) ?? 0;

            decimal volPercentage = 0;
            if (volYesterday > 0) volPercentage = ((volToday - volYesterday) / volYesterday) * 100;
            else if (volToday > 0) volPercentage = 100;

            ViewBag.DailyVolume = volToday;
            ViewBag.VolChange = volPercentage;

            decimal currentDeposit = _context.Accounts.Sum(a => (decimal?)a.Balance) ?? 0;
            ViewBag.TotalDeposit = currentDeposit;
            ViewBag.DepositChange = 5.1m; 

            ViewBag.PendingCards = _context.Cards.Count(c => c.Status == "Pending");
            ViewBag.TotalUsers = _context.Users.Count(u => u.Role != "Admin");

            decimal tryVol = _context.Accounts.Where(a => a.CurrencyCode == "TRY").Sum(a => (decimal?)a.Balance) ?? 0;
            decimal usdVol = _context.Accounts.Where(a => a.CurrencyCode == "USD").Sum(a => (decimal?)a.Balance) ?? 0;
            decimal eurVol = _context.Accounts.Where(a => a.CurrencyCode == "EUR").Sum(a => (decimal?)a.Balance) ?? 0;
            ViewBag.PieData = new List<decimal> { tryVol, usdVol * 34, eurVol * 37 };

            var recentTransactions = _context.Transactions
                .Include(t => t.Account).ThenInclude(a => a.User)
                .Where(t => t.Account.User.Segment == "VIP" || t.Amount > 25000000)
                .OrderByDescending(t => t.TransactionDate)
                .Take(6)
                .ToList();

            return View(recentTransactions);
        }

        public IActionResult Users(string search)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");

            var usersQuery = _context.Users
                .Include(u => u.Accounts)
                    .ThenInclude(a => a.Cards)
                .Where(u => u.Role != "Admin");

            if (!string.IsNullOrEmpty(search))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.IdentityNumber != null && u.IdentityNumber.Contains(search)) ||
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search));
            }

            
            try
            {
                if (_context.AccountProducts.Any())
                    ViewBag.ProductList = _context.AccountProducts.ToList();
                else
                    ViewBag.ProductList = new List<AccountProduct>();
            }
            catch
            {
                ViewBag.ProductList = new List<AccountProduct>();
            }

            var users = usersQuery.OrderByDescending(u => u.CreatedAt).ToList();
            ViewBag.SearchQuery = search;
            return View(users);
        }

        [HttpGet]
        public IActionResult GetUserAccounts(int userId)
        {
            var accounts = _context.Accounts
                .Where(a => a.UserId == userId && a.AccountType == "Vadesiz" && a.Balance > 0)
                .Select(a => new { a.AccountId, a.AccountNumber, a.Balance, a.CurrencyCode, a.AccountName })
                .ToList();
            return Json(accounts);
        }


        [HttpPost]
        public IActionResult CreateUser(string fullName, string email, string password, string identityNumber)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Bu e-posta adresi zaten sistemde kayıtlı!";
                return RedirectToAction("Users");
            }

            var newUser = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = password, 
                IdentityNumber = identityNumber,
                Role = "Customer",           
                CreatedAt = DateTime.Now,
                Segment = "Standard"    
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            LogToSystem("Create", $"{fullName} isimli yeni müşteri manuel olarak eklendi.", "Info");
            TempData["Success"] = "Yeni müşteri başarıyla oluşturuldu.";

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(int userId, int productId, string accountType, decimal initialAmount, int termDays, int? sourceAccountId, bool closeSourceAccount)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            var product = _context.AccountProducts.Find(productId);
            if (product == null) return BadRequest("Geçersiz ürün.");

            if (accountType == "Vadeli" && initialAmount < 25000)
            {
                TempData["Error"] = "Vadeli hesap açılışı için minimum tutar 25.000 TL olmalıdır!";
                return RedirectToAction("Users");
            }

            decimal finalBalance = initialAmount;
            decimal finalInterestRate = 0;
            decimal exchangeRate = 1;
            string closeMessage = "";

            Account? sourceAccount = null;

            if (initialAmount > 0)
            {
                if (!sourceAccountId.HasValue)
                {
                    TempData["Error"] = "Lütfen para kaynağı olacak vadesiz hesabı seçiniz!";
                    return RedirectToAction("Users");
                }

                sourceAccount = _context.Accounts.FirstOrDefault(a => a.AccountId == sourceAccountId && a.UserId == userId);

                if (sourceAccount == null || sourceAccount.Balance < initialAmount)
                {
                    TempData["Error"] = "Kaynak hesapta yeterli bakiye yok!";
                    return RedirectToAction("Users");
                }

                sourceAccount.Balance -= initialAmount;

                if (sourceAccount.CurrencyCode != product.CurrencyCode)
                {
                    exchangeRate = await GetExchangeRateAsync(sourceAccount.CurrencyCode, product.CurrencyCode);
                    finalBalance = initialAmount * exchangeRate;
                }
                else
                {
                    finalBalance = initialAmount;
                }

                if (closeSourceAccount && sourceAccount.Balance == 0)
                {
                    sourceAccount.IsActive = false;
                    closeMessage = " ve kaynak hesap kapatıldı.";
                }
                else
                {
                    closeMessage = " ancak kaynak hesapta bakiye kaldığı için KAPATILMADI.";
                }
            }

            if (accountType == "Vadeli")
            {
                if (product.CurrencyCode == "TRY")
                {
                    var tier = _context.InterestTiers.FirstOrDefault(t =>
                        initialAmount >= t.MinAmount &&
                        initialAmount <= t.MaxAmount &&
                        termDays >= t.MinDays &&
                        termDays <= t.MaxDays);

                    finalInterestRate = tier != null ? tier.InterestRate : 0;
                }
                else
                {
                    finalInterestRate = 0.5m;
                }
            }

            Random rnd = new Random();
            string accNo = "";
            for (int i = 0; i < 16; i++) accNo += rnd.Next(0, 10).ToString();
            string ibanNo = $"TR{rnd.Next(10, 99)}560000{accNo}";

            var newAccount = new Account
            {
                UserId = userId,
                AccountName = product.ProductName,
                AccountNumber = accNo,
                Iban = ibanNo,
                CurrencyCode = product.CurrencyCode,
                AccountType = accountType,
                Balance = finalBalance,
                AccountOpenDate = DateTime.Now,
                IsActive = true,
                TermDays = accountType == "Vadeli" ? termDays : 0,
                InterestRate = accountType == "Vadeli" ? finalInterestRate : 0
            };

            _context.Accounts.Add(newAccount);
            _context.SaveChanges();

           
            string logMsg = $"{user.FullName} için {product.ProductName} açıldı. " +
                            $"Giriş: {initialAmount} {sourceAccount?.CurrencyCode ?? "-"} -> " +
                            $"Sonuç: {finalBalance:N2} {product.CurrencyCode} (Kur: {exchangeRate})";

            LogToSystem("Create", logMsg, "Info");

            TempData["Success"] = "Hesap başarıyla açıldı.";
            return RedirectToAction("Users");
        }
        private async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                if (toCurrency == "XAU" && fromCurrency == "TRY") return 1 / 3200m; 
                if (fromCurrency == "XAU" && toCurrency == "TRY") return 3200m;    

           
                string url = $"https://open.er-api.com/v6/latest/{fromCurrency}"; //Api for currency

                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetStringAsync(url);

                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;

                        if (root.GetProperty("result").GetString() == "success")
                        {
                            var rates = root.GetProperty("rates");

                            if (rates.TryGetProperty(toCurrency, out JsonElement rateElement))
                            {
                                return rateElement.GetDecimal();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API Hatası: " + ex.Message);
            }

            return 1; 
        }


        [HttpPost]
        public IActionResult UpdateUser(int userId, string fullName, string email, string identityNumber, string segment)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.FullName = fullName;
                user.Email = email;
                user.IdentityNumber = identityNumber;
                user.Segment = segment;
                _context.SaveChanges();
            }

            LogToSystem("Update", $"{user.FullName} (ID: {user.UserId}) Bilgileri Güncellendi!", "Info");

            return RedirectToAction("Users");
        }


        [HttpPost]
        public IActionResult CreateCard(int userId, decimal limit, string cardType)
        {
            var user = _context.Users.Include(u => u.Accounts).FirstOrDefault(u => u.UserId == userId);

            if (user == null) return NotFound();
            var mainAccount = user.Accounts.FirstOrDefault();
            if (mainAccount == null) return RedirectToAction("Users");

            Random rnd = new Random();
            string cardNo = "";
            bool isUnique = false;

            do
            {
                string prefix = (cardType == "Visa") ? "4" : "5";
                string suffix = "";
                for (int i = 0; i < 15; i++) suffix += rnd.Next(0, 10).ToString();
                cardNo = prefix + suffix;

                if (!_context.Cards.Any(c => c.CardNumber == cardNo)) isUnique = true;
            } while (!isUnique);

            var newCard = new Card
            {
                AccountId = mainAccount.AccountId,
                CardNumber = cardNo,
                CardType = cardType,
                ExpiryDate = DateTime.Now.AddYears(3).ToString("MM/yy"),
                Cvv = rnd.Next(100, 999).ToString(),
                CardLimit = limit,
                Debt = 0,
                Status = "Pending", 
                IsInternetEnabled = true
            };

            _context.Cards.Add(newCard);
            _context.SaveChanges();
            LogToSystem("Create", $"{user.FullName} adına {newCard.CardNumber} numaralı yeni kart başarıyla oluşturuldu.", "Info");

            return RedirectToAction("Users");
        }


        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users
                .Include(u => u.Accounts).ThenInclude(a => a.Cards)
                .FirstOrDefault(u => u.UserId == id);

            if (user != null)
            {
                foreach (var acc in user.Accounts)
                {
                    _context.Cards.RemoveRange(acc.Cards);
                }
                _context.Accounts.RemoveRange(user.Accounts);
                _context.Users.Remove(user);
                _context.SaveChanges();
            }

        

            LogToSystem("Delete", $"{user.FullName} (ID: {user.UserId}) sistemden silindi!", "Danger");

            return RedirectToAction("Users");
        }

        [HttpGet]
        public IActionResult GetChartData(string period)
        {
            var today = DateTime.Today;
            List<string> labels = new List<string>();
            List<decimal> incomeData = new List<decimal>();
            List<decimal> expenseData = new List<decimal>();

            if (period == "weekly")
            {
                for (int i = 6; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    labels.Add(date.ToString("dddd", new System.Globalization.CultureInfo("tr-TR")));

                    var inc = _context.Transactions
                        .Where(t => t.TransactionDate.Value.Date == date && (t.TransactionType == "Transfer" || t.TransactionType == "Deposit" || t.TransactionType == "Sell")) 
                        .Sum(t => (decimal?)t.Amount) ?? 0;

                    var exp = _context.Transactions
                        .Where(t => t.TransactionDate.Value.Date == date && (t.TransactionType == "Payment" || t.TransactionType == "Withdraw" || t.TransactionType == "Buy")) 
                        .Sum(t => (decimal?)t.Amount) ?? 0;

                    incomeData.Add(inc);
                    expenseData.Add(exp);
                }
            }
            else if (period == "monthly")
            {
                for (int i = 29; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    labels.Add(date.ToString("dd MMM", new System.Globalization.CultureInfo("tr-TR")));

                    var inc = _context.Transactions
                        .Where(t => t.TransactionDate.Value.Date == date && (t.TransactionType == "Transfer" || t.TransactionType == "Deposit" || t.TransactionType == "Sell"))
                        .Sum(t => (decimal?)t.Amount) ?? 0;

                    var exp = _context.Transactions
                        .Where(t => t.TransactionDate.Value.Date == date && (t.TransactionType == "Payment" || t.TransactionType == "Withdraw" || t.TransactionType == "Buy"))
                        .Sum(t => (decimal?)t.Amount) ?? 0;

                    incomeData.Add(inc);
                    expenseData.Add(exp);
                }
            }
            else if (period == "yearly")
            {
                for (int i = 11; i >= 0; i--)
                {
                    var date = today.AddMonths(-i);
                    labels.Add(date.ToString("MMMM", new System.Globalization.CultureInfo("tr-TR"))); 

                    var month = date.Month;
                    var year = date.Year;

                    var inc = _context.Transactions
                        .Where(t => t.TransactionDate.Value.Month == month && t.TransactionDate.Value.Year == year && (t.TransactionType == "Transfer" || t.TransactionType == "Deposit" || t.TransactionType == "Sell"))
                        .Sum(t => (decimal?)t.Amount) ?? 0;

                    var exp = _context.Transactions
                        .Where(t => t.TransactionDate.Value.Month == month && t.TransactionDate.Value.Year == year && (t.TransactionType == "Payment" || t.TransactionType == "Withdraw" || t.TransactionType == "Buy"))
                        .Sum(t => (decimal?)t.Amount) ?? 0;

                    incomeData.Add(inc);
                    expenseData.Add(exp);
                }
            }

            return Json(new { labels, income = incomeData, expense = expenseData });
        }
        public IActionResult Transactions(string search, string type)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");

            var query = _context.Transactions
                .Include(t => t.Account)
                .ThenInclude(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.Account.User.FullName.Contains(search) ||
                    t.SenderIBAN.Contains(search) ||
                    t.ReceiverIBAN.Contains(search) ||
                    t.ReceiverName.Contains(search) ||
                    t.ReferenceCode.Contains(search));
            }

            if (!string.IsNullOrEmpty(type) && type != "All")
            {
                query = query.Where(t => t.TransactionType == type);
            }

            var transactions = query.OrderByDescending(t => t.TransactionDate).ToList();

            ViewBag.SearchQuery = search;
            ViewBag.SelectedType = type;

            return View(transactions);
        }

        public IActionResult CardApprovals()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");

            var pendingCards = _context.Cards
                .Include(c => c.Account)
                .ThenInclude(a => a.User)
                .Where(c => c.Status == "Pending")
                .ToList();

            return View(pendingCards);
        }


        [HttpPost]
        public IActionResult ApproveCard(int cardId)
        {
            var card = _context.Cards.Include(c => c.Account).ThenInclude(a => a.User).FirstOrDefault(c => c.CardId == cardId);

            if (card != null)
            {
                card.Status = "Active";
                _context.SaveChanges();

                LogToSystem("Approve", $"{card.Account.User.FullName} isimli müşterinin {card.CardType} kartı onaylandı.", "Info");
            }
            return RedirectToAction("CardApprovals");
        }


        [HttpPost]
        public IActionResult RejectCard(int cardId)
        {
            var card = _context.Cards.Include(c => c.Account).ThenInclude(a => a.User).FirstOrDefault(c => c.CardId == cardId);

            if (card != null)
            {
                string userName = card.Account.User.FullName; 
                _context.Cards.Remove(card);
                _context.SaveChanges();

                LogToSystem("Reject", $"{userName} isimli müşterinin kart başvurusu reddedildi.", "Warning");
            }
            return RedirectToAction("CardApprovals");
        }

        public IActionResult SecurityLogs()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");

            var logs = _context.SystemLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToList();

            return View(logs);
        }

        private async void LogToSystem(string actionType, string description, string level)
        {

            try
            {
              
                int adminId = 1;
                string adminName = "Süper Admin";

                string ip = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                await _publishEndpoint.Publish(new SystemLogEvent
                {
                    UserId = adminId,
                    UserName = adminName,
                    ActionType = actionType,
                    Description = description,
                    IpAddress = ip,
                    LogLevel = level,
                    CreatedAt = DateTime.Now
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine("RabbitMQ Hatası: " + ex.Message);
            }
        }
     

        public IActionResult Settings()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Login");

            var settings = _context.SystemSettings.ToList();
            ViewBag.Settings = settings;
            var interestTiers = _context.InterestTiers.OrderBy(x => x.MinDays).ThenBy(x => x.MinAmount).ToList();

            return View(interestTiers);
        }


        [HttpPost]
        public IActionResult UpdateSettings(decimal interestRate, decimal transferLimit, decimal spread, bool maintenance, bool register, bool notify)
        {
            UpdateSettingValue("InterestRate", interestRate.ToString());
            UpdateSettingValue("DailyTransferLimit", transferLimit.ToString());
            UpdateSettingValue("ExchangeSpread", spread.ToString());

            UpdateSettingValue("MaintenanceMode", maintenance.ToString().ToLower());
            UpdateSettingValue("AllowNewRegister", register.ToString().ToLower());

            _context.SaveChanges();

            LogToSystem("Update", "Sistem ayarları güncellendi.", "Warning");

            TempData["Message"] = "Ayarlar başarıyla kaydedildi.";

            return RedirectToAction("Settings");
        }

        private void UpdateSettingValue(string key, string newValue)
        {
            var setting = _context.SystemSettings.FirstOrDefault(s => s.SettingKey == key);
            if (setting != null)
            {
                setting.SettingValue = newValue;
            }
        }


        [HttpPost]
        public IActionResult UpdateInterestTier(int id, string rate) 
        {

            if (string.IsNullOrEmpty(rate)) return Json(new { success = false });
            string normalizedRate = rate.Replace(",", ".");

            if (decimal.TryParse(normalizedRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedRate))
            {
                var tier = _context.InterestTiers.Find(id);
                if (tier != null)
                {
                    tier.InterestRate = parsedRate;
                    _context.SaveChanges();
                    return Json(new { success = true });
                }
            }

            return Json(new { success = false });
        }
    }
}