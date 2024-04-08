using System;
using System.Collections.Generic;

namespace lojalBackend;

public partial class User
{
    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Email { get; set; }

    public string Type { get; set; } = null!;

    public string Organization { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public int? Credits { get; set; }

    public DateTime? LatestUpdate { get; set; }

    public virtual Organization OrganizationNavigation { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
