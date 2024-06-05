using System;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.WebApp.Controllers
{
    public class BuildListController : Controller
    {
        public async Task<IActionResult> IndexAsync(int threshold_s, bool upper,
            string repo, string job, string branch = "", string why = "",
            [FromServices] ForecastingContext forecastingContext = default!)
        {
            string vcsUrl = "https://github.com/" + repo;
            ViewBag.Repo = repo;
            IQueryable<BuildEntrySlim> filtering = forecastingContext.Builds
                .Where(b => b.VcsUrl == vcsUrl && b.JobName == job);
            if (!string.IsNullOrWhiteSpace(branch))
                filtering = filtering.Where(b => b.Branch == branch);
            if (!string.IsNullOrWhiteSpace(why))
                filtering = filtering.Where(b => b.Why == why);
            var builds = await filtering
                .OrderBy(b => b.StartTime)
                .Select(b => new { b.StartTime, b.BuildUrl, b.VcsRevision, BuildTime = b.SumOfBuildTimeInSteps })
                .ToArrayAsync().ConfigureAwait(false);

            return View(builds
                .Where(b => upper ? (b.BuildTime >= TimeSpan.FromSeconds(threshold_s)) : (b.BuildTime < TimeSpan.FromSeconds(threshold_s)))
                .Select(b => new
                {
                    b.StartTime,
                    b.BuildTime,
                    BuildUrl = new Uri(b.BuildUrl),
                    CommitUrl = new Uri(vcsUrl + "/commit/" + b.VcsRevision),
                }));
        }
    }
}
