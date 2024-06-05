using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using CsvHelper;
using ForecastBuildTime.Helpers;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime
{
    internal class Playground
    {
        private readonly MongoClient _mongoClient;
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
        private readonly ILogger<Playground> _logger;
        private readonly DirectoryHelper _directoryHelper;

        public Playground(MongoClient mongoClient, IMongoCollection<BuildEntry> mongoCollection,
            IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<Playground> logger, DirectoryHelper directoryHelper)
        {
            _mongoClient = mongoClient;
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _directoryHelper = directoryHelper;
        }

        public void Run()
        {
            var db = _mongoClient.GetDatabase("forecastBuildTime");
            var collection = db.GetCollection<BuildEntry>("cc_builds_2021");
            var result = collection.AsQueryable().Where(e => e.VcsUrl == "https://github.com/artsy/positron")
                .Select(e => e.Id).Take(111).ToList();
            foreach (var item in result)
            {
                Console.WriteLine(item);
            }
        }

        public async Task RunAsync()
        {
            var results = await (await _mongoCollection.AsQueryable().GroupBy(b => new { b.VcsUrl, b.Workflows!.JobName }).Select(g => new { Repository = g.Key.VcsUrl, JobName = g.Key.JobName, BuildCount = g.Count() }).ToCursorAsync()).ToListAsync();
            var csvPath = _directoryHelper.GetFullPath("long-tail.csv");
            await using var csvWriter = new CsvWriter(File.CreateText(csvPath), CultureInfo.InvariantCulture);
            await csvWriter.WriteRecordsAsync(results);
        }

        public async Task OAsync()
        {
            string vcsUrl = "https://github.com/numpy/numpy";
            string status = "success";

            var db = _mongoClient.GetDatabase("forecastBuildTime");
            var collection = db.GetCollection<BuildEntry>("cc_builds_2021");
            Console.WriteLine((await collection.AsQueryable().FirstOrDefaultAsync()).Branch);
            var result = await collection.AsQueryable()
                .Where(e => e.VcsUrl == vcsUrl && e.Status == status)
                .OrderByDescending(e => e.BuildTimeMillis)
                .Select(e => new { e.BuildUrl, e.Steps })
                .AsAsyncEnumerable()
                .Select(e => new
                {
                    e.BuildUrl,
                    BuildTime = e.Steps.Sum(s => (long?)s.Actions.First().RunTimeMillis) ?? 0,
                }).ToListAsync();
            ConsoleTable.From(result).Configure(o => o.NumberAlignment = Alignment.Right).Write(Format.Alternative);
        }

        public async Task TestMongoDriverContainsAsync()
        {
            var list = new List<string>
            {
                "https://circleci.com/gh/ajoberstar/ike.cljj/30", "https://circleci.com/gh/ajoberstar/ike.cljj/34",
            };
            var db = _mongoClient.GetDatabase("forecastBuildTime");
            var collection = db.GetCollection<BuildEntry>("cc_builds_2021");
            var count = await collection.AsQueryable().Where(b => list.Contains(b.BuildUrl)).CountAsync()
                .ConfigureAwait(false);
            Console.WriteLine(count);
        }
    }
}
