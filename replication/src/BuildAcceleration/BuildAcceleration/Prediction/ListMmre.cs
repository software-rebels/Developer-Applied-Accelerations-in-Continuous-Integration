using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.Prediction
{
    public class ListMmre
    {
        private readonly ForecastingContext _forecastingContext;
        private readonly ILogger<PredictingWrapper> _logger;

        public ListMmre(ForecastingContext forecastingContext, ILogger<PredictingWrapper> logger)
        {
            _forecastingContext = forecastingContext;
            _logger = logger;
        }

        public async Task ListAsync()
        {
            var evaluations = await _forecastingContext.Evaluations.AsQueryable().ToListAsync();
            var records = evaluations.GroupBy(e => new { e.VcsUrl, e.JobName }).Select(g =>
            {
                var dic = new Dictionary<string, object>
                {
                    [nameof(g.Key.VcsUrl)] = g.Key.VcsUrl,
                    [nameof(g.Key.JobName)] = g.Key.JobName,
                    [nameof(PredictionEvaluation.BuildCount)] = g.Max(e => e.BuildCount),
                };
                foreach (var e in g)
                {
                    dic[e.Approach + "_MMRE"] = e.MMRE;
                    dic[e.Approach + "_RSquared"] = e.RSquared;
                }
                return dic;
            });

            await using var csvWriter = new CsvWriter(File.CreateText("mmre.csv"), CultureInfo.InvariantCulture);
            await csvWriter.WriteDictionaryRecordsAsync(records).ConfigureAwait(false);
        }

        public async Task ListRSquaredAsync()
        {
            // get all projects
            var projects = await _forecastingContext.Evaluations.Select(e => e.VcsUrl).Distinct().ToListAsync().ConfigureAwait(false);
            var records = new List<Dictionary<string, object>>();
            foreach (var project in projects)
            {
                var evals = await _forecastingContext.Evaluations.Where(e => e.VcsUrl == project).ToListAsync().ConfigureAwait(false);
                var jobs = evals.GroupBy(e => e.JobName);
                foreach (var job in jobs)
                {
                    var dic = new Dictionary<string, object>
                    {
                        [nameof(PredictionEvaluation.VcsUrl)] = project,
                        [nameof(PredictionEvaluation.JobName)] = job.Key,
                        [nameof(PredictionEvaluation.BuildCount)] = job.Max(e => e.BuildCount),
                    };
                    foreach (var e in job)
                    {
                        dic[e.Approach + "_Count"] = e.BuildCount;
                        dic[e.Approach + "_MMRE"] = e.MMRE;
                        dic[e.Approach + "_RSquared"] = e.RSquared;
                    }
                    records.Add(dic);
                }

                var approaches = evals.GroupBy(e => e.Approach);
                var dic2 = new Dictionary<string, object>
                {
                    [nameof(PredictionEvaluation.VcsUrl)] = project,
                    [nameof(PredictionEvaluation.JobName)] = "All",
                    [nameof(PredictionEvaluation.BuildCount)] = approaches.Max(a => a.Sum(e => e.BuildCount)),
                };
                foreach (var a in approaches)
                {
                    dic2[a.Key + "_Count"] = a.Sum(e => e.BuildCount);
                    dic2[a.Key + "_MMRE"] = GetMiddle(a.Select(e => (e.MMRE, e.BuildCount)).ToList());
                    dic2[a.Key + "_RSquared"] = GetMiddle(a.Select(e => (e.RSquared, e.BuildCount)).ToList());
                }
                records.Add(dic2);
            }
            var dicAll = new Dictionary<string, object>
            {
                [nameof(PredictionEvaluation.VcsUrl)] = "All",
                [nameof(PredictionEvaluation.JobName)] = "All",
                [nameof(PredictionEvaluation.BuildCount)] = await _forecastingContext.Evaluations.SumAsync(e => e.BuildCount).ConfigureAwait(false),
            };
            var approachNames = await _forecastingContext.Evaluations
                .Select(e => e.Approach)
                .Distinct()
                .ToListAsync().ConfigureAwait(false);
            foreach (var a in approachNames)
            {
                var evals = await _forecastingContext.Evaluations
                    .Where(e => e.Approach == a)
                    .Select(e => new { e.MMRE, e.BuildCount, e.RSquared })
                    .ToListAsync().ConfigureAwait(false);
                dicAll[a + "_Count"] = evals.Sum(e => e.BuildCount);
                dicAll[a + "_MMRE"] = GetMiddle(evals.Select(e => (e.MMRE, e.BuildCount)).ToList());
                dicAll[a + "_RSquared"] = GetMiddle(evals.Select(e => (e.RSquared, e.BuildCount)).ToList());
            }
            records.Add(dicAll);

            await using var csvWriter = new CsvWriter(File.CreateText("rsquared.csv"), CultureInfo.InvariantCulture);
            await csvWriter.WriteDictionaryRecordsAsync(records).ConfigureAwait(false);

        }

        private double GetMiddle(IList<(double, int)> values)
        {
            values = values.Where(t => !double.IsNaN(t.Item1)).OrderBy(v => v.Item1).ToArray();
            if (values.Count == 0)
            {
                return double.NaN;
            }
            int exceeding = 0;
            int i = 0;
            int j = values.Count - 1;
            while (i < j)
            {
                if (exceeding <= 0)
                {
                    exceeding += values[i].Item2;
                    i++;
                }
                else
                {
                    exceeding -= values[j].Item2;
                    j--;
                }
            }
            Debug.Assert(i == j);
            if (exceeding - values[i].Item2 > 0)
            {
                return values[i - 1].Item1;
            }
            else if (exceeding + values[i].Item2 < 0)
            {
                return values[i + 1].Item1;
            }
            else if (exceeding - values[i].Item2 == 0)
            {
                return (values[i - 1].Item1 + values[i].Item1) / 2;
            }
            else if (exceeding + values[i].Item2 == 0)
            {
                return (values[i + 1].Item1 + values[i].Item1) / 2;
            }
            else
            {
                return values[i].Item1;
            }
        }
    }
}
