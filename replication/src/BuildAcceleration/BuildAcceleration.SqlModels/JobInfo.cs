using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.SqlModels;

[Index(nameof(VcsUrl), nameof(JobName), IsUnique = true)]
public class JobInfo
{
    public int Id { get; set; }
    public string VcsUrl { get; set; } = default!;
    public string JobName { get; set; } = default!;
    public int MaxNonZeroBuildCountPerMonth { get; set; }
    public int NonZeroBuildCount { get; set; }
    public bool MeetMinimumSampleSize { get; set; } //50
    public bool SampledToInspect { get; set; }
    public ulong Random { get; set; }
    public bool RandomValueCreated { get; set; }
}