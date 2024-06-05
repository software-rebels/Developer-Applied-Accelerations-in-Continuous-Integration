using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.WebApp.Controllers;
public class BuildLogController : Controller
{
    public async Task<IActionResult> Index(string buildUrl, [FromServices] ForecastingContext forecastingContext)
    {
        var build = await forecastingContext.Builds.FirstOrDefaultAsync(b => b.BuildUrl == buildUrl).ConfigureAwait(false);
        ViewBag.CommitUrl = build?.VcsUrl + "/commit/" + build?.VcsRevision;
        ViewBag.VcsRevision = build?.VcsRevision;
        var apiUrl = buildUrl.Replace("https://circleci.com/gh/", "https://circleci.com/api/v1.1/project/github/");
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Add("Circle-Token", "65b3ad1736cc1a7bcff6b983ebd5ed97d0c87520");
        var ret = await httpClient.GetFromJsonAsync<ApiRet>(apiUrl).ConfigureAwait(false);
        var allocations = await ret!.Steps.SelectMany(s => s.Actions.Select(a => new { StepName = s.Name, Action = a }))
            .ToAsyncEnumerable()
            .SelectAwait(async a =>
            {
                LogOutput[]? output = default;
                if (a.Action.OutputUrl != null)
                {
                    await using var gzipStream = await httpClient.GetStreamAsync(a.Action.OutputUrl).ConfigureAwait(false);
                    await using var decompressor = new GZipStream(gzipStream, CompressionMode.Decompress);
                    output = await JsonSerializer.DeserializeAsync<LogOutput[]>(decompressor).ConfigureAwait(false);
                }
                return new { a.StepName, Output = output?[0].Message ?? string.Empty, AllocationId = a.Action.AllocationId, RunTime = TimeSpan.FromMilliseconds(a.Action.RunTimeMillis) };
            })
            .GroupBy(a => a.AllocationId)
            .ToListAsync();
        ViewBag.allocations = allocations;

        return View();
    }
}

public class ApiRet
{
    [JsonPropertyName("steps")]
    public Step[] Steps { get; set; } = default!;
}

public class Step
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
    [JsonPropertyName("actions")]
    public Action[] Actions { get; set; } = default!;
}

public class Action
{
    [JsonPropertyName("allocation_id")]
    public string AllocationId { get; set; } = default!;
    [JsonPropertyName("output_url")]
    public string? OutputUrl { get; set; }
    [JsonPropertyName("run_time_millis")]
    public long RunTimeMillis { get; set; }
}

public class LogOutput
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;
}
