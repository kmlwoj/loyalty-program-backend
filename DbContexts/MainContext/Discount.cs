using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.MainContext;

public partial class Discount
{
    public int DiscId { get; set; }

    public int OfferId { get; set; }

    public string? Name { get; set; }

    public string Reduction { get; set; } = null!;

    public virtual ICollection<Code> Codes { get; set; } = new List<Code>();

    public virtual Offer Offer { get; set; } = null!;
}
