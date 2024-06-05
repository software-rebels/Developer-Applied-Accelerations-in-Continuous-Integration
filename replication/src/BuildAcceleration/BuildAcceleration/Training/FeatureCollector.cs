using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastBuildTime.DataTransfer;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.Training;

internal class FeatureCollector
{
    private readonly IMongoCollection<BuildEntry> _mongoCollection;
    private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
    private readonly ILogger<FeatureCollector> _logger;

    public FeatureCollector(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<FeatureCollector> logger)
    {
        _mongoCollection = mongoCollection;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task ExtractFeatures(string vcsUrl = "", string job = "", string branch = "", string why = "")
    {
        await using var csvWriter = new CsvHelper.CsvWriter(File.CreateText("data_set.csv"), CultureInfo.InvariantCulture);

        // string localRepoPath = $@"C:\projects\{vcsUrl[(vcsUrl.LastIndexOf('/') + 1)..]}";
        // var localRepo = new Repository(localRepoPath);
        // var commits = localRepo.Commits.QueryBy(new CommitFilter
        // {
        //     IncludeReachableFrom = mongoBuilds.Select(b => b.VcsRevision).ToList(),
        //     SortBy = CommitSortStrategies.Topological
        // });

        IAsyncEnumerable<BuildEntrySlim[]> chunks = CreateChunks(vcsUrl, job, branch, why, 1000);
        int i = 0;
        await foreach (BuildEntrySlim[] buildSlimEntries in chunks)
        {
            _logger.LogInformation("Processing chunk {}", i++);
            List<string> buildUrls = buildSlimEntries.Select(b => b.BuildUrl).ToList();

            List<BuildEntry> mongoBuilds = await
                (await _mongoCollection.AsQueryable().Where(b => buildUrls.Contains(b.BuildUrl))
                    .ToCursorAsync().ConfigureAwait(false))
                .ToListAsync().ConfigureAwait(false);

            var dataEnum =
                from s in buildSlimEntries
                join b in mongoBuilds on s.BuildUrl equals b.BuildUrl into mongoJoin
                from b in mongoJoin.DefaultIfEmpty()
                    // let commit = commits.FirstOrDefault(c => c.Sha == b?.VcsRevision)
                    // let parentCommit = commit?.Parents.FirstOrDefault()
                    // let diff = localRepo.Diff.Compare<TreeChanges>(parentCommit?.Tree, commit?.Tree)
                select new
                {
                    BuildUrl = s.BuildUrl,
                    StartTime = s.StartTime.ToUnixTimeSeconds() - new DateTimeOffset(2020, 1, 1, 0, 0, 0, default).ToUnixTimeSeconds(),
                    BuildTime = (long)s.SumOfBuildTimeInSteps.TotalMilliseconds,
                    NumberOfSteps = b.Steps.Count,
                    NoDependencyCache = Convert.ToInt32(b.NoDependencyCache == true),
                    Oss = Convert.ToInt32(b.Oss),
                    Parallel = b.Parallel,
                    IsRetry = Convert.ToInt32(b.RetryOf != null),
                    SshDisabled = Convert.ToInt32(b.SshDisabled),
                    Timedout = Convert.ToInt32(b.Timedout),
                    BuildNum = b.BuildNum,
                    IsUser = Convert.ToInt32(b.User.IsUser),
                    // IsMerged = Convert.ToInt32(commit?.Parents.Count() > 1),
                    // Added = diff.Added.Count(),
                    // Deleted = diff.Deleted.Count(),
                    // Modified = diff.Modified.Count(),
                    // Renamed = diff.Renamed.Count(),
                    // Copied = diff.Copied.Count(),
                    BeyoundThreshold = Convert.ToInt32(s.SumOfBuildTimeInSteps.TotalSeconds > 900),
                };

            await csvWriter.WriteRecordsAsync(dataEnum).ConfigureAwait(false);
        }
    }

    private async IAsyncEnumerable<BuildEntrySlim[]> CreateChunks(string vcsUrl, string job, string branch, string why, int chunkSize)
    {
        int lastId = -1;
        do
        {
            await using ForecastingContext forecastingContext = _dbContextFactory.CreateDbContext();
            IQueryable<BuildEntrySlim> filtering = forecastingContext.Builds.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(vcsUrl) || !string.IsNullOrWhiteSpace(job))
                filtering = filtering.Where(b => b.VcsUrl == vcsUrl && b.JobName == job);
            if (!string.IsNullOrWhiteSpace(branch))
                filtering = filtering.Where(b => b.Branch == branch);
            if (!string.IsNullOrWhiteSpace(why))
                filtering = filtering.Where(b => b.Why == why);

            filtering = filtering.OrderBy(b => b.Id);

            BuildEntrySlim[] chunk = await filtering.Where(b => b.Id > lastId).Take(chunkSize).ToArrayAsync().ConfigureAwait(false);
            if (chunk.Length > 0)
            {
                lastId = chunk.Last().Id;
                yield return chunk;
            }
            else
                break;
        } while (lastId > 0);
    }
}
