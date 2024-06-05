using System;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using ForecastBuildTime.MongoDBModels;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataFilter
{
    public class MonthlyEmptyStepBuilds
    {
        private readonly IMongoCollection<BuildEntry> _b;

        public MonthlyEmptyStepBuilds(IMongoCollection<BuildEntry> b)
        {
            _b = b;
        }

        public async Task Run()
        {
            Console.WriteLine("Input repo name");
            while (Console.ReadLine() is string repo)
            {
                if (string.IsNullOrWhiteSpace(repo)) continue;
                var vcsUrl = "https://github.com/" + repo;
                var result = await _b.AsQueryable()
                    .Where(b => b.VcsUrl == vcsUrl)
                    .ToCursorAsync()
                    .Wrap()
                    .Select(b => (b.QueuedAt, Steps_Count: b.Steps.Count, b.Platform))
                    .GroupBy(b => b.QueuedAt.ToString("yyyy-MM"))
                    .SelectAwait(async g => new
                    {
                        Month = g.Key,
                        EmptySteps = await g.CountAsync(b => b.Steps_Count == 0),
                        NonEmptySteps = await g.CountAsync(b => b.Steps_Count != 0),
                        Platform1 = await g.CountAsync(b => b.Platform == "1.0"),
                        Platform2 = await g.CountAsync(b => b.Platform == "2.0"),
                    })
                    .OrderBy(g => g.Month)
                    .ToListAsync();
                
                ConsoleTable.From(result).Configure(o => o.NumberAlignment = Alignment.Right).Write(Format.Alternative);
            }
        }
    }
}