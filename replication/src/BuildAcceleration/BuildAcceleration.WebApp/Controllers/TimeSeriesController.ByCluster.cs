using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.WebApp.Controllers;

public partial class TimeSeriesController : Controller
{
    public async Task<IActionResult> ByCluster(string repo, string job, string where = "",
            [FromServices] ForecastingContext forecastingContext = default!)
    {
        var vcsUrl = "https://github.com/" + repo;
        ViewBag.Repo = repo;
        // test url: https://aws-forecast.b11p.com/TimeSeries/ForecastByWindowSize?repo=diem/diem&job=code_coverage&branch=master&why=github&forecastAll=true&where=(%22AttachedProperties%22-%3E%27build_of_day%27)::integer!%3D1
        IQueryable<BuildEntrySlim> filtering = string.IsNullOrWhiteSpace(where)
            ? forecastingContext.Builds
            : forecastingContext.Builds.FromSqlRaw($"SELECT * FROM \"Builds\" WHERE {where}");
        filtering = filtering.Where(b => b.VcsUrl == vcsUrl && b.JobName == job);
        var builds = await filtering
            .OrderBy(b => b.StartTime)
            .Select(b => new { b.StartTime, b.SumOfBuildTimeInSteps, b.BuildUrl, YamlUrl = $"{b.VcsUrl}/blob/{b.VcsRevision}/.circleci/config.yml", DetailUrl = $"/BuildDetails?repo={HttpUtility.UrlEncode(repo)}&job={HttpUtility.UrlEncode(job)}&commit_id={HttpUtility.UrlEncode(b.VcsRevision)}" })
            .ToArrayAsync().ConfigureAwait(false);

        var clusters = await forecastingContext.AccelerationSamples.AsNoTracking()
            .Where(s => s.VcsUrl == vcsUrl && s.JobName == job)
            .Include(s => s.ClusterCenters)
            .FirstOrDefaultAsync().ConfigureAwait(false);
        var clusterDict = (clusters?.ClusterCenters ?? new List<KMeansClusters>()).ToDictionary(c => c.Month);

        var buildCountMonth = new Dictionary<string, (int lowerCount, int higherCount)>();

        var series = builds
            .GroupBy(b =>
            {
                var month = b.StartTime.ToString("yyyy-MM");
                if (!clusterDict.TryGetValue(month, out var clusterCenter))
                    return "None";
                if (clusterCenter.Higher == 0 && clusterCenter.Lower == 0)
                    return "None";
                var fromHigher = Math.Abs(b.SumOfBuildTimeInSteps.TotalSeconds - clusterCenter.Higher);
                var fromLower = Math.Abs(b.SumOfBuildTimeInSteps.TotalSeconds - clusterCenter.Lower);
                if (fromHigher < fromLower)
                {
                    buildCountMonth.TryGetValue(month, out var count);
                    buildCountMonth[month] = (count.lowerCount, count.higherCount + 1);
                    return "Higher";
                }
                else
                {
                    buildCountMonth.TryGetValue(month, out var count);
                    buildCountMonth[month] = (count.lowerCount + 1, count.higherCount);
                    return "Lower";
                }
            })
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                name = $"{g.Key}",
                symbolSize = 5,
                data = g
                    .Select(b =>
                        new object[] { b.StartTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), b.SumOfBuildTimeInSteps.TotalMilliseconds, b.YamlUrl, /*b.DetailUrl,*/ $"/BuildDetails?build_url={HttpUtility.UrlEncode(b.BuildUrl)}" }),
                type = "scatter",
                yAxisIndex = 0,
                step = (object)false,
            })
            .ToList();

        if (buildCountMonth.Count > 0)
        {
            // duplicate last month with key of next month
            var lastMonth = buildCountMonth.Keys.Max()!;
            var lastMonthDate = DateTimeOffset.ParseExact(lastMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            var nextMonth = lastMonthDate.AddMonths(1).ToString("yyyy-MM");
            var nextMonthCount = buildCountMonth[lastMonth];
            buildCountMonth.Add(nextMonth, nextMonthCount);
        }

        // Get the percentage of lower of each month
        var lowerPercentageSeries = new
        {
            name = "LowerPercentage",
            symbolSize = 0,
            data = buildCountMonth
                .Select(kv => new object[] { kv.Key, kv.Value.lowerCount / (double)(kv.Value.lowerCount + kv.Value.higherCount) })
                .ToList().AsEnumerable(),
            type = "line",
            yAxisIndex = 1,
            step = (object)"end",

        };
        series.Add(lowerPercentageSeries);

        ViewBag.Legend = new
        {
            bottom = 30,
            data = series.Select(s => s.name),
        };
        return View("MultipleY", series);
    }
}
