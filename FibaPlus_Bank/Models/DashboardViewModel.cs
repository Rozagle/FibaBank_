
using System.Collections.Generic;

namespace FibaPlus_Bank.Models
{
    public class DashboardViewModel
    {
        public User User { get; set; }            
        public List<Account> Accounts { get; set; } 
        public List<Card> Cards { get; set; }      
        public List<Transaction> RecentTransactions { get; set; } 
    }
}