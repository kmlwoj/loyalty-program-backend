using System;
using System.Collections.Generic;

namespace lojalBackend;

public partial class Organization
{
    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
