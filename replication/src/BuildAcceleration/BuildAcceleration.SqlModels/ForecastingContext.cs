using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ForecastBuildTime.SqlModels
{
    public class ForecastingContext : DbContext
    {
        public DbSet<BuildEntrySlim> Builds { get; protected set; } = null!;
        public DbSet<CircleYml> CircleYmls { get; protected set; } = null!;
        public DbSet<PredictionEvaluation> Evaluations { get; protected set; } = null!;
        public DbSet<AccelerationSample> AccelerationSamples { get; protected set; } = null!;
        public DbSet<KMeansClusters> KMeansClusters { get; protected set; } = null!;
        public DbSet<JobInfo> JobInfos { get; protected set; } = null!;

        public ForecastingContext(DbContextOptions<ForecastingContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(@"Server=localhost;Port=13339;Database=forecasting;User Id=forecasting;Password=123", options => options.EnableRetryOnFailure());
            }
            optionsBuilder.ConfigureWarnings(c => c.Log((RelationalEventId.CommandExecuting, LogLevel.Debug)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<BuildEntrySlim>()
            //    .HasOne(b => b.CircleYml)
            //    .WithMany(y => y.Builds)
            //    .HasForeignKey(b => b.CircleYmlHash)
            //    .HasPrincipalKey(y => y.Sha256)
            //    .OnDelete(DeleteBehavior.ClientNoAction)
            //    .IsRequired(false);

            modelBuilder.Entity<BuildEntrySlim>()
                .Property(b => b.AttachedProperties)
                .HasDefaultValueSql("'{}'::jsonb");

            modelBuilder.Entity<KMeansClusters>()
                .HasOne(c => c.AccelerationSample)
                .WithMany(s => s.ClusterCenters)
                .HasForeignKey(c => new { c.VcsUrl, c.JobName })
                .HasPrincipalKey(s => new { s.VcsUrl, s.JobName });

            modelBuilder.Entity<AccelerationSample>()
                .Property(s => s.HitManualRules)
                .HasDefaultValue(new Dictionary<string, bool>());
        }
    }
}
