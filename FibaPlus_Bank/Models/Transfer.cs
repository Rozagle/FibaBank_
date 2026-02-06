namespace FibaPlus_Bank.Models
{
    public class Transfer
    {
        public int SenderAccountId { get; set; }
        public string ReceiverIban { get; set; }
        public string ReceiverName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public int PaymentTypeId { get; set; }
        public string PaymentType { get; set; }

    }
}