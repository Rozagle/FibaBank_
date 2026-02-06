namespace FibaPlus_Bank.Events
{
    public class SystemLogEvent
    {
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string ActionType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string LogLevel { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}