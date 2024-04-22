using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.ShopContext;

public partial class Code
{
    public int CodeId { get; set; }

    public int OfferId { get; set; }

    public ulong? State { get; set; }

    public DateTime Expiry { get; set; }

    public virtual Offer Offer { get; set; } = null!;
}
