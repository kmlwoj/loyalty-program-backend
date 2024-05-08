using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.MainContext;

public partial class Transaction
{
    public int TransId { get; set; }

    public string Login { get; set; } = null!;

    public DateTime TransDate { get; set; }

    public int CodeId { get; set; }

    public int OfferId { get; set; }

    public int Price { get; set; }

    public string Shop { get; set; } = null!;

    public virtual Code Code { get; set; } = null!;

    public virtual User LoginNavigation { get; set; } = null!;

    public virtual Organization ShopNavigation { get; set; } = null!;
}
