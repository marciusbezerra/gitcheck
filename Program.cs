using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GitCheck
{
    class Program
    {
        const string Version = "1.0.3";

        private static List<string> ListGitNoRemote = new List<string>();
        private static List<string> ListGitBranchesNoPushed = new List<string>();
        private static List<string> ListGitWithUntrackedFiles = new List<string>();
        private static List<string> ListGitNoIndexed = new List<string>();
        private static List<string> ListGitWithoutCommit = new List<string>();
        private static List<string> ListGitNoPushed = new List<string>();
        private static List<string> ListGitOk = new List<string>();
        private static List<string> ListGitUnknown = new List<string>();
        private static List<string> ListRootLevelTwoNoGit = new List<string>();
        private static List<string> ListRootFiles = new List<string>();

        private static Config Config = new();



        // changes not staged for commit
        // changes to be committed
        // your branch is ahead of
        // your branch is up to date with
        // nothing to commit, working tree clean

        static void Main(string[] args)
        {
            Config = GetConfig();
            LogoService.PrintLogo(Version);
            Console.WriteLine();
            var curDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"Getting directory list from '{curDir}'...");
            Console.WriteLine();
            var gitDirs = Directory.GetDirectories(curDir, ".git", SearchOption.AllDirectories);
            foreach (var gitDir in gitDirs)
            {
                var repositoryDir = Path.GetDirectoryName(gitDir);
                Console.WriteLine($"Processing... {repositoryDir}...");

                Directory.SetCurrentDirectory(repositoryDir);

                var output = DoCmd($"git remote -v");
                output = output.Trim().ToLowerInvariant();
                var hasNoRemote = string.IsNullOrWhiteSpace(output);
                if (hasNoRemote)
                    ListGitNoRemote.Add(Path.GetDirectoryName(gitDir));

                output = DoCmd($"git log --branches --not --remotes --simplify-by-decoration --decorate --oneline");
                output = output.Trim().ToLowerInvariant();
                var hasBranchesNoPushed = !string.IsNullOrWhiteSpace(output);
                if (hasBranchesNoPushed)
                    ListGitBranchesNoPushed.Add($"{Path.GetDirectoryName(gitDir)} -> {output}");

                if (!hasNoRemote && !hasBranchesNoPushed)
                {
                    output = DoCmd($"git status");
                    output = output.Trim().ToLowerInvariant();

                    if (output.Contains("untracked files"))
                        ListGitWithUntrackedFiles.Add(Path.GetDirectoryName(gitDir));
                    else if (output.Contains("changes not staged for commit"))
                        ListGitNoIndexed.Add(Path.GetDirectoryName(gitDir));
                    else if (output.Contains("changes to be committed"))
                        ListGitWithoutCommit.Add(Path.GetDirectoryName(gitDir));
                    else if (output.Contains("your branch is ahead of"))
                        ListGitNoPushed.Add(Path.GetDirectoryName(gitDir));
                    else if (output.Contains("your branch is up to date with") || output.Contains("nothing to commit, working tree clean"))
                        ListGitOk.Add(Path.GetDirectoryName(gitDir));
                    else
                        ListGitUnknown.Add(Path.GetDirectoryName($"{Path.GetDirectoryName(gitDir)} -> {output}"));
                }

            }

            SearchLevelTwoNoGitDirectories(curDir);
            SearchRootFiles(curDir);

            Directory.SetCurrentDirectory(curDir);

            Console.Clear();
            LogoService.PrintLogo(Version);

            if (Config.IgnoredNoGitDirs.Count > 0 || Config.IgnoredNoGitFiles.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Ignoring no git folders: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(string.Join(", ", Config.IgnoredNoGitDirs));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Ignoring no git files: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(string.Join(", ", Config.IgnoredNoGitFiles));
                Console.ResetColor();
            }


            if (ListGitNoRemote.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITHOUT REMOTE:");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: After fix this. Re-run Check!");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitNoRemote) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitBranchesNoPushed.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES BRANCHES NOT PUSHED:");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: After fix this. Re-run Check!");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitBranchesNoPushed) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitWithUntrackedFiles.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITH UNTRACKED FILES:");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitWithUntrackedFiles) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitNoIndexed.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITH NO INDEXED FILES:");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitNoIndexed) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitWithoutCommit.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITH NO COMMITED FILES:");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitWithoutCommit) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitNoPushed.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITH NO PUSHED FILES:");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitNoPushed) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitOk.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES OK:");
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (var item in ListGitOk) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListGitUnknown.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITH UNKNOWN STATUS:");
                Console.ForegroundColor = ConsoleColor.Magenta;
                foreach (var item in ListGitUnknown) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListRootLevelTwoNoGit.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITHOUT GIT (MAX TWO LEVELS):");
                Console.ForegroundColor = ConsoleColor.Magenta;
                foreach (var item in ListRootLevelTwoNoGit) Console.WriteLine(item);
                Console.ResetColor();
            }

            if (ListRootFiles.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("ROOT FILES:");
                Console.ForegroundColor = ConsoleColor.Magenta;
                foreach (var item in ListRootFiles) Console.WriteLine(item);
                Console.ResetColor();
            }

            Console.WriteLine("bye!");
        }

        private static void SearchRootFiles(string path)
        {
            ListRootFiles = Directory.GetFiles(path).Where(path => !FileDirectoryMatchesPatterns(path, Config.IgnoredNoGitFiles)).ToList();
        }

        private static void SearchLevelTwoNoGitDirectories(string path, int level = 0)
        {
            if (!Directory.Exists(Path.Combine(path, ".git")))
                if (!FileDirectoryMatchesPatterns(path, Config.IgnoredNoGitDirs))
                    ListRootLevelTwoNoGit.Add(path);

            if (level < 2)
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (string directory in directories)
                    SearchLevelTwoNoGitDirectories(directory, level + 1);
            }
        }

        private static string DoCmd(string cmd)
        {
            var consoleEncoding = Console.OutputEncoding;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            try
            {
                var p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                if (isWindows)
                {
                    p.StartInfo.FileName = "cmd";
                    p.StartInfo.Arguments = $"/c {cmd}";
                }
                else
                {
                    p.StartInfo.FileName = "/bin/bash";
                    p.StartInfo.Arguments = $"-c \" {cmd} \"";
                }

                p.Start();

                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return output;
            }
            finally
            {
                Console.OutputEncoding = consoleEncoding;
            }
        }

        private static Config GetConfig()
        {
            var appDirectory = GetApplicationDirectory();
            var config = new ConfigurationBuilder()
                .SetBasePath(appDirectory)
                .AddJsonFile("config.json", optional: true)
                .Build();

            var configObject = config.Get<Config>();

            return configObject ?? new Config();
        }

        private static string GetApplicationDirectory()
        {
            var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            var exeDirectory = Path.GetDirectoryName(exePath);

            return exeDirectory;
        }

        static bool FileDirectoryMatchesPatterns(string path, List<string> patterns)
        {
            var matcher = new Matcher();
            matcher.AddIncludePatterns(patterns);

            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var result = matcher.Match(directory, fileName);

            return result.HasMatches;
        }
    }
}
