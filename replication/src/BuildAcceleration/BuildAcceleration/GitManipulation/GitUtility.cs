using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp;

namespace ForecastBuildTime.GitManipulation
{
    public static class GitUtility
    {
        public static string GetRepositoryPath(string repoName)
        {
            string path = GetRepoPath(repoName);
            if (!Repository.IsValid(path))
                Repository.Clone($"https://github.com/{repoName}.git", path);
            return path;
        }

        public static Repository GetRepository(string repoName)
        {
            var path = GetRepositoryPath(repoName);
            return new Repository(path);
        }

        private static string GetRepoPath(string repoName)
        {
            const string repoFolder = "ci_repos";
            string basePath = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows), RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) switch
            {
                (true, _) => "C:",
                (_, true) => Environment.GetFolderPath(Environment.SpecialFolder.Desktop, Environment.SpecialFolderOption.Create),
                _ => "/"
            };
            return Path.Combine(basePath, repoFolder, repoName[(repoName.LastIndexOf('/') + 1)..]);
        }
    }
}
