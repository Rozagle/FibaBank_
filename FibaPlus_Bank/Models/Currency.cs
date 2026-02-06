using System.ComponentModel.DataAnnotations;

namespace FibaPlus_Bank.Models
{
    public class Currency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(3)]
        public string Code { get; set; }  

        public string Name { get; set; }  

        public string Symbol { get; set; }
    }
}