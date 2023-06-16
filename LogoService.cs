
using System;
using System.Text;

namespace GitCheck
{
    public class LogoService
    {
        public static void PrintLogo(string version)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(@"   ___ _ _       ___ _               _    ");
            Console.WriteLine(@"  / _ (_) |_    / __\ |__   ___  ___| | __");
            Console.WriteLine(@" / /_\/ | __|  / /  | '_ \ / _ \/ __| |/ /");
            Console.WriteLine(@"/ /_\\| | |_  / /___| | | |  __/ (__|   < ");
            Console.WriteLine(@"\____/|_|\__| \____/|_| |_|\___|\___|_|\_\");
            Console.WriteLine(@"       copyright © 2022 by Marcius Bezerra");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@$"       {version}");
            Console.ResetColor();
        }
    }
}