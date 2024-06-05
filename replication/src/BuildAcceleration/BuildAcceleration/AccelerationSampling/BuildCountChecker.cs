using System;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.AccelerationSampling;

public class BuildCountChecker
{
    private readonly ForecastingContext _forecastingContext;
    private readonly ILogger<BuildCountChecker> _logger;

    public BuildCountChecker(ForecastingContext forecastingContext, ILogger<BuildCountChecker> logger)
    {
        _forecastingContext = forecastingContext;
        _logger = logger;
    }

    // Find all build jobs, check if builds of any month are more than 50.
    // If so, mark the job as meeting the minimum sample size.
    public async Task CheckBuildCount()
    {
        var jobs = await _forecastingContext.Builds.AsNoTracking().Select(b => new { b.VcsUrl, b.JobName }).Distinct().ToListAsync().ConfigureAwait(false);
        int meetCount = 0;
        int totalCount = 0;
        foreach (var job in jobs)
        {
            _logger.LogInformation($"Checking {job.VcsUrl}/{job.JobName}");
            var builds = await _forecastingContext.Builds.AsNoTracking().Where(b => b.VcsUrl == job.VcsUrl && b.JobName == job.JobName && b.SumOfBuildTimeInSteps != TimeSpan.Zero).ToListAsync().ConfigureAwait(false);
            var buildsPerMonth = builds.GroupBy(b => b.StartTime.ToString("yyyy-MM")).ToList();
            if (buildsPerMonth.Count == 0)
            {
                _logger.LogInformation($"{job.VcsUrl}/{job.JobName} has no valid builds");
                continue;
            }
            var maxNonZeroBuildCount = buildsPerMonth.Max(b => b.Count());
            var meetMinimumSampleSize = maxNonZeroBuildCount >= 50;
            var jobInfo = new JobInfo
            {
                VcsUrl = job.VcsUrl,
                JobName = job.JobName,
                MaxNonZeroBuildCountPerMonth = maxNonZeroBuildCount,
                NonZeroBuildCount = builds.Count,
                MeetMinimumSampleSize = meetMinimumSampleSize,
                SampledToInspect = false,
            };
            _forecastingContext.JobInfos.Add(jobInfo);
            if (meetMinimumSampleSize)
            {
                meetCount++;
            }
            totalCount++;
            _logger.LogInformation($"{job.VcsUrl}/{job.JobName} has {maxNonZeroBuildCount} builds per month. {(meetMinimumSampleSize ? "Meets" : "Does not meet")} minimum sample size.");
        }
        _logger.LogInformation($"Meet minimum sample size: {meetCount}/{totalCount}");
        await _forecastingContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SampleBuildJobsAsync()
    {
        var jobs = await _forecastingContext.JobInfos.Where(j => j.MeetMinimumSampleSize).ToListAsync().ConfigureAwait(false);

    }
}