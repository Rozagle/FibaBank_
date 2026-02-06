using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FibaPlus_Bank.Models
{
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string AccountName { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string Iban { get; set; } = null!;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Balance { get; set; }

        [Required]
        [StringLength(5)]
        public string CurrencyCode { get; set; } = null!;

        [StringLength(20)]
        public string AccountType { get; set; } = "Vadesiz";

        [Column(TypeName = "decimal(18, 4)")]
        public decimal? InterestRate { get; set; }

        public int? TermDays { get; set; }

        public DateTime? AccountOpenDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal AccruedInterest { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}