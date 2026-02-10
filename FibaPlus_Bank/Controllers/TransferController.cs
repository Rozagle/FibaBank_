using FibaPlus_Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FibaPlus_Bank.Controllers
{
    public class TransferController : Controller
    {
        private readonly FibraPlusBankDbContext _context;
        private readonly string[] _restrictedCodes = { "XAU", "GAU", "GOLD", "XAG", "GUM", "SILVER", "XPT", "PLT", "PLATINUM" };

        public TransferController(FibraPlusBankDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(int? selectAccountId, string prefillCard)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var activeCardAccountIds = _context.Cards
                .Where(c => c.Account.UserId == userId && c.Status == "Active")
                .Select(c => c.AccountId)
                .ToList();

            var allAccounts = _context.Accounts
                .Where(x => x.UserId == userId && !_restrictedCodes.Contains(x.CurrencyCode))
                .Where(x => x.AccountType != "Kredi" || activeCardAccountIds.Contains(x.AccountId))
                .ToList();

            var activeCards = _context.Cards
                .Where(c => c.Account.UserId == userId && c.Status == "Active")
                .ToList();

            var filteredAccounts = allAccounts.Where(acc =>
            {
                if (acc.AccountType == "Kredi")
                {
                    var card = activeCards.FirstOrDefault(c => c.AccountId == acc.AccountId);
                    if (card == null) return false;

                    decimal availableLimit = (card.CardLimit ?? 0) - (card.Debt ?? 0);
                    return availableLimit > 0;
                }
                else
                {
                    return acc.Balance > 0;
                }
            }).ToList();

            ViewBag.Cards = activeCards;
            ViewBag.Accounts = filteredAccounts;
            ViewBag.PaymentTypes = _context.PaymentTypes.ToList();
            ViewBag.SelectedAccountId = selectAccountId;

            if (!string.IsNullOrEmpty(prefillCard))
            {
                string cleanNumber = prefillCard.Replace(" ", "").Trim();
                var linkedCard = _context.Cards
                    .FirstOrDefault(c => c.CardNumber == cleanNumber && c.Account.UserId == userId);

                if (linkedCard != null && linkedCard.Status == "Active")
                {
                    ViewBag.SelectedAccountId = linkedCard.AccountId;
                }
            }

            var recentTransfers = _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account.UserId == userId && (t.TransactionType == "Transfer" || t.TransactionType == "İade" || t.TransactionType == "Gelen Transfer")
                            && t.ReceiverName != null && t.Amount != null)
                .OrderByDescending(t => t.TransactionDate).Take(10).ToList();

            return View(recentTransfers);
        }

        [HttpPost]
        public IActionResult SendMoney(Transfer model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Login");

            var senderAccount = _context.Accounts.FirstOrDefault(x => x.AccountId == model.SenderAccountId && x.UserId == userId);

            if (senderAccount == null)
            {
                TempData["Error"] = "Gönderen hesap bulunamadı.";
                return RedirectToAction("Index");
            }

            if (_restrictedCodes.Contains(senderAccount.CurrencyCode))
            {
                TempData["Error"] = "Kıymetli maden hesaplarından transfer yapılamaz.";
                return RedirectToAction("Index");
            }

            var receiverAccount = _context.Accounts.Include(u => u.User).FirstOrDefault(x => x.Iban == model.ReceiverIban);

            if (receiverAccount != null) 
            {
                if (senderAccount.CurrencyCode != receiverAccount.CurrencyCode)
                {
                    TempData["Error"] = $"Farklı para birimleri arasında transfer yapılamaz. ({senderAccount.CurrencyCode} -> {receiverAccount.CurrencyCode})";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                if (senderAccount.CurrencyCode != "TRY")
                {
                    TempData["Error"] = "Dış bankalara sadece TRY transferi yapılabilir.";
                    return RedirectToAction("Index");
                }
            }

            decimal fee = (senderAccount.CurrencyCode == "TRY") ? 13.86m : 25.00m;
            decimal totalDeduction = model.Amount + fee;

            Card senderCard = null;
            if (senderAccount.AccountType == "Kredi")
            {
                senderCard = _context.Cards.FirstOrDefault(c => c.AccountId == senderAccount.AccountId);

                if (senderCard != null)
                {
                    decimal currentDebt = senderCard.Debt ?? 0;
                    decimal limit = senderCard.CardLimit ?? 0;

                    if ((currentDebt + totalDeduction) > limit)
                    {
                        TempData["Error"] = "Kart limiti yetersiz.";
                        return RedirectToAction("Index");
                    }
                }
            }
            else
            {
                if (senderAccount.Balance < totalDeduction)
                {
                    TempData["Error"] = "Yetersiz bakiye.";
                    return RedirectToAction("Index");
                }
            }

            if (senderAccount.AccountType == "Kredi" && senderCard != null)
            {
                senderCard.Debt = (senderCard.Debt ?? 0) + totalDeduction;
                senderAccount.Balance -= totalDeduction; 

                _context.Cards.Update(senderCard);
            }
            else
            {
                senderAccount.Balance -= totalDeduction;
            }

            var outgoingTransaction = new Transaction
            {
                AccountId = senderAccount.AccountId,
                TransactionType = "Transfer",
                Amount = model.Amount,
                Description = $"Transfer: {model.ReceiverName}",
                SenderIBAN = senderAccount.Iban,
                ReceiverIBAN = model.ReceiverIban,
                ReceiverName = model.ReceiverName,
                TransactionDate = DateTime.Now,
                TransactionStatus = "Success",
                CategoryIcon = "fa-solid fa-paper-plane",
                ReferenceCode = "#TR" + new Random().Next(100000, 999999),
                PaymentTypeId = model.PaymentTypeId
            };

            _context.Transactions.Add(outgoingTransaction);

            bool isValid = false;
            string rejectReason = "";

            if (senderAccount.Iban == model.ReceiverIban)
            {
                isValid = false; rejectReason = "Kendi hesabına transfer yapılamaz.";
            }
            else
            {
                if (receiverAccount != null) 
                {
                    string inputName = model.ReceiverName.Replace(" ", "").ToLower(new CultureInfo("tr-TR"));
                    string realName = receiverAccount.User.FullName.Replace(" ", "").ToLower(new CultureInfo("tr-TR"));

                    if (realName.Contains(inputName) || inputName.Contains(realName))
                    {
                        if (receiverAccount.AccountType == "Kredi")
                        {
                            var receiverCard = _context.Cards.FirstOrDefault(c => c.AccountId == receiverAccount.AccountId);
                            if (receiverCard != null)
                            {
                                receiverCard.Debt = (receiverCard.Debt ?? 0) - model.Amount;

                                if (receiverCard.Debt < 0) receiverCard.Debt = 0;

                                _context.Cards.Update(receiverCard);
                            }
                            receiverAccount.Balance += model.Amount;
                        }
                        else
                        {
                            receiverAccount.Balance += model.Amount;
                        }

                        isValid = true;

                        _context.Transactions.Add(new Transaction
                        {
                            AccountId = receiverAccount.AccountId,
                            TransactionType = "Gelen Transfer",
                            Amount = model.Amount,
                            SenderIBAN = senderAccount.Iban,
                            ReceiverIBAN = receiverAccount.Iban,
                            ReceiverName = receiverAccount.User.FullName,
                            TransactionDate = DateTime.Now,
                            TransactionStatus = "Success",
                            CategoryIcon = "fa-solid fa-arrow-down"
                        });
                    }
                    else
                    {
                        isValid = false; rejectReason = "Alıcı Adı/Soyadı uyuşmuyor.";
                    }
                }
                else isValid = true; 
            }

            if (isValid)
            {
                _context.SaveChanges();
                TempData["Success"] = "Transfer gerçekleşti.";
            }
            else
            {
                outgoingTransaction.TransactionStatus = "Rejected";

                if (senderAccount.AccountType == "Kredi" && senderCard != null)
                {
                    senderCard.Debt -= totalDeduction;
                    senderAccount.Balance += totalDeduction;
                    _context.Cards.Update(senderCard);
                }
                else
                {
                    senderAccount.Balance += totalDeduction;
                }

                var refund = new Transaction
                {
                    AccountId = senderAccount.AccountId,
                    TransactionType = "İade",
                    Amount = totalDeduction,
                    Description = $"İADE: {rejectReason}",
                    SenderIBAN = "SİSTEM",
                    ReceiverIBAN = senderAccount.Iban,
                    ReceiverName = senderAccount.AccountName,
                    TransactionDate = DateTime.Now.AddSeconds(2),
                    TransactionStatus = "Refunded",
                    CategoryIcon = "fa-solid fa-rotate-left",
                    ReferenceCode = "#REF" + new Random().Next(100000, 999999)
                };
                _context.Transactions.Add(refund);
                _context.SaveChanges();
                TempData["Error"] = $"İşlem Reddedildi: {rejectReason}. Tutar iade edildi.";
            }

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult CheckIban(string iban, int? senderAccountId)
        {
            if (string.IsNullOrEmpty(iban)) return Json(new { success = false });
            string cleanReceiverIban = iban.Replace(" ", "").Trim();

            if (senderAccountId != null)
            {
                var senderAccount = _context.Accounts.FirstOrDefault(x => x.AccountId == senderAccountId);
                if (senderAccount != null && senderAccount.Iban.Replace(" ", "").Trim() == cleanReceiverIban)
                    return Json(new { success = false, isSameAccount = true, message = "Kendi hesabınıza para gönderemezsiniz." });
            }

            var institution = _context.Institutions.FirstOrDefault(x => x.Iban == cleanReceiverIban);
            if (institution != null) return Json(new { success = true, name = institution.Name, logo = institution.LogoClass, type = "institution", isLocked = true });

            var userAccount = _context.Accounts.Include(u => u.User).FirstOrDefault(x => x.Iban == cleanReceiverIban);
            if (userAccount != null) return Json(new { success = true, name = userAccount.User.FullName, logo = "fa-solid fa-user", type = "user", isLocked = true });

            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult BreakTerm(int accountId, string receiverIban) 
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Oturum kapalı." });

            var account = _context.Accounts.FirstOrDefault(x => x.AccountId == accountId && x.UserId == userId);
            if (account == null) return Json(new { success = false, message = "Gönderen hesap bulunamadı." });

            if (account.AccountType == "Vadesiz") return Json(new { success = true, message = "Hesap zaten vadesiz." });

            string cleanReceiverIban = receiverIban?.Replace(" ", "").Trim();
            var receiverAccount = _context.Accounts.FirstOrDefault(x => x.Iban == cleanReceiverIban);

            if (receiverAccount != null)
            {
                if (account.CurrencyCode != receiverAccount.CurrencyCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"HATA: Farklı para birimleri arasında transfer yapılamaz! ({account.CurrencyCode} -> {receiverAccount.CurrencyCode})"
                    });
                }
            }
            else
            {
                if (account.CurrencyCode != "TRY")
                {
                    return Json(new { success = false, message = "Dış bankalara sadece TRY transferi yapılabilir." });
                }
            }

            if (account.AccountType == "Vadeli")
            {
                try
                {
                    account.AccountType = "Vadesiz";
                    account.InterestRate = 0;
                    account.AccruedInterest = 0;
                    account.TermDays = 0;
                    account.AccountOpenDate = DateTime.Now;
                    _context.SaveChanges();
                    return Json(new { success = true, message = "Vade bozuldu." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Hata: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "İşlem yapılamadı." });
        }
    }
}