using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ForecastBuildTime.SqlModels;
using MathNet.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.Algorithms
{
    public class Predictor
    {
        private readonly IDbContextFactory<ForecastingContext> _forecastingContextFactory;
        private readonly ILogger<Predictor> _logger;

        public Predictor(IDbContextFactory<ForecastingContext> forecastingContextFactory, ILogger<Predictor> logger)
        {
            _forecastingContextFactory = forecastingContextFactory;
            _logger = logger;
        }

        public async Task SimpleAverageAsync(string vcsUrl, string jobName)
        {
            const string approach = "simple-average";
            await using var forecastingContext = _forecastingContextFactory.CreateDbContext();
            var oldEval = await forecastingContext.Evaluations.Where(e => e.VcsUrl == vcsUrl && e.JobName == jobName && e.Approach == approach).FirstOrDefaultAsync().ConfigureAwait(false);
            if (oldEval is not null)
                return;

            var items = await forecastingContext.Builds.Where(b => b.VcsUrl == vcsUrl && b.JobName == jobName).ToListAsync().ConfigureAwait(false);

            var avg = items.Average(b => b.SumOfBuildTimeInSteps.TotalMilliseconds);
            var mmre = items.Average(b => Math.Abs(avg - b.SumOfBuildTimeInSteps.TotalMilliseconds) / b.SumOfBuildTimeInSteps.TotalMilliseconds);

            _logger.LogInformation($"Complete calculation for {vcsUrl}, approach: {approach}, {items.Count} items.");

            var predictionResult = JsonSerializer.SerializeToUtf8Bytes(new { average = avg });

            forecastingContext.Evaluations.Add(new PredictionEvaluation
            {
                Approach = approach,
                VcsUrl = vcsUrl,
                JobName = jobName,
                MMRE = mmre,
                RSquared = 0,
                BuildCount = items.Count,
                //PredictedValues = JsonDocument.Parse(pipe.Reader.AsStream()),
                PredictedValues = JsonDocument.Parse(predictionResult),
            });
            await forecastingContext.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Completed saving result.");
        }

        public async Task SimpleLinearRegressionAsync(string vcsUrl, string jobName)
        {
            const string approach = "simple-linear-regression";
            await using var forecastingContext = _forecastingContextFactory.CreateDbContext();
            var oldEval = await forecastingContext.Evaluations.Where(e => e.VcsUrl == vcsUrl && e.JobName == jobName && e.Approach == approach).FirstOrDefaultAsync().ConfigureAwait(false);
            if (oldEval is not null)
                return;

            var items = await forecastingContext.Builds.Where(b => b.VcsUrl == vcsUrl && b.JobName == jobName).ToListAsync().ConfigureAwait(false);
            if (items.Count < 2)
            {
                // the algorithm needs at least two points.
                return;
            }

            var data = items.Select(b =>
                            (b.StartTime,
                             Elapsed: b.SumOfBuildTimeInSteps.TotalMilliseconds));
            var x = data.Select(b => (double)b.StartTime.ToUnixTimeMilliseconds()).ToArray();
            var y = data.Select(b => b.Elapsed).ToArray();
            var (intercept, slope) = Fit.Line(x, y);
            var mmre = data.Sum(b => Math.Abs(slope * b.StartTime.ToUnixTimeMilliseconds() + intercept - b.Elapsed) / b.Elapsed) / items.Count;
            // calculate r squared
            var rSquared = 1 - (data.Sum(b => Math.Pow(slope * b.StartTime.ToUnixTimeMilliseconds() + intercept - b.Elapsed, 2)) / data.Sum(b => Math.Pow(b.Elapsed - data.Average(c => c.Elapsed), 2)));

            _logger.LogInformation($"Complete calculation for {vcsUrl}, approach: {approach}, {items.Count} items.");
            _logger.LogInformation($"intercept: {intercept}, slope: {slope}");

            var predictionResult = JsonSerializer.SerializeToUtf8Bytes(new { intercept, slope });
            //var pipe = new System.IO.Pipelines.Pipe();
            //var writer = new Utf8JsonWriter(pipe.Writer);
            //JsonSerializer.Serialize(writer, new { intercept, slope });

            forecastingContext.Evaluations.Add(new PredictionEvaluation
            {
                Approach = approach,
                VcsUrl = vcsUrl,
                JobName = jobName,
                MMRE = mmre,
                RSquared = rSquared,
                BuildCount = items.Count,
                //PredictedValues = JsonDocument.Parse(pipe.Reader.AsStream()),
                PredictedValues = JsonDocument.Parse(predictionResult),
            });
            await forecastingContext.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Completed saving result.");
        }

        public async Task SlidingWindowAverageAsync(string vcsUrl, string jobName, int days = 30)
        {
            string approach = $"window-size-average-{days}-days";
            await using var forecastingContext = _forecastingContextFactory.CreateDbContext();
            var oldEval = await forecastingContext.Evaluations.Where(e => e.VcsUrl == vcsUrl && e.JobName == jobName && e.Approach == approach).FirstOrDefaultAsync().ConfigureAwait(false);
            if (oldEval is not null)
                return;

            var builds = await forecastingContext.Builds.Where(b => b.VcsUrl == vcsUrl && b.JobName == jobName)
                .OrderBy(b => b.StartTime)
                .Select(b => new { b.StartTime, b.SumOfBuildTimeInSteps })
                .ToArrayAsync().ConfigureAwait(false);

            var selectedBuilds = builds;
            var forecasted = new TimeSpan?[selectedBuilds.Length];
            var slidingWindowStart = 0;
            for (int i = 1; i < selectedBuilds.Length; i++)
            {
                while (builds[slidingWindowStart].StartTime < selectedBuilds[i].StartTime - TimeSpan.FromDays(days))
                {
                    slidingWindowStart++;
                }

                if (builds[slidingWindowStart].StartTime >= selectedBuilds[i].StartTime)
                {
                    continue;
                }

                var predictedByAverageMillis = builds[slidingWindowStart..]
                    .TakeWhile(b => b.StartTime < selectedBuilds[i].StartTime)
                    .Average(b => b.SumOfBuildTimeInSteps.TotalMilliseconds);

                forecasted[i] = TimeSpan.FromMilliseconds(predictedByAverageMillis);
            }

            var zipped = selectedBuilds.Zip(forecasted, (o, f) => new
            {
                o.StartTime,
                BuildTime = o.SumOfBuildTimeInSteps,
                ForecastedBuildTime = f,
            }).Where(z => z.ForecastedBuildTime != null).ToList();

            var countOfForecasted = zipped.Count;
            var sumOfMre = zipped
                .Sum(z => Math.Abs((z.ForecastedBuildTime!.Value - z.BuildTime).TotalSeconds) / z.BuildTime.TotalSeconds);
            var mmre = sumOfMre / countOfForecasted;
            // calculate r squared
            var averageSeconds = countOfForecasted > 0 ? selectedBuilds.Average(b => b.SumOfBuildTimeInSteps.TotalSeconds) : double.NaN;
            var rSquared = 1 - zipped.Sum(z => Math.Pow((z.ForecastedBuildTime!.Value - z.BuildTime).TotalSeconds, 2))
                / zipped.Sum(z => Math.Pow(z.BuildTime.TotalSeconds - averageSeconds, 2));

            _logger.LogInformation($"Complete calculation for {vcsUrl}, approach: {approach}, {builds.Length} items.");

            //var predictionResult = JsonSerializer.SerializeToUtf8Bytes(new { intercept, slope });
            //var pipe = new System.IO.Pipelines.Pipe();
            //var writer = new Utf8JsonWriter(pipe.Writer);
            //JsonSerializer.Serialize(writer, new { intercept, slope });

            forecastingContext.Evaluations.Add(new PredictionEvaluation
            {
                Approach = approach,
                VcsUrl = vcsUrl,
                JobName = jobName,
                MMRE = mmre,
                RSquared = rSquared,
                BuildCount = builds.Length,
                //PredictedValues = JsonDocument.Parse(pipe.Reader.AsStream()),
                //PredictedValues = JsonDocument.Parse(predictionResult),
            });
            await forecastingContext.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Completed saving result.");
        }
    }
}
