using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataTransfer
{
    class MissingFieldTransferer
    {
        private const int IterateCount = 500;
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
        private readonly ILogger<MissingFieldTransferer> _logger;

        public MissingFieldTransferer(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<MissingFieldTransferer> logger)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task TransferFieldsAsync()
        {
            int i = 0;
            int last = 0;
            while (true)
            {
                await using var dbContext = _dbContextFactory.CreateDbContext();
                var target = await dbContext.Builds.AsQueryable().Where(b => b.Id > last).OrderBy(b => b.Id).Take(IterateCount).ToListAsync().ConfigureAwait(false);

                if (target.Count == 0)
                    break;
                last = target[^1].Id;

                Console.WriteLine($"Iterate {target.Count} items.");
                var targetId = target.Select(b => b.BuildUrl);
                var source = await (_mongoCollection.AsQueryable().Where(b => targetId.Contains(b.BuildUrl)) as IAsyncCursorSource<BuildEntry>).ToListAsync().ConfigureAwait(false);
                var joined = (from t in target
                              join s in source on t.BuildUrl equals s.BuildUrl
                              select (s, t)).ToList();
                foreach (var (s, t) in joined)
                {
                    //t.Branch = s.Branch;
                    //t.VcsRevision = s.VcsRevision;
                    //if (s.Why == null)
                    //    continue;
                    //t.Why = s.Why;
                    if (s.Workflows?.WorkflowName != null)
                    {
                        t.WorkflowName = s.Workflows.WorkflowName;
                    }
                }
                Console.WriteLine($"Updating {joined.Count} items.");
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                Console.WriteLine("Updated.");

                i += IterateCount;
                Console.WriteLine($"Updated {i} items. Last item: {last}.");
            }
        }

        public async Task TransferYamlFileAsync(string vcsUrl)
        {
            _logger.LogInformation($"Querying vcsurl: {vcsUrl}");
            var cursor = await _mongoCollection.AsQueryable(new AggregateOptions { AllowDiskUse = true }).Where(b => b.VcsUrl == vcsUrl).Select(b => new { b.BuildUrl, CircleYml = b.CircleYml! }).GroupBy(b => b.CircleYml).ToCursorAsync().ConfigureAwait(false);
            await foreach (var g in cursor.Wrap())
            {
                if (g.Key is null)
                    continue;

                var trimmedYaml = g.Key.String;

                // trim
                var idx1 = g.Key.String.IndexOf("# Original config.yml file:");
                if (idx1 == -1)
                {
                    // does not contain the line
                    _logger.LogInformation($"No need to trim yaml: {g.First().BuildUrl}");
                }
                else
                {
                    var idx2 = g.Key.String.LastIndexOf("# Original config.yml file:");
                    _logger.LogInformation($"Trimmed yaml: {g.First().BuildUrl}");
                    if (idx1 != idx2)
                    {
                        // find more than one line
                        _logger.LogError("Find more than one lines indicating original yaml file.");
                        throw new Exception("Find more than one lines indicating original yaml file.");
                    }
                    trimmedYaml = g.Key.String[..idx1];
                }

                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(trimmedYaml));
                var builds = g.Select(b => b.BuildUrl).ToList();
                _logger.LogInformation($"Importing {builds.Count} items... ({Convert.ToHexString(hash)})");
                await using var dbContext = _dbContextFactory.CreateDbContext();
                if (!await dbContext.CircleYmls.AsQueryable().AnyAsync(y => y.Content == trimmedYaml).ConfigureAwait(false))
                {
                    dbContext.CircleYmls.Add(new SqlModels.CircleYml
                    {
                        Sha256 = hash,
                        Content = g.Key.String,
                    });
                }
                var buildEntries = await dbContext.Builds.AsQueryable().Where(b => builds.Contains(b.BuildUrl)).ToListAsync().ConfigureAwait(false);
                _logger.LogInformation($"Fild {buildEntries.Count} items to associate yaml file.");
                foreach (var item in buildEntries)
                {
                    item.CircleYmlHash = hash;
                }
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation("Completed saving.");
            }
        }

        /// <summary>
        /// This is a mis-use.
        /// </summary>
        /// <returns></returns>
        public async Task TransferYamlFileOneByOneAllProjectsAsync()
        {
            List<string> vcsUrls;
            await using (ForecastingContext dbContext = _dbContextFactory.CreateDbContext())
            {
                vcsUrls = await dbContext.Builds.AsQueryable().Select(b => b.VcsUrl).Distinct().ToListAsync().ConfigureAwait(false);
            }
            foreach (string vcsUrl in vcsUrls)
            {
                await TransferYamlFileOneByOneAsync(vcsUrl).ConfigureAwait(false);
            }
        }

        public async Task TransferYamlFileOneByOneAsync(string vcsUrl)
        {
            _logger.LogInformation($"Querying vcsurl: {vcsUrl}");
            var count = await _mongoCollection.CountDocumentsAsync(b => b.VcsUrl == vcsUrl).ConfigureAwait(false);
            _logger.LogInformation($"Found {count} items in total.");

            var cursor = await _mongoCollection.AsQueryable(new AggregateOptions { AllowDiskUse = true }).Where(b => b.VcsUrl == vcsUrl).OrderBy(b => b.StartTime).ToCursorAsync().ConfigureAwait(false);
            string lastYaml = string.Empty;
            int i = 0;
            await foreach (var item in cursor.Wrap())
            {
                i++;
                _logger.LogInformation($"Processing {i} of {count}.");
                if (item.CircleYml == null)
                    continue;
                _logger.LogInformation($"Processing: {item.BuildUrl}");

                var trimmedYaml = item.CircleYml.String;
                bool trimmed = false;

                // trim
                var idx1 = item.CircleYml.String.IndexOf("# Original config.yml file:");
                if (idx1 == -1)
                {
                    // does not contain the line
                }
                else
                {
                    var idx2 = item.CircleYml.String.LastIndexOf("# Original config.yml file:");
                    if (idx1 != idx2)
                    {
                        // find more than one line
                        _logger.LogError("Find more than one lines indicating original yaml file.");
                        throw new Exception("Find more than one lines indicating original yaml file.");
                    }
                    trimmed = true;
                    trimmedYaml = item.CircleYml.String[..idx1];
                }

                // check same
                if (lastYaml != trimmedYaml)
                {
                    _logger.LogInformation($"Find yaml change. Build: {item.BuildUrl} ; Trimmed: {trimmed}");
                    lastYaml = trimmedYaml;
                }

                // hash yaml
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(trimmedYaml));

                // check yaml exist. add or validate
                await using var db = _dbContextFactory.CreateDbContext();
                var existingYaml = await db.CircleYmls.AsQueryable().FirstOrDefaultAsync(y => y.Sha256 == hash).ConfigureAwait(false);
                if (existingYaml == null)
                {
                    _logger.LogInformation($"Adding new yaml file. Build url: {item.BuildUrl} ; hash: {Convert.ToHexString(hash)}");
                    db.CircleYmls.Add(new SqlModels.CircleYml
                    {
                        Sha256 = hash,
                        Content = trimmedYaml,
                    });
                }
                else if (existingYaml.Content != trimmedYaml)
                {
                    _logger.LogError($"Find duplicated hash with different content. Build url: {item.BuildUrl} ; hash: {Convert.ToHexString(hash)}");
                    throw new Exception("Find duplicated hash with different content.");
                }

                // modify build's yaml hash
                var buildEntry = await db.Builds.AsQueryable().FirstOrDefaultAsync(b => b.BuildUrl == item.BuildUrl).ConfigureAwait(false);
                if (buildEntry != null)
                {
                    _logger.LogInformation($"Modifying hash. Build url: {item.BuildUrl}");
                    buildEntry.CircleYmlHash = hash;
                }
                else
                {
                    _logger.LogInformation($"Build not found. Build url: {item.BuildUrl}");
                }

                // save changes.
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task TransferNumberOfStepsAsync()
        {
            var cursor = await _mongoCollection.AsQueryable().Select(b => new { b.BuildUrl, NumberOfSteps = b.Steps.Count }).ToCursorAsync().ConfigureAwait(false);
            while (await cursor.MoveNextAsync().ConfigureAwait(false))
            {
                var builds = cursor.Current.Select(b => b.BuildUrl).ToList();

                await using var dbContext = _dbContextFactory.CreateDbContext();
                var dbBuilds = await dbContext.Builds.Where(b => builds.Contains(b.BuildUrl)).ToListAsync().ConfigureAwait(false);
                var join = from bs in dbBuilds
                           join bm in cursor.Current on bs.BuildUrl equals bm.BuildUrl
                           select new { bs, bm };
                foreach (var j in join)
                {
                    j.bs.NumberOfSteps = j.bm.NumberOfSteps;
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
