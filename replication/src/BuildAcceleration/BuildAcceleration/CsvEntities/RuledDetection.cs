using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.Helpers;

namespace ForecastBuildTime.CsvEntities;

public record RuledDetection(string VcsUrl, string JobName, ulong Random, int NonZeroBuildCount, int MaxNonZeroBuildCountPerMonth, bool HitRule)
{
    internal static async ValueTask<List<RuledDetection>> ReadAsync(DirectoryHelper directoryHelper)
    {
        using var csvReader = new CsvReader(new StreamReader(directoryHelper.GetFullPath("ruled_detection.csv")), CultureInfo.InvariantCulture);
        return await csvReader.GetRecordsAsync<RuledDetection>().ToListAsync();
    }

    internal static async ValueTask WriteAsync(DirectoryHelper directoryHelper, IEnumerable<RuledDetection> records)
    {
        await using var csvWriter =
            new CsvWriter(File.CreateText(directoryHelper.GetFullPath("ruled_detection.csv")), CultureInfo.InvariantCulture);
        await csvWriter.WriteRecordsAsync(records);
    }
}