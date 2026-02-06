using System.ComponentModel.DataAnnotations;

namespace FibaPlus_Bank.Models
{
    public class PaymentType
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}