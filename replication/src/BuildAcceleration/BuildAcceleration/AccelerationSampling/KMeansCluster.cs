using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.Helpers;
using ForecastBuildTime.SqlModels;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace ForecastBuildTime.AccelerationSampling;

/// <summary>
/// This class performs classification.
/// </summary>
[Command("cluster")]
internal class KMeansCluster
{
    private readonly ForecastingContext _forecastingContext;
    private readonly ILogger<KMeansCluster> _logger;
    private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
    private readonly DirectoryHelper _directoryHelper;

    public KMeansCluster(
        ForecastingContext forecastingContext, ILogger<KMeansCluster> logger,
        IDbContextFactory<ForecastingContext> dbContextFactory, DirectoryHelper directoryHelper)
    {
        _forecastingContext = forecastingContext;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _directoryHelper = directoryHelper;
    }

    [Option("--top")]
    public decimal ExcludeTopProportion { get; set; } = 0.05m;
    [Option("--bottom")]
    public decimal ExcludeBottomProportion { get; set; } = 0.01m;
    [Option]
    public bool All { get; set; }

    public async Task OnExecuteAsync()
    {
        if (!All)
        {
            await CalculateCluster();
        }
        else
        {
            await CalculateAllJobsClusters();
        }
    }

    private async ValueTask CalculateAllJobsClusters()
    {
        var jobCount = await _forecastingContext.JobInfos.CountAsync();
        var jobs = await _forecastingContext.JobInfos.AsNoTracking().OrderByDescending(j => j.NonZeroBuildCount).ThenByDescending(j => j.Random).Take(jobCount / 10).ToListAsync();

        var ratioList = MakeListOfInstanceType(new
        {
            VcsUrl = default(string)!,
            JobName = default(string)!,
            WeightedMiddle = default(double),
            AverageRatio = default(double),
            HighestRatio = default(double),
            LowestRatio = default(double),
            ValidBuildCount = default(int),
        });

        InitPython();
        using (Py.GIL())
        {
            // PythonNet can't be used in async context.
            foreach (var job in jobs)
            {
                _logger.LogInformation("{sample.VcsUrl} {sample.JobName}", job.VcsUrl, job.JobName);

                var ratio = CalculateClustersByMonths(job.VcsUrl, job.JobName);
                if (ratio is not null)
                {
                    ratioList.Add(new
                    {
                        VcsUrl = job.VcsUrl,
                        JobName = job.JobName,
                        WeightedMiddle = ratio.WeightedMiddle,
                        AverageRatio = ratio.Average,
                        HighestRatio = ratio.Highest,
                        LowestRatio = ratio.Lowest,
                        ValidBuildCount = ratio.ValidBuildCount,
                    });
                }
            }
        }

        await using var ratioCsvWriter =
            new CsvWriter(File.CreateText(_directoryHelper.GetFullPath("k-means_all.csv")), CultureInfo.InvariantCulture);
        await ratioCsvWriter.WriteRecordsAsync(ratioList);
    }

