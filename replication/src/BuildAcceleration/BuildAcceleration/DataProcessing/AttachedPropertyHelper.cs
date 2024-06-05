using System;
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
    public class AttachedPropertyHelper
    {
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
        private readonly ILogger<AttachedPropertyHelper> _logger;

        public AttachedPropertyHelper(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<AttachedPropertyHelper> logger)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }


        public async Task CalculateBuildNumOfDayAsync(string vcsUrl)
        {
            List<string> jobList;
            await using (ForecastingContext dbContext = _dbContextFactory.CreateDbContext())
                jobList = await dbContext.Builds.AsQueryable().Where(b => b.VcsUrl == vcsUrl).Select(b => b.JobName).Distinct().ToListAsync().ConfigureAwait(false);
            _logger.LogInformation($"Find {jobList.Count} jobs.");
            foreach (string job in jobList)
            {
                _logger.LogInformation($"Job {job}.");
                await using ForecastingContext dbContext = _dbContextFactory.CreateDbContext();
                List<BuildEntrySlim> builds = await dbContext.Builds.AsQueryable()
                    .Where(b => b.VcsUrl == vcsUrl && b.JobName == job)
                    .OrderBy(b => b.StartTime)
                    .ToListAsync().ConfigureAwait(false);
                DateTime currentDate = default;
                int count = default;
                foreach (BuildEntrySlim b in builds)
                {
                    if (b.StartTime.Date != currentDate)
                    {
                        currentDate = b.StartTime.Date;
                        count = 0;
                    }
                    count++;
                    b.AttachedProperties = new Dictionary<string, object>(b.AttachedProperties)
                    {
                        ["build_of_day"] = count,
                    };
                }
                _ = await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
