using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.SqlModels
{
    [Index(nameof(VcsUrl), nameof(JobName), nameof(Approach), IsUnique = true)]
    public class PredictionEvaluation
    {
        public int Id { get; set; }
        public string VcsUrl { get; set; } = null!;
        public string JobName { get; set; } = null!;
        public string Approach { get; set; } = null!;
        public int BuildCount { get; set; }
        public double MMRE { get; set; }
        public double RSquared { get; set; }
        public JsonDocument? PredictedValues { get; set; }
        //public ICollection<BuildEntrySlim> Builds { get; set; } = null!;
    }
}
