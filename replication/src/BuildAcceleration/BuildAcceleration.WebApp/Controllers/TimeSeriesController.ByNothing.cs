using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.WebApp.Controllers;
public partial class TimeSeriesController
{
    public async Task<IActionResult> ByNothing(string repo, string job, string branch = "", string why = "", string where = "", int maxSeconds = default,
            [FromServices] ForecastingContext forecastingContext = default!)
    {
        var vcsUrl = "https://github.com/" + repo;
        ViewBag.Repo = repo;
        // test url: https://aws-forecast.b11p.com/TimeSeries/ForecastByWindowSize?repo=diem/diem&job=code_coverage&branch=master&why=github&forecastAll=true&where=(%22AttachedProperties%22-%3E%27build_of_day%27)::integer!%3D1
        IQueryable<BuildEntrySlim> filtering = string.IsNullOrWhiteSpace(where)
            ? forecastingContext.Builds
            : forecastingContext.Builds.FromSqlRaw($"SELECT * FROM \"Builds\" WHERE {where}");
        filtering = filtering.Where(b => b.VcsUrl == vcsUrl && b.JobName == job);
        if (!string.IsNullOrWhiteSpace(branch))
            filtering = filtering.Where(b => b.Branch == branch);
        if (!string.IsNullOrWhiteSpace(why))
            filtering = filtering.Where(b => b.Why == why);
        if (maxSeconds != default)
            filtering = filtering.Where(b => b.SumOfBuildTimeInSteps <= TimeSpan.FromSeconds(maxSeconds));
        var builds = await filtering
            .OrderBy(b => b.StartTime)
            .Select(b => new { b.StartTime, b.SumOfBuildTimeInSteps, b.NumberOfSteps, b.BuildUrl, YamlUrl = $"{b.VcsUrl}/blob/{b.VcsRevision}/.circleci/config.yml", b.Branch, DetailUrl = $"/BuildDetails?repo={HttpUtility.UrlEncode(repo)}&job={HttpUtility.UrlEncode(job)}&commit_id={HttpUtility.UrlEncode(b.VcsRevision)}" })
            .ToArrayAsync().ConfigureAwait(false);

        var series = builds
            .GroupBy(b => job)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                name = $"{g.Key} steps",
                symbolSize = 5,
                data = g
                    .Select(b =>
                        new object[] { b.StartTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), b.SumOfBuildTimeInSteps.TotalMilliseconds, b.YamlUrl, /*b.DetailUrl,*/ $"/BuildDetails?build_url={HttpUtility.UrlEncode(b.BuildUrl)}" }),
                type = "scatter"
            })
            .ToList();

        // set last yaml from the most common branch in recent 1000 builds
        const int refBuildCount = 1000;
        var last100 = builds.Length > refBuildCount
            ? CreateSegment(builds)[^refBuildCount..]
            : builds;
        var (maxBranch, count) = builds.TakeLast(refBuildCount)
            .Aggregate(new Dictionary<string, nint>(),
                (d, b) =>
                {
                    d[b.Branch ?? string.Empty] = d.GetValueOrDefault(b.Branch ?? string.Empty) + 1;
                    return d;
                })
            .MaxBy(d => d.Value);
        if (!string.IsNullOrWhiteSpace(maxBranch))
        {
            var lastMajorBuild = builds.Last(b => b.Branch == maxBranch);
            ViewBag.LastYamlUrl = lastMajorBuild.YamlUrl;
            ViewBag.MaxBranchProportion = $"{count} / {(builds.Length > refBuildCount ? refBuildCount : builds.Length)}";
        }

        ViewBag.Legend = new
        {
            bottom = 30,
            data = series.Select(s => s.name),
        };
        return View("Index", series);
    }
}
