using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;

namespace ForecastBuildTime.DataTransfer
{
    public static class BuildEntryConverter
    {
        public static BuildEntrySlim SuccessToSlim(this BuildEntry buildEntry, out byte[] hash)
        {
            if (buildEntry.Status != "success")
            {
                throw new InvalidCastException("Only success builds can be converted.");
            }

            //// Check steps
            //int parallel = buildEntry.Parallel;
            //foreach (var step in buildEntry.Steps)
            //{
            //    if (step.Actions.Count(a => a.Status == "success") != parallel)
            //    {
            //        throw new OverflowException($"Action count of step {step.Name} do not match parallel count.");
            //    }

            //    // seems that no need to verify this.
            //    //for (int i = 0; i < parallel; i++)
            //    //{
            //    //    if (step.Actions.Where(a => a.Status == "success").ElementAt(i).Index != i)
            //    //        throw new IndexOutOfRangeException("Index mismatch");
            //    //}
            //}

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(buildEntry.CircleYml!.String));

            var result = new BuildEntrySlim
            {
                VcsUrl = buildEntry.VcsUrl,
                BuildUrl = buildEntry.BuildUrl,
                JobName = buildEntry.Workflows!.JobName,
                Parallel = buildEntry.Parallel,
                SelectedSuccess = false,
                StartTime = buildEntry.StartTime!.Value,
                Status = buildEntry.Status,
                SumOfBuildTimeInSteps = TimeSpan.FromMilliseconds(buildEntry.Steps.Sum(s => (long?)s.Actions.LastOrDefault(a => a.Status == "success")?.RunTimeMillis ?? s.Actions.LastOrDefault()?.RunTimeMillis ?? 0)),
                CircleYmlHash = hash,
            };

            return result;
        }
    }
}
