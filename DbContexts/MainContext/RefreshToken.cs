using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.MainContext;

public partial class RefreshToken
{
    public string Login { get; set; } = null!;

    public string? Token { get; set; }

    public DateTime? Expiry { get; set; }

    public virtual User LoginNavigation { get; set; } = null!;
}
