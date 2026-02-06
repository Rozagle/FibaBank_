using System.ComponentModel.DataAnnotations;

namespace FibaPlus_Bank.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public string Description { get; set; }
        public string GroupName { get; set; }
    }
}