    public async Task CalculateCluster()
    {
        var samples = await _forecastingContext.AccelerationSamples.Include(s => s.ClusterCenters)
            .ToListAsync()
            .ConfigureAwait(false);
        InitPython();
        var resultList = MakeListOfInstanceType(new
        {
            VcsUrl = default(string)!,
            JobName = default(string)!,
            Month = default(string)!,
            Higher = default(double),
            Lower = default(double),
            Count = default(int),
            VarianceHigher = default(double),
            VarianceLower = default(double),
        });
        var logResultList = MakeListOfInstanceType(new
        {
            VcsUrl = default(string)!,
            JobName = default(string)!,
            Month = default(string)!,
            Higher = default(double),
            Lower = default(double),
            Count = default(int),
            VarianceHigher = default(double),
            VarianceLower = default(double),
        });

        var ratioList = MakeListOfInstanceType(new
        {
            VcsUrl = default(string)!,
            JobName = default(string)!,
            WeightedMiddle = default(double),
            AverageRatio = default(double),
            HighestRatio = default(double),
            LowestRatio = default(double),
            ValidBuildCount = default(int),
        });
        var logRatioList = MakeListOfInstanceType(new
        {
            VcsUrl = default(string)!,
            JobName = default(string)!,
            WeightedMiddle = default(double),
            AverageRatio = default(double),
            HighestRatio = default(double),
            LowestRatio = default(double),
            ValidBuildCount = default(int),
        });

        using (var _gil = Py.GIL())
        {
            // PythonNet can't be used in async context.
            foreach (var sample in samples)
            {
                _logger.LogInformation("{sample.VcsUrl} {sample.JobName}", sample.VcsUrl, sample.JobName);
                if (sample.ClusterCenters.Count > 0)
                {
                    _logger.LogInformation("Cluster centers existing, updating...");
                    sample.ClusterCenters.Clear();
                }

                if (sample.ClusterCentersLog == null)
                {
                    sample.ClusterCentersLog = new();
                }
                else if (sample.ClusterCentersLog.Count > 0)
                {
                    _logger.LogInformation("Cluster centers (log) existing, updating...");
                    sample.ClusterCentersLog.Clear();
                }

                var hitManualRules = sample.HitManualRules.Count > 0;
                var ratio = CalculateClustersByMonths(sample.VcsUrl, sample.JobName, (month, cluster, count) =>
                {
                    var (lower, higher, varianceLower, varianceHigher) = cluster;
                    resultList.Add(new
                    {
                        VcsUrl = sample.VcsUrl,
                        JobName = sample.JobName,
                        Month = month,
                        Higher = higher,
                        Lower = lower,
                        Count = count,
                        VarianceHigher = varianceHigher,
                        VarianceLower = varianceLower,
                    });
                    sample.ClusterCenters.Add(new KMeansClusters
                    {
                        Month = month,
                        Higher = higher,
                        Lower = lower,
                    });
                });
                if (ratio is not null)
                {
                    ratioList.Add(new
                    {
                        VcsUrl = sample.VcsUrl,
                        JobName = sample.JobName,
                        WeightedMiddle = hitManualRules ? 0 : ratio.WeightedMiddle,
                        AverageRatio = hitManualRules ? 0 : ratio.Average,
                        HighestRatio = hitManualRules ? 0 : ratio.Highest,
                        LowestRatio = hitManualRules ? 0 : ratio.Lowest,
                        ValidBuildCount = ratio.ValidBuildCount,
                    });
                }
                var ratioLog = CalculateClustersByMonths(sample.VcsUrl, sample.JobName, (month, cluster, count) =>
                {
                    var (lower, higher, varianceLower, varianceHigher) = cluster;
                    logResultList.Add(new
                    {
                        VcsUrl = sample.VcsUrl,
                        JobName = sample.JobName,
                        Month = month,
                        Higher = higher,
                        Lower = lower,
                        Count = count,
                        VarianceHigher = varianceHigher,
                        VarianceLower = varianceLower,
                    });
                    sample.ClusterCentersLog.Add(new KMeansClusters
                    {
                        Month = month,
                        Higher = Math.Exp(higher),
                        Lower = Math.Exp(lower),
                    });
                }, Math.Log);
                if (ratioLog is not null)
                {
                    logRatioList.Add(new
                    {
                        VcsUrl = sample.VcsUrl,
                        JobName = sample.JobName,
                        WeightedMiddle = hitManualRules ? 0 : ratioLog.WeightedMiddle,
                        AverageRatio = hitManualRules ? 0 : ratioLog.Average,
                        HighestRatio = hitManualRules ? 0 : ratioLog.Highest,
                        LowestRatio = hitManualRules ? 0 : ratioLog.Lowest,
                        ValidBuildCount = ratioLog.ValidBuildCount,
                    });
                }
            }
        }

        await using var clusterCsvWriter =
            new CsvWriter(File.CreateText(_directoryHelper.GetFullPath("KMeansCluster.csv")), CultureInfo.InvariantCulture);
        await clusterCsvWriter.WriteRecordsAsync(resultList);

        await using var ratioCsvWriter =
            new CsvWriter(File.CreateText(_directoryHelper.GetFullPath("KMeansClusterRatio.csv")), CultureInfo.InvariantCulture);
        await ratioCsvWriter.WriteRecordsAsync(ratioList);

        await using var logClusterCsvWriter =
            new CsvWriter(File.CreateText(_directoryHelper.GetFullPath("KMeansCluster_log.csv")), CultureInfo.InvariantCulture);
        await logClusterCsvWriter.WriteRecordsAsync(logResultList);

        await using var logRatioCsvWriter =
            new CsvWriter(File.CreateText(_directoryHelper.GetFullPath("KMeansClusterRatio_log.csv")), CultureInfo.InvariantCulture);
        await logRatioCsvWriter.WriteRecordsAsync(logRatioList);

        await _forecastingContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static void InitPython()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Runtime.PythonDLL = @"C:\Users\*\AppData\Local\Programs\Python\Python310\python310.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Runtime.PythonDLL = "/usr/lib/aarch64-linux-gnu/libpython3.10.so.1";
            Runtime.PythonDLL = "/usr/lib/python3.10/config-3.10-x86_64-linux-gnu/libpython3.10.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Runtime.PythonDLL =
                // "/opt/homebrew/opt/python@3.10/Frameworks/Python.framework/Versions/3.10/lib/libpython3.10.dylib";
                "/Users/*/opt/anaconda3/lib/libpython3.9.dylib";
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
        PythonEngine.Initialize();
    }

