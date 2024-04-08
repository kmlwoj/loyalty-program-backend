using System;
using System.Collections.Generic;

namespace lojalBackend;

public partial class Offer
{
    public int OfferId { get; set; }

    public string Name { get; set; } = null!;

    public int Price { get; set; }

    public string Organization { get; set; } = null!;

    public string? Category { get; set; }

    public virtual Category? CategoryNavigation { get; set; }

    public virtual ICollection<Code> Codes { get; set; } = new List<Code>();

    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();

    public virtual Organization OrganizationNavigation { get; set; } = null!;
}
