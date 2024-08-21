using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.WebApp.Controllers
{
    public partial class TimeSeriesController : Controller
    {
        public async Task<IActionResult> ForecastByWindowSize(string repo, string job, string branch = "", string why = "", bool forecastAll = false, int days = 31, string where = "",
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
            var forecastedZipped = zipped.Where(b => b.ForecastedBuildTime.HasValue).ToList();

            var countOfForecasted = forecastedZipped.Count();
            var sumOfMre = forecastedZipped
                .Sum(z => Math.Abs((z.ForecastedBuildTime!.Value - z.BuildTime).TotalSeconds) / z.BuildTime.TotalSeconds);
            ViewBag.mmre = sumOfMre / countOfForecasted;

            // Calculate R square
            var average = forecastedZipped.Average(z => z.BuildTime.TotalSeconds);
            var sumOfSquaresOfResiduals = forecastedZipped.Sum(z => Math.Pow(z.BuildTime.TotalSeconds - z.ForecastedBuildTime!.Value.TotalSeconds, 2));
            var sumOfSquaresOfTotal = forecastedZipped.Sum(z => Math.Pow(z.BuildTime.TotalSeconds - average, 2));
            var rSquare = 1 - sumOfSquaresOfResiduals / sumOfSquaresOfTotal;
            ViewBag.rSquare = rSquare;

            // Calculate RMSE
            var rmse = Math.Sqrt(sumOfSquaresOfResiduals / countOfForecasted);
            ViewBag.rmse = rmse;

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
                    data = forecastedZipped
                        .Select(b =>
                            (new object[] { b.StartTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), b.ForecastedBuildTime!.Value.TotalMilliseconds })),
                    type = "line"
                },
                forecastAll ? additional : default,
            };
            ViewBag.Legend = new
            {
                bottom = 30,
                data = series.Where(s => s != null).Select(s => s!.name),
            };
            return View("Index", series);
        }
    }
}
