using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ForecastBuildTime.DataFilter
{
    public class YamlAnalysis
    {
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
        private readonly ILogger<YamlAnalysis> _logger;

        public YamlAnalysis(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<YamlAnalysis> logger)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task GetYamlChangesAsync(string vcsUrl, string jobName, string branch, string why = "github")
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var builds = await dbContext.Builds.AsQueryable()
                .Where(b => b.VcsUrl == vcsUrl && b.JobName == jobName && b.Branch == branch && b.Why == why)
                .OrderBy(b => b.StartTime)
                .ToListAsync().ConfigureAwait(false);

            var yamlChanged = builds.Aggregate(new List<BuildEntrySlim>(), (l, b) =>
            {
                if (l.Count == 0 || !l[^1].CircleYmlHash.SequenceEqual(b.CircleYmlHash))
                {
                    l.Add(b);
                }
                return l;
            });

            // Currently this method show all builds.
            // It was designed to show builds that have a changed yaml file.
            // Change this to `yamlChanged` to apply the filter.
            var toDisplay = builds;

            var yamlHashes = toDisplay.Select(b => b.CircleYmlHash);
            var yamlFiles = await dbContext.CircleYmls.AsQueryable().Where(y => yamlHashes.Contains(y.Sha256)).ToListAsync().ConfigureAwait(false);
            var joined =
                from b in toDisplay
                join y in yamlFiles on Convert.ToHexString(b.CircleYmlHash) equals Convert.ToHexString(y.Sha256) into yamls
                from y in yamls.DefaultIfEmpty()
                select new { b, y };

            ConsoleTables.ConsoleTable
                .From(joined.Select(j => new { j.b.StartTime, j.b.BuildUrl, Yaml = Convert.ToHexString(j.b.CircleYmlHash), BuildTime = j.b.SumOfBuildTimeInSteps, YamlId = j.y?.Id }))
                .Write(ConsoleTables.Format.Alternative);
        }
    }
}
