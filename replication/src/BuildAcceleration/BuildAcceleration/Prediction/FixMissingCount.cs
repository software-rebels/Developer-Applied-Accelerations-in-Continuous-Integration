using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastBuildTime.Algorithms;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.Prediction
{
    public class FixMissingCount
    {
        private readonly IDbContextFactory<ForecastingContext> _forecastingContextFactory;
        private readonly ILogger<FixMissingCount> _logger;

        public FixMissingCount(IDbContextFactory<ForecastingContext> forecastingContextFactory, ILogger<FixMissingCount> logger)
        {
            _forecastingContextFactory = forecastingContextFactory;
            _logger = logger;
        }

        public async Task FixAsync()
        {
            using var context = _forecastingContextFactory.CreateDbContext();
            var evaluations = await context.Evaluations.AsQueryable().ToListAsync().ConfigureAwait(false);
            var missingEvaluationQueryable =
                from m in evaluations.Where(e => e.BuildCount == 0)
                join r in evaluations.Where(e => e.BuildCount != 0)
                    on new { m.VcsUrl } equals new { r.VcsUrl }
                    into references
                select new { Missing = m, References = references };
            var missingEvaluation = missingEvaluationQueryable.ToList();
            foreach (var m in missingEvaluation)
            {
                var references = m.References.Select(e => e.BuildCount).Distinct();
                var count = references.Take(2).Count();
                if (count > 1)
                {
                    _logger.LogWarning($"Inconsistant reference build count! {m.Missing.VcsUrl}, {m.Missing.Approach}");
                    continue;
                }
                if (count == 0)
                {
                    _logger.LogWarning($"Missing reference build count! {m.Missing.VcsUrl}, {m.Missing.Approach}");
                    continue;
                }
                m.Missing.BuildCount = references.First();
            }
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
