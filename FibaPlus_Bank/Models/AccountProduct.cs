using System.ComponentModel.DataAnnotations;

namespace FibaPlus_Bank.Models;

public class AccountProduct
{
    [Key] 
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string CurrencyCode { get; set; }
    public string Category { get; set; }
}