using System;

namespace FibaPlus_Bank.Models
{
    public class Institution
    {
        public int Id { get; set; }
        public string Name { get; set; }      
        public string Iban { get; set; }     
        public string LogoClass { get; set; } 
    }
}