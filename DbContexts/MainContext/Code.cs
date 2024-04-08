using System;
using System.Collections.Generic;

namespace lojalBackend;

public partial class Code
{
    public int CodeId { get; set; }

    public int OfferId { get; set; }

    public DateTime Expiry { get; set; }

    public virtual Offer Offer { get; set; } = null!;

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
