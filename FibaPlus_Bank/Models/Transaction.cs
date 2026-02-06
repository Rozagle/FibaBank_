using System;
using System.Collections.Generic;

namespace FibaPlus_Bank.Models
{
    public partial class Transaction
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public string? TransactionType { get; set; } 
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? CategoryIcon { get; set; }
        public string? SenderIBAN { get; set; }      
        public string? ReceiverIBAN { get; set; }   
        public string? ReceiverName { get; set; }    
        public string? TransactionStatus { get; set; } 
        public string? ReferenceCode { get; set; }   
        public int? PaymentTypeId { get; set; }
        public virtual PaymentType PaymentType { get; set; }
        public virtual Account Account { get; set; } = null!;
    }
}