using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ForecastBuildTime.DataProcessing
{
    public class BuildConfigChangeDetector
    {
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
        private readonly ILogger<BuildConfigChangeDetector> _logger;

        public BuildConfigChangeDetector(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<BuildConfigChangeDetector> logger)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task DetectConfigChange(string vcsUrl)
        {
            await using ForecastingContext? db = _dbContextFactory.CreateDbContext();
            var combines = await db.Builds
                .AsQueryable()
                .Where(b => vcsUrl == b.VcsUrl)
                .Select(b => new { b.Branch, b.Why, b.JobName })
                .Distinct()
                .ToListAsync()
                .ConfigureAwait(false);
        }
    }
}
