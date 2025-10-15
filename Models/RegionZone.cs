using System;
using System.Collections.Generic;

namespace MicrDbChequeProcessingSystem.Models;

public partial class RegionZone
{
    public long RegionId { get; set; }

    public string RegionName { get; set; } = null!;

    public long? CreatedByUserId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Bank> Banks { get; set; } = new List<Bank>();

    public virtual UserProfile? CreatedByUser { get; set; }
}
