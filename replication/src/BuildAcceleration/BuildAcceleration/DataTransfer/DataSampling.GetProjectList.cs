using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataTransfer
{
    public static class EnumerableExtension
    {
        public static ArraySegment<T> TakeRandom<T>(this IEnumerable<T> source, int count)
        {
            var array = new T[count];
            int i = 0;
            var random = new Random();
            foreach (var item in source)
            {
                if (i++ < count)
                {
                    array[i - 1] = item;
                }
                else
                {
                    var replaceIndex = random.Next(i);
                    if (replaceIndex < count)
                    {
                        array[replaceIndex] = item;
                    }
                }
            }

            return i >= count ? array : array[0..i];
        }
    }

    public partial class DataSampling
    {
        private async Task<List<string>> GetProjectsAsync()
        {
            var projects = await _mongoCollection.AsQueryable().Select(b => b.VcsUrl).Distinct().ToListAsync().ConfigureAwait(false);
            return projects;
        }

        private async Task<IList<string>> GetSampledBuildsInProject(string vcsUrl)
        {
            IList<string> builds = await _mongoCollection
                .AsQueryable()
                .Where(b => b.VcsUrl == vcsUrl && b.Status == "success" && b.Workflows != null)
                .Select(b => b.BuildUrl)
                .ToListAsync().ConfigureAwait(false);
            if (builds.Count > 10000)
            {
                builds = builds.TakeRandom(10000);
            }
            return builds;
        }
    }
}
