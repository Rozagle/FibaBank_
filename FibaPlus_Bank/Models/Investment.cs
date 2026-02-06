using FibaPlus_Bank.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FibaPlus_Bank.Models
{
    public class Investment
    {
        [Key]
        public int InvestmentId { get; set; }

        public int UserId { get; set; }

        public int AccountId { get; set; }

        public string? InvestmentType { get; set; }

        public string? InstrumentType { get; set; } 

        [Required]
        [StringLength(20)]
        public string InstrumentCode { get; set; } = null!; 

        [Required]
        [StringLength(150)]
        public string InstrumentName { get; set; } = null!;

        [Column(TypeName = "decimal(18, 4)")]
        public decimal Quantity { get; set; }


        [Column(TypeName = "decimal(18, 4)")]
        public decimal PurchasePrice { get; set; } 


        [Column(TypeName = "decimal(18, 4)")]
        public decimal CurrentPrice { get; set; } 

        public string? CurrencyCode { get; set; } = "TRY";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdateDate { get; set; } = DateTime.Now;


        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}