using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.WebApp.Controllers;

public partial class HistogramController : Controller
{
    // GET: HistogramController1
    public ActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> ShowHistogram(string repo, string job, string start = "", string end = "", int? maxTimeSeconds = default, int? minTimeSeconds = default,
        [FromServices] ForecastingContext forecastingContext = default!)
    {
        int groups = 10;
        ViewBag.Repo = $"{repo} {job}";
        ViewBag.Legend = new object();
        var vcsUrl = "https://github.com/" + repo;
        var buildsQuery = forecastingContext.Builds.AsNoTracking().Where(b => b.VcsUrl == vcsUrl && b.JobName == job);
        DateTimeOffset? startDate = default;
        DateTimeOffset? endDate = default;
        if (!string.IsNullOrWhiteSpace(start))
        {
            startDate = DateTimeOffset.Parse(start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            buildsQuery = buildsQuery.Where(b => b.StartTime >= startDate);
        }
        if (!string.IsNullOrWhiteSpace(end))
        {
            endDate = DateTimeOffset.Parse(end, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            buildsQuery = buildsQuery.Where(b => b.StartTime < endDate);
        }
        var builds = await buildsQuery.ToListAsync().ConfigureAwait(false);

        var maxTime = builds.Max(b => b.SumOfBuildTimeInSteps.TotalSeconds);
        if (maxTimeSeconds.HasValue)
            maxTime = Math.Min(maxTime, maxTimeSeconds.Value);
        var minTime = minTimeSeconds ?? 0.0;
        var step = (maxTime - minTime) / groups;
        var buckets = Enumerable.Range(0, groups + 1).Select(i => (minTime + i * step, minTime + i * step + step)).ToArray();
        buckets[^1].Item2 = double.PositiveInfinity;
        var buildDict = builds
            .GroupBy(b => buckets.First(b2 => b.SumOfBuildTimeInSteps.TotalSeconds >= b2.Item1 && b.SumOfBuildTimeInSteps.TotalSeconds < b2.Item2))
            .ToDictionary(g => g.Key);

        ViewBag.Categories = buckets.Select(b => $"{TimeSpan.FromSeconds((int)b.Item1)}").ToArray();
        var data = buckets.Select(bucket =>
        {
            var builds = buildDict.GetValueOrDefault(bucket) ?? Enumerable.Empty<BuildEntrySlim>();
            return builds.Count();
        }).ToArray();
        ViewBag.Data = data;

        if (startDate != null && startDate?.AddMonths(1) == endDate)
        {
            var clusters = await forecastingContext.KMeansClusters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.VcsUrl == vcsUrl && c.JobName == job && c.Month == start).ConfigureAwait(false);
            if (clusters != null)
            {
                ViewBag.Higher = TimeSpan.FromSeconds(clusters.Higher).ToString();
                ViewBag.Lower = TimeSpan.FromSeconds(clusters.Lower).ToString();
                ViewBag.SplitAt = TimeSpan.FromSeconds((clusters.Higher + clusters.Lower) / 2).ToString();
            }
        }

        return View("Index");
    }
}
