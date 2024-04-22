using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.ShopContext;

public partial class Organization
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
