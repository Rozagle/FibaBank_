using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace FibaPlus_Bank.Models;

public partial class Card
{
    public int CardId { get; set; }
    public int AccountId { get; set; }
    public string? CardNumber { get; set; }
    public string? CardType { get; set; }
    public string? ExpiryDate { get; set; }
    public string? Cvv { get; set; }
    public decimal? CardLimit { get; set; }
    public decimal? Debt { get; set; }
    public bool? IsInternetEnabled { get; set; }

    public string Status { get; set; } = "Active";
    public virtual Account Account { get; set; } = null!;


    [NotMapped]
    public string HolderName
    {
        get
        {
            return Account?.User?.FullName ?? "FİBRABANK MÜŞTERİSİ";
        }
    }

    [NotMapped]
    public decimal AvailableLimit => (CardLimit ?? 0) - (Debt ?? 0);

    [NotMapped]
    public int LimitUsagePercent
    {
        get
        {
            if (CardLimit == null || CardLimit == 0) return 0;
            return (int)((Debt / CardLimit) * 100);
        }
    }

    [NotMapped]
    public bool IsInternetEnabledSafe => IsInternetEnabled ?? false;
}