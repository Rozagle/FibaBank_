using System.ComponentModel.DataAnnotations;

namespace FibaPlus_Bank.Models
{
    public class InterestTier
    {
        [Key]
        public int Id { get; set; }
        public int MinDays { get; set; }      
        public int MaxDays { get; set; }      
        public decimal MinAmount { get; set; } 
        public decimal MaxAmount { get; set; } 
        public decimal InterestRate { get; set; } 
    }
}