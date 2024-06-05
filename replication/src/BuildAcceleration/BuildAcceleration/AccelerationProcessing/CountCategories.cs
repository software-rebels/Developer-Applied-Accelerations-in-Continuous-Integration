using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using ForecastBuildTime.AccelerationProcessing;
using ForecastBuildTime.Helpers;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace ForecastBuildTime;

[Command]
internal class CountCategories
{
    static CountCategories()
    {
        var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
        var yamlObject = deserializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(s_hierarchy);
        s_categoryMap =
            (from p in yamlObject
             from v in (IEnumerable<object>)p.Value["Subcategories"]
             select (Subcategory: (string)v, Category: p.Key))
            .ToDictionary(t => t.Subcategory, t => t.Category);
    }

    private readonly DirectoryHelper _directoryHelper;
    private readonly ILogger<CountCategories> _logger;
    private readonly PredictionEvaluator _predictionEvaluator;

    public CountCategories(DirectoryHelper directoryHelper, ILogger<CountCategories> logger, PredictionEvaluator predictionEvaluator)
    {
        _directoryHelper = directoryHelper;
        _logger = logger;
        _predictionEvaluator = predictionEvaluator;
    }

    [Option("--sneaky")]
    public bool CreateSneaky { get; set; } = false;

    [Option("-t|--treemap")]
    public bool CreateTreemapCsv { get; set; } = false;

    public async Task OnExecuteAsync()
    {
        if (CreateSneaky)
        {
            await RunSneaky();
        }
        if (CreateTreemapCsv)
        {
            await RunCreateTreemapCsv();
        }
        else
        {
            await RunCategorization();
        }
    }

    public async ValueTask RunSneaky()
    {
        var finalLabels = await ReadLabelsAsync();
        var groups =
            from l in finalLabels
            from purpose in GetSubcategoryEnumerable(l.FinalLabels)
            from mechanism in GetSubcategoryEnumerable(l.FinalMechanisms)
            group 1 by (purpose, mechanism) into g
            where g.Count() > 1
            select $"{g.Key.purpose} [{g.Count()}] {g.Key.mechanism}";
        var result = string.Join("\r\n", groups);
        Console.WriteLine(result);
    }

    public async ValueTask RunCreateTreemapCsv()
    {
        var finalLabels = await ReadLabelsAsync();
        int i = 1;
        var tree =
            from l in finalLabels
            from purpose in GetCategoryEnumerable(l.FinalLabels)
            from mechanism in GetCategoryEnumerable(l.FinalMechanisms)
            group 1 by (purpose, mechanism) into g
            select new
            {
                Name = g.Key.mechanism + "<br>" + new string(' ', i++),
                Parent = g.Key.purpose,
                Value = g.Count(),
            };
        tree = tree.Concat(finalLabels.SelectMany(l => GetCategoryEnumerable(l.FinalLabels)).Distinct().Select(purpose => new { Name = purpose, Parent = "", Value = 0 }));
        var csvPath = _directoryHelper.GetFullPath("treemap_data.csv");
        await using var csvWriter = new CsvWriter(File.CreateText(csvPath), CultureInfo.InvariantCulture);
        await csvWriter.WriteRecordsAsync(tree);
    }

