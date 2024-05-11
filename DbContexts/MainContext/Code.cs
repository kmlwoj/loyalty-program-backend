using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.MainContext;

public partial class Code
{
    public int CodeId { get; set; }

    public int OfferId { get; set; }

    public DateTime Expiry { get; set; }

    public int? DiscId { get; set; }

    public virtual Discount? Disc { get; set; }

    public virtual Offer Offer { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
