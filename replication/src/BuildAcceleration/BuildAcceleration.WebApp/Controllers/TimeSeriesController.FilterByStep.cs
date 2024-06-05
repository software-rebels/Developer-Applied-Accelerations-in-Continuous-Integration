using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.WebApp.Controllers
{
    public partial class TimeSeriesController : Controller
    {
        public async Task<IActionResult> FilterByStep(string repo, string job, string branch = "", string why = "", bool forecastAll = false, int days = 31, string where = "",
            [FromServices] ForecastingContext forecastingContext = default!, [FromServices] IMongoCollection<BuildEntry> mongoCollection = default!)
        {
            var vcsUrl = "https://github.com/" + repo;
            ViewBag.Repo = repo;
            // test url: https://deprecated/TimeSeries/ForecastByWindowSize?repo=diem/diem&job=code_coverage&branch=master&why=github&forecastAll=true&where=(%22AttachedProperties%22-%3E%27build_of_day%27)::integer!%3D1
            IQueryable<BuildEntrySlim> filtering = string.IsNullOrWhiteSpace(where)
                ? forecastingContext.Builds
                : forecastingContext.Builds.FromSqlRaw($"SELECT * FROM \"Builds\" WHERE {where}");
            filtering = filtering.Where(b => b.VcsUrl == vcsUrl && b.JobName == job);
            if (!string.IsNullOrWhiteSpace(branch))
                filtering = filtering.Where(b => b.Branch == branch);
            if (!string.IsNullOrWhiteSpace(why))
                filtering = filtering.Where(b => b.Why == why);
            {
                List<string> mongoQueryList = await filtering.Select(b => b.BuildUrl).ToListAsync().ConfigureAwait(false);
                List<BuildEntry> testedBuilds = await (mongoCollection.AsQueryable()
                    .Where(b => mongoQueryList.Contains(b.BuildUrl) && b.Steps.Any(s => s.Name == "Run code coverage"))
                    as IAsyncCursorSource<BuildEntry>)
                    .ToListAsync();
                List<string> finalBuildUrls = testedBuilds.ConvertAll(b => b.BuildUrl);
                filtering = filtering.Where(b => finalBuildUrls.Contains(b.BuildUrl));
            }
            var builds = await filtering
                .OrderBy(b => b.StartTime)
                .Select(b => new { b.StartTime, b.SumOfBuildTimeInSteps, b.SelectedSuccess, b.CircleYmlHash })
                .ToArrayAsync().ConfigureAwait(false);

            var selectedBuilds =
                forecastAll
                ? builds
                : builds
                    .Where(b => b.SelectedSuccess)
                    .ToArray();
            var forecasted = new TimeSpan?[selectedBuilds.Length];
            var slidingWindowStart = 0;
            for (int i = 1; i < selectedBuilds.Length; i++)
            {
                while (builds[slidingWindowStart].StartTime < selectedBuilds[i].StartTime - TimeSpan.FromDays(days))
                {
                    slidingWindowStart++;
                }

                if (builds[slidingWindowStart].StartTime >= selectedBuilds[i].StartTime)
                {
                    continue;
                }

                var predictedByAverageMillis = builds[slidingWindowStart..]
                    .TakeWhile(b => b.StartTime < selectedBuilds[i].StartTime)
                    .Average(b => b.SumOfBuildTimeInSteps.TotalMilliseconds);

                forecasted[i] = TimeSpan.FromMilliseconds(predictedByAverageMillis);
            }

            var zipped = selectedBuilds.Zip(forecasted, (o, f) => new
            {
                o.StartTime,
                BuildTime = o.SumOfBuildTimeInSteps,
                ForecastedBuildTime = f,
                o.CircleYmlHash,
            }).ToList();

            var countOfForecasted = zipped.Count(z => z.ForecastedBuildTime != null);
            var sumOfMre = zipped.Where(z => z.ForecastedBuildTime != null)
                .Sum(z => Math.Abs((z.ForecastedBuildTime!.Value - z.BuildTime).TotalSeconds) / z.BuildTime.TotalSeconds);
            ViewBag.mmre = sumOfMre / countOfForecasted;

            var additional = new
            {
                name = default(string)!,
                symbolSize = default(int),
                data = default(IEnumerable<object[]>)!,
                type = default(string)!,
            };
            if (forecastAll)
            {
                string lastYaml = string.Empty;
                additional = new
                {
                    name = "yaml-changed",
                    symbolSize = 5,
                    data = zipped
                        .Where(b =>
                        {
                            string currentYaml = Convert.ToHexString(b.CircleYmlHash);
                            bool changed = currentYaml != lastYaml;
                            lastYaml = currentYaml;
                            return changed;
                        })
                        .Select(b =>
                            (new object[] { b.StartTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), b.BuildTime.TotalMilliseconds }))
                        .ToList()
                        as IEnumerable<object[]>,
                    type = "scatter"
                };
            }

            var series = new[]
            {
                new
                {
                    name = "Actural",
                    symbolSize = 5,
                    data = zipped
                        .Select(b =>
                            (new object[] { b.StartTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), b.BuildTime.TotalMilliseconds })),
                    type = "scatter"
                },
                new
                {
                    name = "Forecasted",
                    symbolSize = 5,
                    data = zipped
                        .Where(b => b.ForecastedBuildTime != null)
                        .Select(b =>
                            (new object[] { b.StartTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), b.ForecastedBuildTime!.Value.TotalMilliseconds })),
                    type = "line"
                },
                forecastAll ? additional : default,
            };
            ViewBag.Legend = new
            {
                bottom = 30,
                data = series.Select(s => s.name),
            };
            return View("Index", series);
        }
    }
}
