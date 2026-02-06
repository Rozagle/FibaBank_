using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Http; 
using Microsoft.AspNetCore.Mvc;

namespace FibaPlus_Bank.Controllers
{
    public class LoginController : Controller
    {
        private readonly FibraPlusBankDbContext _context;

        public LoginController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                var role = HttpContext.Session.GetString("UserRole");
                if (role == "Admin") return RedirectToAction("Index", "Admin");
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Index(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email && x.PasswordHash == password);

            if (user != null)
            {
                var maintenanceSetting = _context.SystemSettings.FirstOrDefault(s => s.SettingKey == "MaintenanceMode");
                bool isMaintenanceActive = maintenanceSetting != null && maintenanceSetting.SettingValue == "true";

                if (isMaintenanceActive && user.Role != "Admin")
                {
                    ViewBag.Error = "⚠️ Sistem şu an bakım çalışması nedeniyle geçici olarak hizmet dışıdır. Lütfen daha sonra tekrar deneyiniz.";
                    return View();
                }


                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.FullName);

                string role = string.IsNullOrEmpty(user.Role) ? "User" : user.Role;
                HttpContext.Session.SetString("UserRole", role);

                if (role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Home"); 
                }
            }
            else
            {
                ViewBag.Error = "E-posta veya şifre hatalı!";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            
            var registerSetting = _context.SystemSettings.FirstOrDefault(s => s.SettingKey == "AllowNewRegister");
            bool isRegisterAllowed = registerSetting == null || registerSetting.SettingValue == "true";

            if (!isRegisterAllowed)
            {
                TempData["Error"] = "🚫 Şu anda yeni müşteri kabulü yapılmamaktadır.";
                return RedirectToAction("Index");
            }

            return View();
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SendVerificationCode(string email)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Bu e-posta adresiyle kayıtlı müşteri bulunamadı.";
                return RedirectToAction("ForgotPassword");
            }

            Random rnd = new Random();
            string code = rnd.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("ResetCode", code);
            HttpContext.Session.SetString("ResetEmail", email);

            TempData["Info"] = "Doğrulama Kodunuz (Test): " + code;

            return RedirectToAction("VerifyCode");
        }

        [HttpGet]
        public IActionResult VerifyCode()
        {
            if (HttpContext.Session.GetString("ResetCode") == null) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public IActionResult VerifyCode(string code)
        {
            string sessionCode = HttpContext.Session.GetString("ResetCode");

            if (code == sessionCode)
            {
                return RedirectToAction("ResetPassword");
            }
            else
            {
                TempData["Error"] = "Girdiğiniz kod hatalı!";
                return View();
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString("ResetCode") == null) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Şifreler uyuşmuyor.";
                return View();
            }

            string email = HttpContext.Session.GetString("ResetEmail");
            var user = _context.Users.FirstOrDefault(x => x.Email == email);

            if (user != null)
            {
                user.PasswordHash = newPassword;
                _context.SaveChanges();

                HttpContext.Session.Remove("ResetCode");
                HttpContext.Session.Remove("ResetEmail");

                TempData["Success"] = "Şifreniz başarıyla değiştirildi. Giriş yapabilirsiniz.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}