using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace GitCheck
{
    class Program
    {
        private static List<string> ListGitNoRemote = new List<string>();
        private static List<string> ListGitBranchesNoPushed = new List<string>();
        private static List<string> ListGitWithUntrackedFiles = new List<string>();
        private static List<string> ListGitNoIndexed = new List<string>();
        private static List<string> ListGitWithoutCommit = new List<string>();
        private static List<string> ListGitNoPushed = new List<string>();
        private static List<string> ListGitOk = new List<string>();
        private static List<string> ListGitUnknown = new List<string>();


        // changes not staged for commit
        // changes to be committed
        // your branch is ahead of
        // your branch is up to date with
        // nothing to commit, working tree clean

        static void Main(string[] args)
        {
            LogoService.PrintLogo();
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

            Directory.SetCurrentDirectory(curDir);

            Console.Clear();
            LogoService.PrintLogo();

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

            Console.WriteLine("bye!");
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
    }
}
