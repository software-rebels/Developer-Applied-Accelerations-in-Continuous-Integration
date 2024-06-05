using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Octokit;
using Polly;
using CircleYml = ForecastBuildTime.SqlModels.CircleYml;

namespace ForecastBuildTime.GitManipulation
{
    public class SetYamlHashFromRepository
    {
        private const int IterateCount = 50;
        private readonly IMongoCollection<BuildEntry> _mongoCollection;
        private readonly IDbContextFactory<ForecastingContext> _dbContextFactory;
        private readonly ILogger<SetYamlHashFromRepository> _logger;

        public SetYamlHashFromRepository(IMongoCollection<BuildEntry> mongoCollection, IDbContextFactory<ForecastingContext> dbContextFactory, ILogger<SetYamlHashFromRepository> logger)
        {
            _mongoCollection = mongoCollection;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task ProcessRepository(string repoName)
        {
            var splashIndex = repoName.IndexOf('/');
            Debug.Assert(splashIndex > 0);
            var owner = repoName[..splashIndex];
            var name = repoName[(splashIndex + 1)..];
            var vcsUrl = "https://github.com/" + repoName;
            var ghRetryPolicy = Policy.Handle<RateLimitExceededException>().WaitAndRetryForeverAsync((_, e, _) => (e as RateLimitExceededException)!.GetRetryAfterTimeSpan(), (_, _, _) => Task.CompletedTask);

            List<string> buildCommits;
            var configFiles = new Dictionary<string, (string sha, string content)>();

            await using (var db = _dbContextFactory.CreateDbContext())
            {
                buildCommits = await db.Builds.AsQueryable().Where(b => b.VcsUrl == vcsUrl && string.Compare(b.BuildUrl, "https://circleci.com/gh/diem/diem/99907") > 0).Select(b => b.VcsRevision).Distinct().ToListAsync().ConfigureAwait(false);
                _logger.LogInformation($"Get {buildCommits.Count} commits.");

                for (int i = 0; i < buildCommits.Count; i++)
                {
                    var commitId = buildCommits[i];
                    var github = new GitHubClient(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name))
                    {
#warning "Replace with your GitHub token."
                        Credentials = new Credentials("deprecated")
                    };

                    await ghRetryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            _logger.LogInformation($"Start processing {commitId}.");
                            var contents = await github.Repository.Content.GetAllContentsByRef(owner, name, ".circleci/config.yml", commitId).ConfigureAwait(false);
                            _logger.LogInformation($"Commit {commitId} successfully fetched.");

                            if (contents.Count != 1)
                            {
                                _logger.LogWarning($"Commit: {commitId} got more than one files through GitHub API.");
                            }

                            var circleYaml = contents.Count > 0 ? contents[0] : default;

                            if (circleYaml != null)
                            {
                                _logger.LogInformation($"Found: {commitId}");
                                configFiles.Add(commitId, (circleYaml.Sha, circleYaml.Content));
                            }
                            else
                            {
                                // TODO: get file even unchanged, because this only contains changed yaml file
                                _logger.LogWarning($"Not found: {commitId}");
                                configFiles.Add(commitId, (string.Empty, string.Empty));
                            }

                            var rateLimit = github.GetLastApiInfo().RateLimit;
                            if (rateLimit.Remaining < 5)
                            {
                                await Task.Delay(rateLimit.Reset - DateTimeOffset.UtcNow).ConfigureAwait(false);
                            }
                        }
                        catch (ApiValidationException e)
                        {
                            //6274b7ad00585f7428cf53cbe83e02a37d6f36bf
                            _logger.LogWarning($"API returns bad result on {commitId} .", e);
                            _logger.LogWarning(e.ToString());
                        }
                        catch (NotFoundException e)
                        {
                            // no such commit or file
                            _logger.LogWarning($"API returns bad result Not Found on {commitId} .", e);
                        }
                        catch (RateLimitExceededException e)
                        {
                            // this should be wrapped
                            _logger.LogWarning($"Reached rate limit! Next: {e.GetRetryAfterTimeSpan()}");
                            throw;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Execution error on {commitId} .", e);
                            _logger.LogError(e.ToString());
                        }
                    }).ConfigureAwait(false);
                }

                _logger.LogInformation("Fetched commit id and sha:");
                _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(configFiles.Select(p => new { CommitId = p.Key, FileSha = p.Value.sha })));

                // skipping save to db.
                //var circleYmls = configFiles.Values.Where(t => !string.IsNullOrEmpty(t.sha)).Select(t => new CircleYml
                //{
                //    Sha256 = Convert.FromHexString(t.sha),
                //    Content = t.content,
                //});
                //db.CircleYmls.AddRange(circleYmls);
                //await db.SaveChangesAsync().ConfigureAwait(false);
            }

            _logger.LogInformation("Yaml successfully saved.");

            bool enditer = false;
            bool isFirst = false;
            string lastBuild = "https://circleci.com/gh/diem/diem/99907";
            while (!enditer)
            {
                await using var db = _dbContextFactory.CreateDbContext();
                var builds = await db.Builds.AsQueryable().Where(b => b.VcsUrl == vcsUrl && (isFirst || string.Compare(b.BuildUrl, lastBuild) > 0)).OrderBy(b => b.BuildUrl).Take(IterateCount).ToListAsync().ConfigureAwait(false);
                if (builds.Count == 0)
                {
                    break;
                }
                enditer = builds.Count != IterateCount;
                isFirst = false;
                lastBuild = builds[^1].BuildUrl;
                _logger.LogInformation($"Iter {builds.Count} builds. Last is {lastBuild}");

                foreach (var b in builds)
                {
                    var (sha, content) = configFiles.GetValueOrDefault(b.VcsRevision);
                    (sha, content) = (sha ?? string.Empty, content ?? string.Empty);
                    b.CircleYmlHash = Convert.FromHexString(sha);
                }

                _logger.LogInformation("Saving...");
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
