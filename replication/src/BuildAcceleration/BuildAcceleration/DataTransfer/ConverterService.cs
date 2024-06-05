using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataTransfer
{
    public class ConverterService
    {
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;

        public ConverterService(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
        }

        public async Task ConvertAsync()
        {
            await using var writer = TextWriter.Synchronized(File.AppendText($"/output/{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));
            var queue = new BlockingCollection<IEnumerable<BuildEntry>>(new ConcurrentQueue<IEnumerable<BuildEntry>>(), 10);
            Func<int, Task> runnerFunc = async id =>
            {
                foreach (var batch in queue.GetConsumingEnumerable())
                {
                    var converted = new List<BuildEntrySlim>();
                    foreach (var item in batch)
                    {
                        try
                        {
                            var currentConverted = item.SuccessToSlim(out var hash);
                            converted.Add(currentConverted);
                        }
                        catch (Exception e)
                        {
                            await writer.WriteLineAsync($"Convert failed: {item.BuildUrl}").ConfigureAwait(false);
                            await writer.WriteLineAsync(e.ToString()).ConfigureAwait(false);
                        }
                    }
                    try
                    {
                        await using var dbContext = _dbContextFactory.CreateDbContext();
                        dbContext.Builds.AddRange(converted);
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                        await writer.WriteLineAsync($"{id}: Saved {converted.Count} items.").ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        await writer.WriteLineAsync(e.ToString()).ConfigureAwait(false);
                        await writer.WriteLineAsync("Saving to PostgreSQL failed!").ConfigureAwait(false);
                        await writer.WriteLineAsync("Failed builds:").ConfigureAwait(false);
                        foreach (var c in converted)
                        {
                            await writer.WriteLineAsync(c.BuildUrl).ConfigureAwait(false);
                        }
                    }
                }
            };
            var addingTasks = Enumerable.Range(0, 4).Select(i => Task.Run(() => runnerFunc(i))).ToArray();
            var cursor = await _mongoCollection.AsQueryable().Where(b => b.Status == "success" && b.Workflows != null).ToCursorAsync().ConfigureAwait(false);
            //if (cursor.Current != null)
            //{
            //    queue.Add(cursor.Current.ToList());
            //}

            while (await cursor.MoveNextAsync().ConfigureAwait(false))
            {
                await writer.WriteLineAsync($"Batch for count: {cursor.Current.Count()}").ConfigureAwait(false);
                queue.Add(cursor.Current.ToList());
            }

            queue.CompleteAdding();

            await Task.WhenAll(addingTasks).ConfigureAwait(false);
        }
    }
}
