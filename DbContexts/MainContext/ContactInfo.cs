using System;
using System.Collections.Generic;

namespace lojalBackend.DbContexts.MainContext;

public partial class ContactInfo
{
    public string? Name { get; set; }

    public string? Position { get; set; }

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;
}
