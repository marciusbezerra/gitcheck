using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GitCheck
{
    class Program
    {
        private static List<string> ListGitNoRemote = new List<string>();
        private static List<string> ListGitNoIndexed = new List<string>();
        private static List<string> ListGitNoCommited = new List<string>();
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
            var curDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"Getting directory list from '{curDir}'...");
            var gitDirs = Directory.GetDirectories(curDir, ".git", SearchOption.AllDirectories);
            foreach (var gitDir in gitDirs)
            {
                var repositoryDir = Path.GetDirectoryName(gitDir);
                Console.WriteLine($"Processing... {repositoryDir}...");

                Directory.SetCurrentDirectory(repositoryDir);

                var output = DoCmd($"git remote -v");

                output = output.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(output))
                    ListGitNoRemote.Add(gitDir);
                else
                {
                    output = DoCmd($"git status");
                    output = output.Trim().ToLowerInvariant();

                    if (output.Contains("changes not staged for commit"))
                        ListGitNoIndexed.Add(gitDir);
                    else if (output.Contains("changes to be committed"))
                        ListGitNoCommited.Add(gitDir);
                    else if (output.Contains("your branch is ahead of"))
                        ListGitNoPushed.Add(gitDir);
                    else if (output.Contains("your branch is up to date with") || output.Contains("nothing to commit, working tree clean"))
                        ListGitOk.Add(gitDir);
                    else
                        ListGitUnknown.Add(gitDir);
                }

            }

            Directory.SetCurrentDirectory(curDir);

            Console.Clear();

            if (ListGitNoRemote.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITHOUT REMOTE:");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitNoRemote) Console.WriteLine(item);
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

            if (ListGitNoCommited.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("DIRECTORIES WITH NO COMMITED FILES:");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var item in ListGitNoCommited) Console.WriteLine(item);
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

            Console.WriteLine("END!");
            Console.ReadLine();
        }

        private static string DoCmd(string cmd)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = $"/c {cmd}";
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
    }
}
