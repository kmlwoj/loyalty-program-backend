using System;
using System.Collections.Generic;

namespace lojalBackend;

public partial class RefreshToken
{
    public string Login { get; set; } = null!;

    public string? Token { get; set; }

    public DateTime? Expiry { get; set; }
}
