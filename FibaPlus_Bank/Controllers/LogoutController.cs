using Microsoft.AspNetCore.Mvc;

namespace FibaPlus_Bank.Controllers
{
    public class LogoutController : Controller
    {
        public IActionResult Index()
        {        
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Login");
        }
    }
}