using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.WebApp.Controllers;
public class BuildDetailsController : Controller
{
    // public async Task<IActionResult> Index(string repo, string job, string commit_id, [FromServices] IMongoCollection<BuildEntry> builds)
    // {
    //     var build = await builds.AsQueryable()
    //         .Where(b => b.VcsUrl == "https://github.com/" + repo && b.Workflows!.JobName == job && b.VcsRevision == commit_id)
    //         .FirstOrDefaultAsync();
    //     if (build is null)
    //     {
    //         return NotFound();
    //     }
    //     ViewBag.Repo = repo;
    //     return View(build);
    // }

    public async Task<IActionResult> Index(string build_url, [FromServices] IMongoCollection<BuildEntry> builds)
    {
        var build = await builds.AsQueryable()
            .FirstOrDefaultAsync(b => b.BuildUrl == build_url);
        if (build is null)
        {
            return NotFound();
        }
        ViewBag.Repo = build.VcsUrl["https://github.com/".Length..];
        return View(build);
    }
}
