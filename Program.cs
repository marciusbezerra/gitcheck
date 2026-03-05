using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using GitCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

const string Version = "1.0.6";

List<string> ListGitNoRemote = [];
List<string> ListGitBranchesNoPushed = [];
List<string> ListGitWithUntrackedFiles = [];
List<string> ListGitNoIndexed = [];
List<string> ListGitWithoutCommit = [];
List<string> ListGitNoPushed = [];
List<string> ListGitOk = [];
List<string> ListGitUnknown = [];
List<string> ListProjectRootNoGit = [];
List<string> ListFoldersOutsideProjectRoot = [];
List<string> ListRootFiles = [];


// changes not staged for commit
// changes to be committed
// your branch is ahead of
// your branch is up to date with
// nothing to commit, working tree clean

var Config = GetConfig();
LogoService.PrintLogo(Version);
Console.WriteLine();
var curDir = Directory.GetCurrentDirectory(); // @D:\Projects
Console.WriteLine($"Getting directory list from '{curDir}'...");
Console.WriteLine();

var gitRepositories = Directory.GetDirectories(curDir, ".git", SearchOption.AllDirectories);

foreach (var gitDir in gitRepositories)
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

SearchProjectRoots(curDir);
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

if (ListProjectRootNoGit.Count > 0)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("DIRECTORIES WITHOUT GIT (INSIDE PROJECT ROOT):");
    Console.ForegroundColor = ConsoleColor.Magenta;
    foreach (var item in ListProjectRootNoGit) Console.WriteLine(item);
    Console.ResetColor();
}

if (ListFoldersOutsideProjectRoot.Count > 0)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("DIRECTORIES OUTSIDE PROJECT ROOT:");
    Console.ForegroundColor = ConsoleColor.Magenta;
    foreach (var item in ListFoldersOutsideProjectRoot) Console.WriteLine(item);
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

void SearchProjectRoots(string rootPath)
{
    var projectRootFiles = Directory.GetFiles(rootPath, ".gitcheckprojroot", SearchOption.AllDirectories);

    if (projectRootFiles.Length == 0)
    {
        Console.WriteLine("No project roots found (.gitcheckprojroot)");
        return;
    }

    var projectRoots = projectRootFiles
        .Select(file => Path.GetDirectoryName(file))
        .OrderBy(path => path)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var allProcessedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Process first-level directories within each project root
    foreach (var projectRoot in projectRoots.OrderBy(p => p))
    {
        Console.WriteLine($"Processing project root: {projectRoot}");

        var firstLevelDirs = Directory.GetDirectories(projectRoot)
            .Where(d => !allProcessedFolders.Contains(d))
            .ToList();

        foreach (var directory in firstLevelDirs)
        {
            allProcessedFolders.Add(directory);

            // Skip if this directory is itself a project root
            if (projectRoots.Contains(directory))
            {
                Console.WriteLine($"  {directory} -> IS A PROJECT ROOT (SKIPPING)");
                continue;
            }

            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                Console.WriteLine($"  {directory} -> HAS GIT (SKIPPING)");
                continue;
            }

            if (FileDirectoryMatchesPatterns(directory, Config.IgnoredNoGitDirs))
            {
                Console.WriteLine($"  {directory} -> MATCHES IGNORED PATTERNS (SKIPPING)");
                continue;
            }

            Console.WriteLine($"  {directory} -> NO GIT (ADDING TO LIST)");
            ListProjectRootNoGit.Add(directory);
        }
    }

    // Find first-level directories outside of all project roots
    var allFirstLevelDirs = Directory.GetDirectories(rootPath)
        .Where(d => !allProcessedFolders.Contains(d))
        .ToList();

    foreach (var directory in allFirstLevelDirs)
    {
        // Skip if this directory is a project root
        if (projectRoots.Contains(directory))
        {
            Console.WriteLine($"{directory} -> IS A PROJECT ROOT (SKIPPING)");
            continue;
        }

        var isInsideAnyProjectRoot = projectRoots.Any(pr =>
            directory.StartsWith(pr, StringComparison.OrdinalIgnoreCase) &&
            Path.GetDirectoryName(directory) == pr);

        if (isInsideAnyProjectRoot)
        {
            continue;
        }

        if (FileDirectoryMatchesPatterns(directory, Config.IgnoredNoGitDirs))
        {
            Console.WriteLine($"{directory} -> MATCHES IGNORED PATTERNS (SKIPPING)");
            continue;
        }

        Console.WriteLine($"{directory} -> OUTSIDE PROJECT ROOT (ADDING TO LIST)");
        ListFoldersOutsideProjectRoot.Add(directory);
    }

    Console.WriteLine("\nSUMMARY:");
    Console.WriteLine($"Projects without git in project roots: {ListProjectRootNoGit.Count}");
    Console.WriteLine($"Folders outside project roots: {ListFoldersOutsideProjectRoot.Count}");
}

void SearchRootFiles(string path)
{
    ListRootFiles = Directory.GetFiles(path).Where(path => !FileDirectoryMatchesPatterns(path, Config.IgnoredNoGitFiles)).ToList();
}

string DoCmd(string cmd)
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

Config GetConfig()
{
    var appDirectory = GetApplicationDirectory();
    var config = new ConfigurationBuilder()
        .SetBasePath(appDirectory)
        .AddJsonFile("config.json", optional: true)
        .Build();

    var configObject = config.Get<Config>();

    return configObject ?? new Config();
}

string GetApplicationDirectory()
{
    var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
    var exeDirectory = Path.GetDirectoryName(exePath);

    return exeDirectory;
}

bool FileDirectoryMatchesPatterns(string path, List<string> patterns)
{
    if (patterns == null || patterns.Count == 0)
        return false;

    var matcher = new Matcher();
    matcher.AddIncludePatterns(patterns);

    var normalizedPath = path.Replace("\\", "/");

    var result = matcher.Match(normalizedPath);
    if (result.HasMatches)
        return true;

    var parts = normalizedPath.Split('/');
    foreach (var part in parts)
    {
        if (string.IsNullOrWhiteSpace(part))
            continue;

        result = matcher.Match(part);
        if (result.HasMatches)
            return true;
    }

    return false;
}
