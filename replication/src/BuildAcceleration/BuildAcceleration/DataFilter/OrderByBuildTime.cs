using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataFilter
{
    public class OrderByBuildTime
    {
        private readonly IMongoCollection<BuildEntry> _b;

        public OrderByBuildTime(IMongoCollection<BuildEntry> b)
        {
            _b = b;
        }

        public async Task Run()
        {
            Console.WriteLine("Input repo name");
            while (Console.ReadLine() is string repo)
            {
                Console.WriteLine("Input job name");
                string? job_name = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(repo))
                    continue;
                var vcsUrl = "https://github.com/" + repo;
                var queryable = string.IsNullOrWhiteSpace(job_name)
                    ? _b.AsQueryable()
                         .Where(b => b.VcsUrl == vcsUrl && b.Status == "success" && b.Workflows != null)
                    : _b.AsQueryable()
                        .Where(b => b.VcsUrl == vcsUrl && b.Status == "success" && b.Workflows != null && b.Workflows.JobName == job_name);
                var result = await queryable
                    .AsAsyncEnumerable()
                    .Select(b => new { b.BuildTimeMillis, b.Id, b.Status, b.BuildUrl, b.Workflows.JobName })
                    .OrderByDescending(b => b.BuildTimeMillis ?? 0)
                    .ToListAsync();
                var i = 0;
                while (i < result.Count)
                {
                    for (int j = 0; j < 20; j++)
                    {
                        var current = result[i++];
                        Console.WriteLine($"{current.BuildUrl} : {current.BuildTimeMillis} {current.JobName}");
                        if (i >= result.Count)
                        {
                            break;
                        }
                    }

                    var s = Console.ReadLine();
                    if (s is null || s == "q")
                    {
                        break;
                    }
                }
            }
        }
    }
}