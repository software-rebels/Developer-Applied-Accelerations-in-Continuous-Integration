using System;
using System.Threading.Tasks;
using ForecastBuildTime.AccelerationSampling;
using ForecastBuildTime.Helpers;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ForecastBuildTime
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args)
                .UseCommandLineApplication<MainCommand>(args, _ => { })
                .Build();

            try
            {
                // await host.Services.GetRequiredService<Playground>().RunAsync().ConfigureAwait(false);
                // await host.Services.GetRequiredService<AccelerationProcessing.PredictionEvaluator>().EvaluatePrediction().ConfigureAwait(false);
                Environment.ExitCode = await host.RunCommandLineApplicationAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // host.Services.GetRequiredService<ILogger<Program>>().LogError(e, "End execution with exception.");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(s =>
                {
#if DEBUG
                    s.AddTransient(_ =>
                        new MongoClient("mongodb://forecastRead:+3o2;.cIU883%2Fz@localhost:27017/forecastBuildTime"));
#else
                    s.AddTransient(_ =>
                        new MongoClient("mongodb://forecastRead:+3o2;.cIU883%2Fz@forecast-mongo:27017/forecastBuildTime"));
#endif
                    s.AddTransient(service =>
                        service.GetRequiredService<MongoClient>()
                            .GetDatabase("forecastBuildTime")
                            .GetCollection<BuildEntry>("cc_builds_2021"));
#if DEBUG
                    s.AddDbContext<ForecastingContext>(builder =>
                        builder.UseNpgsql(@"Server=localhost;Port=13339;Database=forecasting;User Id=forecasting;Password=r27sgJNKcdH-_SqT3nEWAMX}TyuNwWh>W8.RR.N7Gg7vjbGe{9PpKF8_xDfQ{b;", options => options.EnableRetryOnFailure().CommandTimeout(300)));
                    s.AddDbContextFactory<ForecastingContext>(builder =>
                        builder.UseNpgsql(@"Server=localhost;Port=13339;Database=forecasting;User Id=forecasting;Password=r27sgJNKcdH-_SqT3nEWAMX}TyuNwWh>W8.RR.N7Gg7vjbGe{9PpKF8_xDfQ{b;", options => options.EnableRetryOnFailure().CommandTimeout(300)));
#else
                    s.AddDbContext<ForecastingContext>(builder =>
                        builder.UseNpgsql(@"Server=forecast-pg;Port=13339;Database=forecasting;User Id=forecasting;Password=r27sgJNKcdH-_SqT3nEWAMX}TyuNwWh>W8.RR.N7Gg7vjbGe{9PpKF8_xDfQ{b;", options => options.EnableRetryOnFailure().CommandTimeout(300)));
                    s.AddDbContextFactory<ForecastingContext>(builder =>
                        builder.UseNpgsql(@"Server=forecast-pg;Port=13339;Database=forecasting;User Id=forecasting;Password=r27sgJNKcdH-_SqT3nEWAMX}TyuNwWh>W8.RR.N7Gg7vjbGe{9PpKF8_xDfQ{b;", options => options.EnableRetryOnFailure().CommandTimeout(300)));
#endif
                    s.AddSingleton<DirectoryHelper>();

                    s.AddTransient<Playground>();
                    s.AddTransient<DataFilter.OrderByBuildTime>();
                    s.AddTransient<DataFilter.MonthlyEmptyStepBuilds>();
                    s.AddTransient<DataFilter.ListAllRepos>();
                    s.AddTransient<DataTransfer.ConverterService>();
                    s.AddTransient<DataTransfer.DataSampling>();
                    s.AddTransient<DataTransfer.MissingFieldTransferer>();
                    s.AddTransient<Algorithms.Predictor>();
                    s.AddTransient<Prediction.PredictingWrapper>();
                    s.AddTransient<Prediction.FixMissingCount>();
                    s.AddTransient<Prediction.ListMmre>();
                    s.AddTransient<DataFilter.YamlAnalysis>();
                    s.AddTransient<GitManipulation.SetYamlHashFromRepository>();
                    s.AddTransient<DataProcessing.AttachedPropertyHelper>();
                    s.AddTransient<Training.FeatureCollector>();
                    s.AddTransient<AccelerationSampling.Sampler>();
                    s.AddTransient<AccelerationSampling.PrintSample>();
                    s.AddTransient<AccelerationSampling.ShapiroWilkCalculator>();
                    s.AddTransient<AccelerationSampling.KMeansCluster>();
                    s.AddTransient<AccelerationSampling.BuildCountChecker>();
                    s.AddTransient<AccelerationProcessing.PredictionEvaluator>();
                });
    }
}
