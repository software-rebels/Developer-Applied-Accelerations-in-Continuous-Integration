using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataTransfer
{
    public partial class DataSampling
    {
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;

        public DataSampling(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
        }

        public async Task SampleAsync()
        {
            var projects = await GetProjectsAsync().ConfigureAwait(false);
            await using var writer = TextWriter.Synchronized(File.AppendText($"/output/{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));

            foreach (var vcsUrl in projects)
            {
                var builds = await GetSampledBuildsInProject(vcsUrl);
                await using var dbContext = _dbContextFactory.CreateDbContext();
                var buildsToMark = await dbContext.Builds.AsQueryable().Where(b => builds.Contains(b.BuildUrl)).ToListAsync().ConfigureAwait(false);
                foreach (var item in buildsToMark)
                {
                    item.SelectedSuccess = true;
                }
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                await writer.WriteLineAsync($"Sampled {builds.Count} in {vcsUrl}").ConfigureAwait(false);
            }
        }
    }
}
