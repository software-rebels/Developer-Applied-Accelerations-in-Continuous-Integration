using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;

namespace ForecastBuildTime.AccelerationSampling;

public class PrintSample
{
    private readonly ForecastingContext _forecastingContext;

    public PrintSample(ForecastingContext forecastingContext)
    {
        _forecastingContext = forecastingContext;
    }

    public async ValueTask PrintSamples()
    {
        var samples = await _forecastingContext.AccelerationSamples.AsNoTracking()
            .ToListAsync().ConfigureAwait(false);

        await using var writer = File.CreateText("accel_samples.csv");
        await using var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

        await csv.WriteRecordsAsync(samples).ConfigureAwait(false);
    }
}