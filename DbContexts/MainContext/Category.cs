using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.MainContext;

public partial class Category
{
    public string Name { get; set; } = null!;

    public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
