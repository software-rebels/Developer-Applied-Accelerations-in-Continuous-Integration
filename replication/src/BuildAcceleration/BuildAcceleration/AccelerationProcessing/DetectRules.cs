using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.CsvEntities;
using ForecastBuildTime.Helpers;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using YamlDotNet.Serialization;
using Action = System.Action;

namespace ForecastBuildTime.AccelerationProcessing;

[Command("detect", Description = "Detect with circleci command line tools.")]
internal partial class DetectRules
{
    private readonly IMongoCollection<BuildEntry> _collection;
    private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
    private readonly ILogger<DetectRules> _logger;
    private readonly DirectoryHelper _directoryHelper;

    public DetectRules(IMongoCollection<BuildEntry> collection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<DetectRules> logger, DirectoryHelper directoryHelper)
    {
        _collection = collection;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _directoryHelper = directoryHelper;
    }

    [Option(Description = "Detect all jobs that satisfy the 10% requirement. If enabled, output a CSV file instead of writing to database.")]
    public bool All { get; set; }

    public async Task OnExecuteAsync()
    {
        if (!All)
        {
            await DetectSamplesAndSaveToDb();
        }
        else
        {
            await DetectAllAsync();
        }
    }

    private async ValueTask DetectSamplesAndSaveToDb()
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var samples = await db.AccelerationSamples.ToListAsync();
        foreach (var sample in samples)
        {
            await DetectRule(db, sample.VcsUrl, sample.JobName, () =>
            {
                _logger.LogInformation("Hitted manual rule: repo {vcsUrl}, job {jobName}", sample.VcsUrl, sample.JobName);
                sample.HitManualRules = new Dictionary<string, bool>(sample.HitManualRules)
                {
                    ["circle-command"] = true,
                };
            });
        }
        _logger.LogInformation("Saving changes to database...");
        await db.SaveChangesAsync();
    }

    private async ValueTask DetectAllAsync()
    {
        await using var db = _dbContextFactory.CreateDbContext();
        var jobCount = await db.JobInfos.CountAsync();
        var jobs = await db.JobInfos.AsNoTracking().OrderByDescending(j => j.NonZeroBuildCount).ThenByDescending(j => j.Random).Take(jobCount / 10).ToListAsync();
        List<RuledDetection> resultList = new List<RuledDetection>();
        foreach (var job in jobs)
        {
            var hitRule = await DetectRule(db, job.VcsUrl, job.JobName);
            resultList.Add(new RuledDetection
            (
                job.VcsUrl,
                job.JobName,
                job.Random,
                job.NonZeroBuildCount,
                job.MaxNonZeroBuildCountPerMonth,
                hitRule
            ));
        }
        await RuledDetection.WriteAsync(_directoryHelper, resultList);
    }

    private async ValueTask<bool> DetectRule(ForecastingContext db, string vcsUrl, string jobName, Action? matchCallback = null)
    {
        var last1000 = await db.Builds
                .AsNoTracking()
                .Where(b => b.VcsUrl == vcsUrl && b.JobName == jobName)
                .OrderByDescending(b => b.StartTime)
                .Take(1000)
                .ToListAsync();
        var (maxBranch, count) = last1000.Aggregate(new Dictionary<string, nint>(),
                (d, b) => { d[b.Branch ?? string.Empty] = d.GetValueOrDefault(b.Branch ?? string.Empty) + 1; return d; })
            .MaxBy(d => d.Value);
        if (!string.IsNullOrWhiteSpace(maxBranch))
        {
            var lastMajorBuild = last1000.First(b => b.Branch == maxBranch);
            var build = await _collection.AsQueryable().FirstAsync(b => b.BuildUrl == lastMajorBuild.BuildUrl);
            var yaml = build.CircleYml?.String;
            if (yaml == null)
            {
                _logger.LogError("Cannot find YAML in repo {vcsUrl}, job {jobName}", vcsUrl, jobName);
                return false;
            }
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            CircleYmlDefinition definition;
            try
            {
                var yamlObject = deserializer.Deserialize<object>(yaml);
                definition = YamlResolver.ConvertToType<CircleYmlDefinition>(yamlObject);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error parsing YAML in repo {vcsUrl}, job {jobName}", vcsUrl, jobName);
                return false;
            }
            var steps = definition.Jobs.GetValueOrDefault(jobName)?.Steps;
            if (steps == null)
            {
                _logger.LogError("Cannot find job in repo {vcsUrl}, job {jobName}", vcsUrl, jobName);
                return false;
            }

            // next, we search scripts that calls "circle" or "circleci-agent".
            // maybe starts with "circle"?
            foreach (var step in steps)
            {
                if (step is not Dictionary<object, object> s)
                {
                    continue;
                }
                if ((s.GetValueOrDefault("run") as Dictionary<object, object>)?.GetValueOrDefault("command") is not string command)
                {
                    continue;
                }

                var regex = CircleCommandRegex();
                var isMatch = regex.IsMatch(command);
                if (isMatch)
                {
                    matchCallback?.Invoke();
                    return true;
                }
            }
        }
        return false;
    }

    [GeneratedRegex(@"^\s*circle", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex CircleCommandRegex();
}
