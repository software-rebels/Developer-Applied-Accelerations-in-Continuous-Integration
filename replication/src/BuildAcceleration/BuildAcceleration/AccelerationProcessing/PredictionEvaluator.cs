using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using ForecastBuildTime.Helpers;

namespace ForecastBuildTime.AccelerationProcessing;

internal class PredictionEvaluator
{
    private readonly DirectoryHelper _directoryHelper;

    public PredictionEvaluator(DirectoryHelper directoryHelper)
    {
        _directoryHelper = directoryHelper;
    }

    public async ValueTask EvaluatePrediction()
    {
        (List<Inspection> inspections, List<Detection> ratios) = await ReadCsvAsync().ConfigureAwait(false);
        var results = GetGroundTruth(inspections, ratios);

        Evaluate(results, out List<(double threshold, double? precision)> precisionList, out List<(double threshold, double recall)> recallList);

        await using var w1 = File.CreateText(_directoryHelper.GetFullPath("recall.csv"));
        await using var csvWriterRecall = new CsvWriter(w1, CultureInfo.InvariantCulture);
        await csvWriterRecall.WriteRecordsAsync(recallList.Select(t => new { t.threshold, t.recall }))
            .ConfigureAwait(false);
        await using var w2 = File.CreateText(_directoryHelper.GetFullPath("precision.csv"));
        await using var csvWriterPrecision = new CsvWriter(w2, CultureInfo.InvariantCulture);
        await csvWriterPrecision.WriteRecordsAsync(precisionList.Select(t => new { t.threshold, t.precision }))
            .ConfigureAwait(false);
    }

    public async ValueTask EvaluateSensitivity()
    {
        (List<Inspection> inspections, List<Detection> ratios) = await ReadCsvAsync();

        var f1List = Enumerable.Range(1, 10).Select(i =>
        {
            var proportion = i / 10.0;
            var ground = GetGroundTruth(inspections, ratios, proportion);
            var f1 = Evaluate(ground, out _, out _);
            return new
            {
                Proportion = proportion,
                Count = ground.Count,
                F1 = f1,
            };
        }).ToList();

        ConsoleTables.ConsoleTable.From(f1List).Write();
    }

    private static double Evaluate(IList<(double ratio, bool positive)> groundTruth, out List<(double threshold, double? precision)> precisionList, out List<(double threshold, double recall)> recallList)
    {
        double maxRatio = groundTruth.Max(r => r.ratio); // higher, less likely to accel
        double maxF1Score = 0;
        precisionList = new();
        recallList = new();
        int truePositive = groundTruth.Count(t => t.positive);
        for (int i = 1; i <= maxRatio * 100 + 1; i++)
        {
            double threshold = i / 100.0;
            var detectedPositive = groundTruth.Where(t => t.ratio <= threshold).ToList();
            double recall = detectedPositive.Count(t => t.positive) / (double)truePositive;
            double? precision = null;
            recallList.Add((threshold, recall));
            if (detectedPositive.Count != 0)
            {
                precision = detectedPositive.Count(t => t.positive) / (double)detectedPositive.Count;
            }

            precisionList.Add((threshold, precision));

            precision ??= double.NaN;
            double f1 = 2 * precision.Value * recall / (precision.Value + recall);
            if (f1 > maxF1Score)
            {
                maxF1Score = f1;
            }
        }
        return maxF1Score;
    }

    private static IList<(double ratio, bool positive)> GetGroundTruth(IList<Inspection> inspections, List<Detection> ratios, double proportion = 1)
    {
        var inspTake = proportion == 1
            ? inspections
            : inspections.OrderByDescending(i => i.CountByMonth).Take((int)(inspections.Count * proportion));
        var results = new List<(double ratio, bool positive)>();
        foreach (var insp in inspTake)
        {
            if (insp.ResultBool == null)
            {
                continue;
            }

            bool positive = insp.ResultBool.Value;

            results.Add((ratios.First(d => d.VcsUrl == insp.VcsUrl && d.JobName == insp.JobName).RatioWeightedMiddle,
                positive));
        }
        return results;
    }

    internal async ValueTask<(List<Inspection> inspections, List<Detection> ratios)> ReadCsvAsync()
    {
        string inspectionPath = _directoryHelper.GetFullPath("inspection_agreed.csv");
        string ratioPath = _directoryHelper.GetFullPath("KMeansClusterRatio.csv");
        using var r1 = new StreamReader(inspectionPath);
        using var inspCsv = new CsvReader(r1, CultureInfo.InvariantCulture);
        var inspections = await inspCsv.GetRecordsAsync<Inspection>()
            // .Where(insp => insp.IsAgreed == true) // only use agreed jobs
            .ToListAsync().ConfigureAwait(false);
        using var r2 = File.OpenText(ratioPath);
        using var ratioCsv = new CsvReader(r2, CultureInfo.InvariantCulture);
        var ratios = await ratioCsv.GetRecordsAsync<Detection>().ToListAsync().ConfigureAwait(false);
        return (inspections, ratios);
    }

    internal class Inspection
    {
        public string VcsUrl { get; set; }
        public string JobName { get; set; }
        [Name("formalizedResults")] public string Result { get; set; }
        [Name("agreed")] public string Agreed { get; set; }
        [Name("Count/Month")]
        public int CountByMonth { get; set; }

        public bool? ResultBool => Result switch
        {
            "no" or "p1" => false,
            "p2" or "yes" => true,
            _ => null,
        };

        public bool? IsAgreed => Agreed switch
        {
            "agreed" => true,
            "conflicted" => false,
            _ => null,
        };
    }

    internal class Detection
    {
        public string VcsUrl { get; set; }
        public string JobName { get; set; }
        [Name("WeightedMiddle")] public double RatioWeightedMiddle { get; set; }
    }

    private record Aggregation(string VcsUrl, string JobName, bool? IsAccelerated, double Ratio);
}
