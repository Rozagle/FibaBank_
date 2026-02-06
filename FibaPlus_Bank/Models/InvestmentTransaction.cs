using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FibaPlus_Bank.Models
{
    public class InvestmentTransaction
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int AccountId { get; set; }

        [Required]
        [StringLength(20)]
        public string InstrumentCode { get; set; } = null!;

        public string? InvestmentType { get; set; } 

        public string? TransactionType { get; set; } 

        [Column(TypeName = "decimal(18, 4)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
    }
}