    public async ValueTask RunCategorization()
    {
        var finalLabels = await ReadLabelsAsync();
        var (inspections, ratios) = await _predictionEvaluator.ReadCsvAsync();
        var inspected = finalLabels.Where(l => inspections.FirstOrDefault(i => i.VcsUrl == l.VcsUrl && i.JobName == l.JobName)?.ResultBool == true).ToList();
        Debug.Assert(inspected.Count == 23);
        var detected = finalLabels
            .Where(l =>
                l.HitRule
                || l.WeightedMiddle < 0.27)
            .ToList();
        // Debug.Assert(detected.Count == 280, $"The detected number is {detected.Count}");
        await ProcessCategoriesAsync(finalLabels, row => row.FinalLabels, "Purpose.csv", GetSubcategoryEnumerable);
        await ProcessCategoriesAsync(finalLabels, row => row.FinalMechanisms, "Mechanism.csv", GetSubcategoryEnumerable);
        await ProcessCategoriesAsync(finalLabels, row => row.FinalMagnitude, "Magnitude.csv", GetSubcategoryEnumerable);
        await ProcessCategoriesAsync(finalLabels, row => row.FinalLabels, "Purpose_cate.csv", GetCategoryEnumerable);
        await ProcessCategoriesAsync(finalLabels, row => row.FinalMechanisms, "Mechanism_cate.csv", GetCategoryEnumerable);
        await ProcessCategoriesAsync(finalLabels, row => row.FinalMagnitude, "Magnitude_cate.csv", GetCategoryEnumerable);

        // await ProcessCategoriesAsync(inspected, row => row.FinalLabels, "Purpose_insp.csv");
        // await ProcessCategoriesAsync(inspected, row => row.FinalMechanisms, "Mechanism_insp.csv");
        // await ProcessCategoriesAsync(inspected, row => row.FinalMagnitude, "Magnitude_insp.csv");

        // await ProcessCategoriesAsync(detected, row => row.FinalLabels, "Purpose_detect.csv");
        // await ProcessCategoriesAsync(detected, row => row.FinalMechanisms, "Mechanism_detect.csv");
        // await ProcessCategoriesAsync(detected, row => row.FinalMagnitude, "Magnitude_detect.csv");

        _logger.LogInformation("Total jobs count is {count}", finalLabels.Count);
        _logger.LogInformation("Total repos count is {count}", finalLabels.Select(i => i.VcsUrl).Distinct().Count());
    }

    private async ValueTask<List<FinalInspection>> ReadLabelsAsync()
    {
        string finalLabelsPath = _directoryHelper.GetFullPath("Final Labels.csv");
        using var r1 = new StreamReader(finalLabelsPath);
        using var csv = new CsvReader(r1, CultureInfo.InvariantCulture);
        return await csv.GetRecordsAsync<FinalInspection>().ToListAsync();
    }

    private async ValueTask ProcessCategoriesAsync(IEnumerable<FinalInspection> finalInspections, Func<FinalInspection, string> selector, string fileName, Func<string, IEnumerable<string>> categoryMapper)
    {
        var itemsByProject = GetResultsByProject(finalInspections, selector, categoryMapper);
        var itemsByJob = GetResults(finalInspections, selector, categoryMapper);
        var itemsComprehensive =
            (from ip in itemsByProject
             join ij in itemsByJob on ip.Name equals ij.Name
             orderby ip.Count descending, ij.Count descending
             select new
             {
                 Categoty = GetMappedCategoryName(ip.Name),
                 ip.Name,
                 JobCount = ij.Count,
                 RepoCount = ip.Count,
             })
            .ToList();
        ConsoleTable
            .From(itemsComprehensive)
            .Configure(o => o.NumberAlignment = Alignment.Right)
            .Write();

        Debug.Assert(itemsByProject.Count == itemsByJob.Count);
        Debug.Assert(itemsComprehensive.Count == itemsByProject.Count);

        string magnitudePath = _directoryHelper.GetFullPath(fileName);
        await using var writer = new CsvWriter(File.CreateText(magnitudePath), CultureInfo.InvariantCulture);
        await writer.WriteRecordsAsync(itemsComprehensive);
    }

