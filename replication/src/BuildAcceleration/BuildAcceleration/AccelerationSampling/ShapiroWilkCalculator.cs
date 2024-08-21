using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Python.Runtime;

namespace ForecastBuildTime.AccelerationSampling;

public class ShapiroWilkCalculator
{
    private readonly ForecastingContext _forecastingContext;

    public ShapiroWilkCalculator(ForecastingContext forecastingContext)
    {
        _forecastingContext = forecastingContext;
    }

    public async Task CalculateAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Runtime.PythonDLL = @"C:\Users\yinmi\AppData\Local\Programs\Python\Python310\python310.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Runtime.PythonDLL = "/lib/aarch64-linux-gnu/libpython3.8.so";
        }
        else
        {
            throw new NotImplementedException();
        }

        var samples = await _forecastingContext.AccelerationSamples.ToListAsync().ConfigureAwait(false);
        var resultList = MakeListOfInstanceType(new
        {
            VcsUrl = default(string)!,
            JobName = default(string)!,
            Month = default(string)!,
            Statistic = default(double),
            PValue = default(double),
            Count = default(int)
        });

        PythonEngine.Initialize();
        using var _gil = Py.GIL();
        // PythonNet can't be used in async context.
        foreach (var sample in samples)
        {
            Console.WriteLine($"{sample.VcsUrl} {sample.JobName}");

            var buildTimeMonths = _forecastingContext.Builds
                .Where(b => b.VcsUrl == sample.VcsUrl && b.JobName == sample.JobName)
                .Select(b => new { b.SumOfBuildTimeInSteps, b.StartTime })
                .AsEnumerable()
                .GroupBy(b => (b.StartTime.Year, b.StartTime.Month))
                .OrderBy(g => (g.Key.Year << 4) + g.Key.Month)
                .ToList();

            foreach (var mo in buildTimeMonths)
            {
                var (year, month) = mo.Key;
                var buildTimes = mo.Select(b => b.SumOfBuildTimeInSteps.TotalSeconds).ToArray();
                if (buildTimes.Length < 3)
                {
                    continue;
                }

                dynamic stats = Py.Import("scipy.stats");
                dynamic shapiro_test = stats.shapiro(buildTimes);
                var result = new ShapiroWilk
                {
                    W = shapiro_test.statistic,
                    P = shapiro_test.pvalue
                };
                sample.ShapiroWilk = result;

                Console.WriteLine($"{sample.VcsUrl} {sample.JobName} {year:0000}-{month:00} {result.W:0.00} {result.P:0.00} {buildTimes.Length}");
                resultList.Add(new
                {
                    VcsUrl = sample.VcsUrl,
                    JobName = sample.JobName,
                    Month = $"{year:0000}-{month:00}",
                    Statistic = result.W,
                    PValue = result.P,
                    Count = buildTimes.Length
                });
            }

        }

        await using var csvWriter = new CsvWriter(File.CreateText("ShapiroWilkTest.csv"), CultureInfo.InvariantCulture);

        csvWriter.WriteRecords(resultList);

        Console.WriteLine("Skipping save to db.");
    }

    private static List<T> MakeListOfInstanceType<T>(T instance)
    {
        return new List<T>();
    }
}
