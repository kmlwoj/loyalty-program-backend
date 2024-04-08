using System;
using System.Collections.Generic;

namespace lojalBackend;

public partial class ContactRequest
{
    public int ContReqId { get; set; }

    public DateTime ContReqDate { get; set; }

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;
}