    private static readonly IDictionary<string, string> s_subcategoryMap = new Dictionary<string, string>
    {
        { "Skip building chache when  only documents are modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip build if only changed docs", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip tests if only changed docs", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip docker builds when only documents are modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip docker push when only documents are modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip when only certain files were changed", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip if specific type of content in the repository is matched", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip testing when only documents are modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip building dependencies when build.sh script is not modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip dependency build when dependency file hasn't changed", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip rebuilds when shell script is not modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip copying dependencies when no dependencies are updated", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip if only changed docs", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip builds if only doc changes", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip builds (many things) when only documents are modified", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip if only certain folders changed", "Skip build, test, publish, etc. when only specific files change" },
        { "Skip builds for PR commtis updating release changelog", "Skip build, test, publish, etc. when only specific files change" }, // special, show in the paper.

        { "Skip tests when instance was not setup", "Skip build, test, etc. when infrastructure is not ready" },
        { "The required server instance was not created", "Skip build, test, etc. when infrastructure is not ready" },

        { "Don't build if the image is already up to date.", "Skip build if the artifact/cache/target already made" },
        { "Skip building cache when cache has been already made", "Skip build if the artifact/cache/target already made" },
        { "Skip creating cache when cache has been already made", "Skip build if the artifact/cache/target already made" },
        { "Skip if the cache to build already exists", "Skip build if the artifact/cache/target already made" },
        { "Skip updating cache when cache has been already made", "Skip build if the artifact/cache/target already made" },
        { "Skip if Google Chrome is already installed", "Skip build if the artifact/cache/target already made" },
        { "Do not build if the dist folder already exists", "Skip build if the artifact/cache/target already made" },
        { "Skip init db if already init'ed", "Skip build if the artifact/cache/target already made" },

        { "Skip testing when files in specific directories are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip docker builds when specific components are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip docker-build and tests when source code are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Only build when deps changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip if certain folder has not changed.", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skipping the build if no important files have changed.", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip testing when source code are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip tests when files in specific directories are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip build, test, and publish when files in specific directories are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip install when specific components are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip publish when files in specific directories are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip install when only non-documents are modified", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip install when configs are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip when pod file is not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "No files match grep pattern", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip if dependency file has not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip test if no native code has changed", "Skip build, test, publish, etc. when specific files not changed" }, // this is special. the commit may change other source code but still skips
        { "Skip running test scripts when certain folders are not changed", "Skip build, test, publish, etc. when specific files not changed" },
        { "Skip install when two lock files are the same", "Skip build, test, publish, etc. when specific files not changed" }, // this is special and may need separate subcategory. It detects changing to a special file instead of code change.

        { "Skip measuring coverage when coverage has already been measured today", "Skip too frequent invocations" },

        { "Only run on master or PR", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip docker push for non-master branch", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip fork builds", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip if not certain branch", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Avoid testing if the build is not PR", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip running if not PR", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Do not skip build, test, and publish for PR commits", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Avoiding unnecessary build in dev branches when certain folders are not changed", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip Generate cross arch snapshot (arm/arm64) for PRs", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip testing for specific branches", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip if the branch is not a PR.", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Only run tests on certain branch or with tag", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },
        { "Skip a command tar if the branch is master", "Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions" },

        { "Skip docker push when developers specified to skip the build", "Skip when specified by developer" },
        { "Skip docker builds when developers specified to skip the build", "Skip when specified by developer" },
        { "Skip testing when developers specified to skip the build", "Skip when specified by developer" },
        { "Skip when the tag is found.", "Skip when specified by developer" },
        { "Skip slow tests when specified", "Skip when specified by developer" },

        { "Speed up compilation by ccache", "Speed up using external compilation cache" },
        { "Accelerate builds using external caching", "Speed up using external compilation cache" },
        { "Use external sccache", "Speed up using external compilation cache" },

        // { "Skip if already contains breaking changes", "Skip for builds with specific outcome" },
        { "Skip tests when it is nightly build", "Skip builds with specific schedule" },
        { "Only build on nightly build", "Skip builds with specific schedule" },
        { "Run different test sets depending on branch and nightly builds.", "Skip builds with specific schedule" },

        { "Skip test commands under specific build condition", "Skip task under specific build condition" },
        { "Skip if already contains breaking changes", "Skip task under specific build condition" },
        { "Skipping create distribution because build is not for release", "Skip task under specific build condition" },

        // mechanisms
        { "Check branch name using environment variable", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check PR_number using environment variable", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check repository name using environment variable", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check branch name with environment variable", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check Circle CI env var.", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check env var.", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Use Circle CI env var.", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check branch name using Circle CI environment variable", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check PR_number and branch name using Circle CI environment variable", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },
        { "Check branch name using environment variables and regular expression", "Check branch name, PR number, repository name, etc. from CircleCI environmental variables" },

        { "Check user env var.", "Check user-defined environmental variables" },
        { "Check custom env var.", "Check user-defined environmental variables" },
        { "Check status of env var", "Check user-defined environmental variables" },

        { "Check git diff to find changes", "Check file changes by git commands such as \"git diff\" command" },
        { "Use git diff to find changes in specific directories", "Check file changes by git commands such as \"git diff\" command" },
        { "Use git diff and regular expressions to find changes in spefici file types", "Check file changes by git commands such as \"git diff\" command" },
        { "Check git history", "Check file changes by git commands such as \"git diff\" command" },
        { "Use git diff command.", "Check file changes by git commands such as \"git diff\" command" },
        { "Check git diff-tree to find changes", "Check file changes by git commands such as \"git diff\" command" },
        { "Use git command and check keyword", "Check file changes by git commands such as \"git diff\" command" },
        { "Use git diff to find changes", "Check file changes by git commands such as \"git diff\" command" },
        { "Check git log to find changes/Check git log to find a file indicating that the commit should be skiped", "Check file changes by git commands such as \"git diff\" command" },
        { "Check git log to find changes", "Check file changes by git commands such as \"git diff\" command" },
        { "Use git log to find changes in specific files", "Check file changes by git commands such as \"git diff\" command" },

        { "Check git log to check commit messages", "Check metadata using git command" },
        { "Check keyword from git message", "Check metadata using git command" },

        { "Bash check if file exists", "Check target files with bash command" }, // checked
        { "Checking a cache file using bash -f option", "Check target files with bash command" },
        { "Use find command.", "Check target files with bash command" },
        { "Check if folder exists using bash command", "Check target files with bash command" },
        { "Checking a cache file using find command", "Check target files with bash command" }, 
        { "Use find command", "Check target files with bash command" }, // the find command is used to get files and do further commands.
        
        { "Using cache and code hash, also check flag file", "Check flag files" }, // checked; a flag file may be cached file, dependency file etc.
        { "Checking a cache file", "Check flag files" }, // checked
        { "Use files from other jobs", "Check flag files" },
        { "Use cached files.", "Check flag files" },
        { "Restoring cache and checking cache", "Check flag files" },
        { "Use bash -s command", "Check flag files" },
        { "Check the last date of coverage measurement", "Check flag files" },
        
        { "Check the content of pod file", "Other file commands" },
        { "Check hash of the build file", "Other file commands" },
        { "Manually check cache", "Other file commands" },
        { "Use linux system command", "Other file commands" }, // one occurance
        { "Compare two files using diff command", "Other file commands" },
        { "Check command return value", "Other file commands" },
        { "Check with related command and bash scripts", "Other file commands" },
        
        { "Ccache", "Use external compilation cache tool" },
        { "Use sccache", "Use external compilation cache tool" },



        { "Check API tag for dependency changes", "Check Docker image tag from API" },
        { "Use GitHub API", "Check GitHub API" },
        { "Accessing to Github API", "Check GitHub API" },
        { "Check modified files using GitHub API and save a file with value true to indicate doc only change", "Check GitHub API" },


        { "Intigrates with package manager (yarn)", "Use command defined in package manager file" },

        // magnitude
        { "remaining steps", "Remaining steps" },
        { "step", "Step" },
        { "partial step", "Partial step" },
    };

    private static readonly IDictionary<string, string> s_categoryMap;

    private static readonly string s_hierarchy = """
    File changes:
        Number: P1
        Subcategories:
            - Skip build, test, publish, etc. when specific files not changed
            - Skip build, test, publish, etc. when only specific files change
    External conditions:
        Number: P2
        Subcategories:
            - Skip build, test, publish, etc. when specified branch/fork/PR does not satisfy conditions
            - Skip when specified by developer
            - Skip task under specific build condition
            - Skip builds with specific schedule
            - Skip for builds with specific outcome
            - Do partial package in specified tag
            - Do partial package in specified branches
    Running environment:
        Number: P3
        Subcategories:
            - Skip build if the artifact/cache/target already made
            - Speed up using external compilation cache
            - Skip build, test, etc. when infrastructure is not ready
            - Skip installing software if it is alread installed

    Environmental variables and parameters:
        Number: M1
        Subcategories:
            - Check branch name, PR number, repository name, etc. from CircleCI environmental variables
            - Checking pipeline's parameters
            - Check user-defined environmental variables
    Git commands:
        Number: M2
        Subcategories:
            - Check file changes by git commands such as "git diff" command
            - Check metadata using git command
    External tools and APIs:
        Number: M3
        Subcategories:
            - Use external compilation cache tool
            - Check modified files using GitHub API and save a file with value true to indicate doc only change
            - Use GitHub API
            - Check Docker image tag from API
            - Check GitHub API
    File commands:
        Number: M4
        Subcategories:
            - Check flag files
            - Check target files with bash command
            - Use shell commands
            - Use command defined in package manager file
            - Other file commands
    Logs/outputs:
        Number: M5
        Subcategories:
            - Inspecting trace logs
            - Find test logs
            - Inspecting outputs generated by a program
    """;

    static string GetMappedSubcategoryName(string c) => s_subcategoryMap.TryGetValue(c, out var mapped) ? mapped : c;
    static string GetMappedCategoryName(string c)
    {
        return s_categoryMap.TryGetValue(c, out var mapped) ? mapped : ((Func<string>)(() =>
        {
            Console.WriteLine(c);
            return "Others";
        }))();
    }

    public List<Category> GetResults(IEnumerable<FinalInspection> finalInspections, Func<FinalInspection, string> selector, Func<string, IEnumerable<string>> categoryMapper)
    {
        var cats = finalInspections
            .SelectMany(row =>
            {
                var categoriesString = selector(row);
                if (string.IsNullOrWhiteSpace(categoriesString))
                {
                    _logger.LogWarning("Found empty inspection at {name}", row.VcsUrl);
                }
                return categoryMapper(categoriesString);
            });
        return cats.GroupBy(x => x)
            .Select(g => new Category(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
    }

    private static IEnumerable<string> GetSubcategoryEnumerable(string categoriesString)
    {
        var categories = categoriesString.Split(';');
        if (categories.Contains("x"))
        {
            return System.Linq.Enumerable.Empty<string>();
        }
        return categories
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Select(GetMappedSubcategoryName);
    }

    private static IEnumerable<string> GetCategoryEnumerable(string categoriesString)
    {
        var subcategories = GetSubcategoryEnumerable(categoriesString);
        return subcategories.Select(GetMappedCategoryName).Distinct();
    }

    public List<Category> GetResultsByProject(IEnumerable<FinalInspection> finalInspections, Func<FinalInspection, string> selector, Func<string, IEnumerable<string>> categoryMapper)
    {
        var cats = finalInspections.GroupBy(row => row.VcsUrl)
            .SelectMany(g =>
            {
                var categories = GetResults(g, selector, categoryMapper); // categories in this repo.
                return categories.Select(c => c.Name).Distinct();
            });
        return cats.GroupBy(x => x)
            .Select(g => new Category(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
    }

    public class FinalInspection
    {
        [Name("Number")]
        public int Number { get; set; }
        [Name("VcsUrl")]
        public required string VcsUrl { get; set; }
        [Name("JobName")]
        public required string JobName { get; set; }
        [Name("Programming Purposes")]
        public required string FinalLabels { get; set; }
        [Name("Programming Mechanisms")]
        public required string FinalMechanisms { get; set; }
        [Name("Programming Magnitude")]
        public required string FinalMagnitude { get; set; }
        [Name("WeightedMiddle")]
        public double WeightedMiddle { get; set; }
        [Name("HitRule")]
        public bool HitRule { get; set; }
    }

    public record struct Category(string Name, int Count);
}