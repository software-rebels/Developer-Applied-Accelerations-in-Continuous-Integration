using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.Algorithms;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.Prediction
{
    public class PredictingWrapper
    {
        private readonly Predictor _predictor;
        private readonly ForecastingContext _forecastingContext;
        private readonly ILogger<PredictingWrapper> _logger;

        public PredictingWrapper(Predictor predictor, ForecastingContext forecastingContext, ILogger<PredictingWrapper> logger)
        {
            _predictor = predictor;
            _forecastingContext = forecastingContext;
            _logger = logger;
        }

        public async Task Predict(string vcsUrl)
        {
            var toPredict = _forecastingContext.Builds.AsQueryable().Where(b => b.VcsUrl == vcsUrl).Select(b => new { b.VcsUrl, b.JobName }).Distinct().ToAsyncEnumerable();
            await foreach (var p in toPredict)
            {
                _logger.LogInformation($"Predicting: {p.VcsUrl}, {p.JobName}");
                await _predictor.SlidingWindowAverageAsync(p.VcsUrl, p.JobName).ConfigureAwait(false);
                await _predictor.SimpleLinearRegressionAsync(p.VcsUrl, p.JobName).ConfigureAwait(false);
                await _predictor.SimpleAverageAsync(p.VcsUrl, p.JobName).ConfigureAwait(false);
            }
        }

        public async Task PredictAll()
        {
            var projects = await _forecastingContext.Builds.Select(b => b.VcsUrl).Distinct().ToListAsync().ConfigureAwait(false);
            for (int i = 0; i < projects.Count; i++)
            {
                _logger.LogInformation($"Predicting: {i + 1}/{projects.Count}");
                string project = projects[i];
                await Predict(project).ConfigureAwait(false);
            }
        }
    }
}
