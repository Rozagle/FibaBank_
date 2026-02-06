using System;
using System.Collections.Generic;

namespace FibaPlus_Bank.Models;

public partial class User
{
    public string Segment { get; set; } = "Standard";  
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? IdentityNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Investment> Investments { get; set; } = new List<Investment>();
}
