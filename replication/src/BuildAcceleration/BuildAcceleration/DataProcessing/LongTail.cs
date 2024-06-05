using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.Helpers;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace ForecastBuildTime.DataProcessing;

[Command]
internal class LongTail
{
    private readonly MongoClient _mongoClient;
    private readonly IMongoCollection<BuildEntry> _mongoCollection;
    private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
    private readonly ILogger<LongTail> _logger;
    private readonly DirectoryHelper _directoryHelper;

    [Option("-p|--path")]
    public string? CsvPath { get; set; }

    public LongTail(MongoClient mongoClient, IMongoCollection<BuildEntry> mongoCollection,
        IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<LongTail> logger, DirectoryHelper directoryHelper)
    {
        _mongoClient = mongoClient;
        _mongoCollection = mongoCollection;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _directoryHelper = directoryHelper;
    }

    public async Task OnExecuteAsync()
    {
        var results = await (await _mongoCollection.AsQueryable().GroupBy(b => new { b.VcsUrl, b.Workflows!.JobName }).Select(g => new { Repository = g.Key.VcsUrl, JobName = g.Key.JobName, BuildCount = g.Count() }).ToCursorAsync()).ToListAsync();
        var csvPath = string.IsNullOrWhiteSpace(CsvPath)
            ? _directoryHelper.GetFullPath("long-tail.csv")
            : Path.Combine(CsvPath, "long-tail.csv");
        await using var csvWriter = new CsvWriter(File.CreateText(csvPath), CultureInfo.InvariantCulture);
        await csvWriter.WriteRecordsAsync(results);
    }
}