    private Ratio? CalculateClustersByMonths(string vcsUrl, string jobName, Action<string, Cluster, int>? callback = null, Func<double, double>? timeConvert = null)
    {
        var builds = _forecastingContext.Builds.AsNoTracking()
            .Where(b => b.VcsUrl == vcsUrl && b.JobName == jobName &&
                b.SumOfBuildTimeInSteps != TimeSpan.Zero)
            .Select(b => new
            {
                b.SumOfBuildTimeInSteps,
                b.StartTime
            })
            .ToList();
        if (builds.Count < 2)
        {
            return default;
        }

        _logger.LogWarning("Filtering top and bottom. originally {builds.Count} builds", builds.Count);
        double topTime = builds
            .Select(b => b.SumOfBuildTimeInSteps.TotalSeconds)
            .OrderByDescending(t => t)
            .ElementAt((int)((builds.Count - 1) * ExcludeTopProportion));
        double bottomTime = builds
            .Select(b => b.SumOfBuildTimeInSteps.TotalSeconds)
            .OrderBy(t => t)
            .ElementAt((int)((builds.Count - 1) * ExcludeBottomProportion));

        builds = builds.Where(b =>
                b.SumOfBuildTimeInSteps.TotalSeconds <= topTime &&
                b.SumOfBuildTimeInSteps.TotalSeconds >= bottomTime)
            .ToList();
        _logger.LogInformation("{builds.Count} builds remaining.", builds.Count);

        if (builds.Count < 2)
        {
            return default;
        }

        var allBuildTime = builds.Select(b => b.SumOfBuildTimeInSteps.TotalSeconds);
        if (timeConvert != null)
        {
            allBuildTime = allBuildTime.Select(timeConvert);
        }
        var allCluster =
            GetClusterCenters(allBuildTime.ToList());
        callback?.Invoke("All", allCluster, builds.Count);

        var buildTimeMonths = builds
            .GroupBy(b => (b.StartTime.Year, b.StartTime.Month))
            .OrderBy(g => (g.Key.Year << 4) + g.Key.Month);

        var ratios = MakeListOfInstanceType(new
        {
            Ratio = default(double),
            BuildCount = default(int),
        });

        foreach (var mo in buildTimeMonths)
        {
            var (year, month) = mo.Key;
            var buildTimes = mo.Select(b => b.SumOfBuildTimeInSteps.TotalSeconds).ToList();
            if (buildTimes.Count < 2)
            {
                continue;
            }
            if (timeConvert is not null)
            {
                buildTimes = buildTimes.ConvertAll(t => timeConvert(t));
            }

            var monthCluster = GetClusterCenters(buildTimes);
            var (lower, higher, varianceLower, varianceHigher) = monthCluster;
            ratios.Add(new
            {
                Ratio = (Math.Sqrt(varianceLower) + Math.Sqrt(varianceHigher)) / (higher - lower),
                BuildCount = buildTimes.Count,
            });

            callback?.Invoke($"{year:0000}-{month:00}", monthCluster, buildTimes.Count);

            var logBuildTimes = buildTimes.ConvertAll(Math.Log);
        }

        return new Ratio(
            GetMiddle(ratios.Select(r => (r.Ratio, r.BuildCount)).ToList()),
            ratios.Average(r => r.Ratio),
            ratios.Max(r => r.Ratio),
            ratios.Min(r => r.Ratio),
            builds.Count);
    }

