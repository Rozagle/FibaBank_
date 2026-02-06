using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FibaPlus_Bank.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly FibraPlusBankDbContext _context;

        public TransactionsController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? accountId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var query = _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId
                            && t.ReceiverName != null  
                            && t.Amount != null);      

            if (accountId != null)
            {
                query = query.Where(t => t.AccountId == accountId);
            }

            var history = query.OrderByDescending(t => t.TransactionDate).ToList();

            return View(history);
        }

    }
}