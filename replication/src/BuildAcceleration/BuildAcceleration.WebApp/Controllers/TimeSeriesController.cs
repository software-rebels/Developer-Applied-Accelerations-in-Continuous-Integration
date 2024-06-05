using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.WebApp.Utility;
using MathNet.Numerics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.WebApp.Controllers
{
    public partial class TimeSeriesController : Controller
    {
        // GET
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> BuildTimeTrendAsync(string repo, string job = "", int count = 10000,
            [FromServices] IMongoCollection<BuildEntry> buildCollection = default!,
            [FromServices] IMemoryCache memoryCache = default!)
        {
            ViewBag.Repo = repo;
            ViewBag.Legend = new object();
            var vcsUrl = "https://github.com/" + repo;
            // var builds = await (await buildCollection.FindAsync(b => b.VcsUrl == vcsUrl)).ToListAsync();
            var builds = buildCollection.AsQueryable().Where(b => b.VcsUrl == vcsUrl && b.BuildTimeMillis < 506807875);
            var temp = builds
                    .Where(b => b.Status == "success")
                    .ToEnumerable()
                    .Where(b => b.QueuedAt != default && b.Steps.Count != 0 && b.Workflows?.JobName != null)
                    .TakeRandom(count);
            if (!string.IsNullOrWhiteSpace(job))
                temp = temp.Where(b => string.Equals(b.Workflows!.JobName, job, StringComparison.OrdinalIgnoreCase));
            var result = temp.GroupBy(b => b.Workflows!.JobName);
            var sb = new StringBuilder("<br/>");
            ViewBag.mmre = sb;
            var series = result.Select(g =>
            {
                var markLineData = new List<object>
                {
                    new
                    {
                        name = "Average",
                        type = "average",
                    }
                };
                var data = g
                        .Select(b =>
                            (b.QueuedAt,
                             b.Steps.Sum(s => (long?)(s.Actions.First().RunTimeMillis)) ?? 0));
                if (g.Take(2).Count() >= 2)
                {
                    var x = data.Select(b => (double)b.QueuedAt.ToUnixTimeSeconds()).ToArray();
                    var y = data.Select(b => (double)b.Item2).ToArray();
                    var (intercept, slope) = Fit.Line(x, y);
                    var mmre = data.Sum(b => Math.Abs(slope * b.QueuedAt.ToUnixTimeSeconds() + intercept - b.Item2) / b.Item2) / g.Count();
                    sb.Append(g.Key).Append(": ").Append(mmre).Append("<br/>");
                    var min_x = data.Min(b => b.QueuedAt);
                    var max_x = data.Max(b => b.QueuedAt);
                    var min_y = min_x.ToUnixTimeSeconds() * slope + intercept;
                    var max_y = max_x.ToUnixTimeSeconds() * slope + intercept;
                    markLineData.Add(new object[]
                    {
                        new
                        {
                            name = "Regression",
                            coord = new object[] { min_x.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), min_y }
                        },
                        new
                        {
                            coord = new object[] { max_x.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), max_y }
                        }
                    });
                }
                return new
                {
                    name = g.Key,
                    symbolSize = 5,
                    data = data
                        .Select(d =>
                            (new object[] { d.QueuedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"), d.Item2 })),
                    type = "scatter",
                    markLine = new
                    {
                        label = new { formatter = g.Key },
                        tooltip = new { formatter = g.Key },
                        data = markLineData
                    }
                };
            });
            ViewBag.Legend = new
            {
                bottom = 30,
                data = series.Select(s => s.name),
            };
            return View("Index", series);
        }

        public IActionResult BuildTimeTrendByStatus(string repo, int count = 10000,
            [FromServices] IMongoCollection<BuildEntry> buildCollection = default!,
            [FromServices] IMemoryCache memoryCache = default!)
        {
            ViewBag.Repo = repo;
            ViewBag.Legend = new object();
            var vcsUrl = "https://github.com/" + repo;
            // var builds = await (await buildCollection.FindAsync(b => b.VcsUrl == vcsUrl)).ToListAsync();
            var builds = buildCollection.AsQueryable().Where(b => b.VcsUrl == vcsUrl && b.BuildTimeMillis < 506807875);
            var result = builds
                    .ToEnumerable()
                    .Where(b => b.QueuedAt != default && b.Steps.Count != 0)
                    .TakeRandom(count)
                    .GroupBy(b => b.Status);
            var series = result.Select(g => new
            {
                name = g.Key,
                symbolSize = 5,
                data = g
                    .Select(b =>
                        (b.QueuedAt.UtcDateTime,
                            b.Steps.Sum(s => (long?)(s.Actions.First().RunTimeMillis)) ?? 0))
                    .Select(d =>
                        new object[] { d.Item1.ToString("yyyy-MM-dd HH:mm:ss"), d.Item2 }),
                type = "scatter"
            });
            ViewBag.Legend = new
            {
                bottom = 30,
                data = series.Select(s => s.name),
            };
            return View("Index", series);
        }
    }
}