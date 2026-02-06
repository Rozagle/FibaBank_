using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

//Buraya artık gerek yok ama silmekte kararsızım kod çalışıyorsa boşver 
namespace FibaPlus_Bank.Controllers
{
    public class InvestmentTradeController : Controller
    {
        private readonly FibraPlusBankDbContext _context;

        public InvestmentTradeController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Trade(InvestmentTrade model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var account = _context.Accounts.FirstOrDefault(x => x.UserId == userId && x.CurrencyCode == "TRY");

            if (account == null)
            {
                account = _context.Accounts.FirstOrDefault(x => x.UserId == userId);
            }

            if (account == null)
            {
                TempData["Error"] = "İşlem yapacak uygun hesap bulunamadı.";
                return RedirectToAction("Index");
            }

            try
            {
                var pUserId = new SqlParameter("@UserId", userId);
                var pAccountId = new SqlParameter("@AccountId", account.AccountId);
                var pSymbol = new SqlParameter("@Symbol", model.Symbol ?? "");
                var pQty = new SqlParameter("@Quantity", model.Quantity);
                var pPrice = new SqlParameter("@UnitPrice", model.UnitPrice);

                if (model.TransactionType == "BUY")
                {
               
                    var pInvType = new SqlParameter("@InvestmentType", model.InvestmentType ?? "STOCK");
                    var pInstType = new SqlParameter("@InstrumentType", model.InstrumentType ?? "EQUITY");
                    var pName = new SqlParameter("@InstrumentName", model.InstrumentName ?? model.Symbol);

                    _context.Database.ExecuteSqlRaw(
                        "EXEC sp_BuyInvestment @UserId, @AccountId, @Symbol, @InvestmentType, @InstrumentType, @InstrumentName, @Quantity, @UnitPrice",
                        pUserId, pAccountId, pSymbol, pInvType, pInstType, pName, pQty, pPrice);

                    TempData["Success"] = $"{model.Symbol} alım işlemi başarılı!";
                }
                else if (model.TransactionType == "SELL")
                {
                
                    _context.Database.ExecuteSqlRaw(
                        "EXEC sp_SellInvestment @UserId, @AccountId, @Symbol, @Quantity, @UnitPrice",
                        pUserId, pAccountId, pSymbol, pQty, pPrice);

                    TempData["Success"] = $"{model.Symbol} satış işlemi başarılı!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "İşlem Başarısız: " + ex.Message;
            }

            return RedirectToAction("Index", "Investments");
        }
    }
}