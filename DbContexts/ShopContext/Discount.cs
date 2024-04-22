using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.ShopContext;

public partial class Discount
{
    public int DiscId { get; set; }

    public int OfferId { get; set; }

    public string? Name { get; set; }

    public string Reduction { get; set; } = null!;

    public DateTime Expiry { get; set; }

    public virtual Offer Offer { get; set; } = null!;
}
