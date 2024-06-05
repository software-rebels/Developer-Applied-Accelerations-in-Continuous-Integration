using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ForecastBuildTime.SqlModels;

[Index(nameof(VcsUrl), nameof(JobName), IsUnique = true)]
public class AccelerationSample
{
    public int Id { get; set; }

    public string VcsUrl { get; set; } = default!;

    public string JobName { get; set; } = default!;

    public int BuildCount { get; set; } = default!;

    public double MmreAverage { get; set; }

    public double MmreLinearRegression { get; set; }

    public double MmreSlidingWindow { get; set; }

    public ShapiroWilk? ShapiroWilk { get; set; }

    public List<KMeansClusters> ClusterCenters { get; set; } = default!;

    /// <summary>
    /// Clusters.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public List<KMeansClusters>? ClusterCentersLog { get; set; }

    private Dictionary<string, bool>? _hitManualRules;

    [Column(TypeName = "jsonb")]
    public Dictionary<string, bool> HitManualRules
    {
        get => _hitManualRules ??= new Dictionary<string, bool>();
        set => _hitManualRules = value;
    }
}

[Owned]
public class ShapiroWilk
{
    public double W { get; set; }

    public double P { get; set; }
}

[Index(nameof(VcsUrl), nameof(JobName), nameof(Month), IsUnique = true)]
public class KMeansClusters
{
    public int Id { get; set; }

    public string VcsUrl { get; set; } = default!;

    public string JobName { get; set; } = default!;

    public string Month { get; set; } = default!;

    public double Higher { get; set; }

    public double Lower { get; set; }

    public AccelerationSample AccelerationSample { get; set; } = default!;
}