    private readonly record struct Cluster(double Lower, double Higher, double VarianceLower, double VarianceHigher)
    {
        public void Deconstruct(out double lower, out double higher, out double varianceLower, out double varianceHigher)
        {
            lower = Lower;
            higher = Higher;
            varianceLower = VarianceLower;
            varianceHigher = VarianceHigher;
        }
    }

    private record Ratio(double WeightedMiddle, double Average, double Highest, double Lowest, int ValidBuildCount);

    private static Cluster GetClusterCenters(
        List<double> buildTimeList)
    {
        dynamic np = Py.Import("numpy");
        dynamic KMeans = (Py.Import("sklearn.cluster") as dynamic).KMeans;

        var kmeans = KMeans(n_clusters: 2);
        var data = np.array(buildTimeList).reshape(-1, 1);
        kmeans.fit(data);

        var (lower, higher) = ((double)kmeans.cluster_centers_[0][0], (double)kmeans.cluster_centers_[1][0]);
        if (lower > higher)
        {
            (higher, lower) = (lower, higher);
        }

        double varianceLower =
            GetVariance(buildTimeList.Where(t => Math.Abs(t - lower) <= Math.Abs(t - higher)).ToList());
        double varianceHigher =
            GetVariance(buildTimeList.Where(t => Math.Abs(t - higher) < Math.Abs(t - lower)).ToList());

        return new Cluster(lower, higher, varianceLower, varianceHigher);
    }

    private static double GetVariance(List<double> buildTimeList)
    {
        if (buildTimeList.Count == 0)
        {
            return double.NaN;
        }

        var mean = buildTimeList.Average();
        var variance = buildTimeList.Sum(b => (b - mean) * (b - mean)) / buildTimeList.Count;
        return variance;
    }

    private static List<T> MakeListOfInstanceType<T>(T instance)
    {
        return new List<T>();
    }

    private double GetMiddle(IList<(double, int)> values)
    {
        values = values.Where(t => !double.IsNaN(t.Item1)).OrderBy(v => v.Item1).ToArray();
        if (values.Count == 0)
        {
            return double.NaN;
        }

        int exceeding = 0;
        int i = 0;
        int j = values.Count - 1;
        while (i < j)
        {
            if (exceeding <= 0)
            {
                exceeding += values[i].Item2;
                i++;
            }
            else
            {
                exceeding -= values[j].Item2;
                j--;
            }
        }

        Debug.Assert(i == j);
        if (exceeding - values[i].Item2 > 0)
        {
            return values[i - 1].Item1;
        }
        else if (exceeding + values[i].Item2 < 0)
        {
            return values[i + 1].Item1;
        }
        else if (exceeding - values[i].Item2 == 0)
        {
            return (values[i - 1].Item1 + values[i].Item1) / 2;
        }
        else if (exceeding + values[i].Item2 == 0)
        {
            return (values[i + 1].Item1 + values[i].Item1) / 2;
        }
        else
        {
            return values[i].Item1;
        }
    }
}
