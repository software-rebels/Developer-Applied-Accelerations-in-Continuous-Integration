using System.Threading.Tasks;
using ForecastBuildTime.AccelerationProcessing;
using ForecastBuildTime.AccelerationSampling;
using ForecastBuildTime.DataProcessing;
using McMaster.Extensions.CommandLineUtils;

namespace ForecastBuildTime;
[Command]
[Subcommand(typeof(DetectRules))]
[Subcommand(typeof(KMeansCluster))]
[Subcommand(typeof(CountCategories))]
[Subcommand(typeof(LongTail))]
internal class MainCommand
{
    private Task OnExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
