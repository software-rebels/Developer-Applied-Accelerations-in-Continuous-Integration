using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.SqlModels
{
    [Index(nameof(VcsUrl))]
    [Index(nameof(BuildUrl), IsUnique = true)]
    [Index(nameof(VcsUrl), nameof(JobName))]
    [Index(nameof(VcsUrl), nameof(SelectedSuccess))]
    public class BuildEntrySlim
    {
        public int Id { get; set; }
        public string VcsUrl { get; set; } = null!;
        public string BuildUrl { get; set; } = null!;
        public DateTimeOffset StartTime { get; set; }
        public string Status { get; set; } = null!;
        public int NumberOfSteps { get; set; }
        public TimeSpan SumOfBuildTimeInSteps { get; set; }
        public string JobName { get; set; } = null!;
        public string? WorkflowName { get; set; }
        public int Parallel { get; set; }
        public bool SelectedSuccess { get; set; }
        public string? Branch { get; set; }
        public string VcsRevision { get; set; } = null!;
        public string Why { get; set; } = null!;

        // Additional properties.
        [Column(TypeName = "jsonb")]
        public IDictionary<string, object> AttachedProperties { get; set; } = null!;

        //[MaxLength(32)]
        public byte[] CircleYmlHash { get; set; } = null!;
        //public CircleYml CircleYml { get; set; } = null!;
    }
}
