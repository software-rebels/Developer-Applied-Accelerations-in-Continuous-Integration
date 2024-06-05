using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.MongoDBModels;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataFilter
{
    public class ListAllRepos
    {
        private readonly IMongoCollection<BuildEntry> _b;

        public ListAllRepos(IMongoCollection<BuildEntry> b)
        {
            _b = b;
        }

        public async Task ListAsync()
        {
            Dictionary<string, GroupInfo> dic = new Dictionary<string, GroupInfo>();
            string recentUrl = string.Empty;
            var list = new[]
            {
                new
                {
                    VcsUrl = "",
                    Yml = 0,
                    Parallel = 0,
                    BuildTimeFromSteps = 0L,
                    BuildTimeFromDifference = 0L,
                }
            }.ToList();
            list.Clear();
            await _b.AsQueryable()
                .Where(b => b.Platform == "2.0" && b.StartTime != null && b.StopTime != null &&
                            b.Status != "success")
                .OrderBy(b => b.VcsUrl)
                .Select(b => new
                {
                    b.VcsUrl,
                    b.CircleYml,
                    b.Steps,
                    b.Parallel,
                    b.StartTime,
                    b.StopTime,
                })
                .AsAsyncEnumerable()
                .ForEachAsync((b, index) =>
                {
                    if (index % 10000 == 0)
                    {
                        Console.WriteLine(index);
                    }
                    
                    // if (!dic.ContainsKey(b.VcsUrl))
                    // {
                    //     dic.Add(b.VcsUrl, new GroupInfo {Repository = b.VcsUrl});
                    // }
                    //
                    // var group = dic[b.VcsUrl];
                    // group.BuildCount++;
                    // if (group.MaxBuildTimeMillis < (long) (b.StopTime - b.StartTime)!.Value.TotalMilliseconds)
                    // {
                    //     group.MaxBuildTimeMillis = (long) (b.StopTime - b.StartTime)!.Value.TotalMilliseconds;
                    // }
                    //
                    // if (b.CircleYml?.String != null)
                    //     group.YmlHashs.Add(b.CircleYml.String.GetHashCode());

                    if (b.VcsUrl != recentUrl)
                    {
                        if (!string.IsNullOrEmpty(recentUrl))
                        {
                            Console.WriteLine($"Complete {recentUrl}");
                            var buildTime = list.Select(i => i.BuildTimeFromSteps).OrderByDescending(i => i).ToList();
                            dic.Add(recentUrl, new GroupInfo
                            {
                                BuildCount = list.Count,
                                MaxBuildTimeMillisFromDifference = list.Max(i => i.BuildTimeFromDifference),
                                MaxBuildTimeMillisFromSteps = list.Max(i => i.BuildTimeFromSteps),
                                BuildTime_90 = buildTime[(int)(buildTime.Count * 0.1)],
                                BuildTime_50 = buildTime[(int)(buildTime.Count * 0.5)],
                                Repository = recentUrl,
                                YmlCount = list.Select(i => i.Yml).Distinct().Count(),
                                ParallelBuildCount = list.Count(i => i.Parallel > 1),
                            });
                            list.Clear();
                            // Console.WriteLine(JsonSerializer.Serialize(dic[recentUrl],
                            //     new JsonSerializerOptions {WriteIndented = true}));
                        }

                        recentUrl = b.VcsUrl;
                        Console.WriteLine($"Starting {b.VcsUrl}");
                    }

                    if (b.StartTime == null || b.StopTime == null || b.CircleYml?.String == null)
                    {
                        return;
                    }

                    list.Add(new
                    {
                        VcsUrl = b.VcsUrl,
                        Yml = b.CircleYml.String.GetHashCode(),
                        Parallel = b.Parallel,
                        BuildTimeFromSteps = b.Steps.Sum(s => (long?) s.Actions.FirstOrDefault()?.RunTimeMillis) ?? 0,
                        BuildTimeFromDifference = (long) (b.StopTime - b.StartTime).Value.TotalMilliseconds,
                    });
                });
            new CsvWriter(Console.Out, CultureInfo.InvariantCulture, true).WriteRecords(dic.Values);
        }

        private class GroupInfo
        {
            public string Repository { get; set; }
            public int BuildCount { get; set; }
            public long MaxBuildTimeMillisFromDifference { get; set; }
            public long MaxBuildTimeMillisFromSteps { get; set; }
            public long BuildTime_90 { get; set; }
            public long BuildTime_50 { get; set; }
            public int YmlCount { get; set; }
            public int ParallelBuildCount { get; set; }
        }
    }
}