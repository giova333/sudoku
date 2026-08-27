using System;
using System.Diagnostics;
using System.IO;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;

namespace Sudoku.Bake
{
    static class Program
    {
        static int Main(string[] args)
        {
            var outputDir = args.Length > 0 ? args[0] : "Assets/_Project/Data/Banks";
            var mainCount = args.Length > 1 ? int.Parse(args[1]) : 2000;
            var dailyCount = args.Length > 2 ? int.Parse(args[2]) : 750;

            Directory.CreateDirectory(outputDir);

            Console.WriteLine($"Baking into {Path.GetFullPath(outputDir)}");
            Console.WriteLine($"  main: {mainCount} per tier, daily: {dailyCount} per tier");
            Console.WriteLine();

            var watch = Stopwatch.StartNew();
            var fellShort = false;

            fellShort |= BakeSet("main", outputDir, mainCount, seedBase: 1_000_000);
            fellShort |= BakeSet("daily", outputDir, dailyCount, seedBase: 9_000_000);

            watch.Stop();
            Console.WriteLine();
            Console.WriteLine($"Done in {watch.Elapsed.TotalMinutes:F1} min.");

            if (fellShort)
            {
                Console.WriteLine("*** One or more banks fell short of the requested count. ***");
                return 1;
            }
            return 0;
        }

        static bool BakeSet(string setName, string outputDir, int count, int seedBase)
        {
            var fellShort = false;

            foreach (DifficultyTier tier in Enum.GetValues(typeof(DifficultyTier)))
            {
                var watch = Stopwatch.StartNew();
                var lastReport = 0;

                var request = new BakeRequest(tier, count, seedBase + (int)tier * 100_000);
                var result = BankBaker.Bake(request, DifficultyProfile.Default, ConstraintSet.Classic,
                    (done, total) =>
                    {
                        if (done - lastReport < Math.Max(1, total / 10)) return;
                        lastReport = done;
                        Console.Write($"\r  {setName}/{tier}: {done}/{total}   ");
                    });

                watch.Stop();
                var path = Path.Combine(outputDir, $"{setName}-{tier}.bytes");
                File.WriteAllBytes(path, PuzzleBankSerializer.Write(tier, result.Puzzles));

                Console.Write("\r");
                Console.WriteLine($"  {setName}/{result.Summary}  " +
                                  $"[{watch.Elapsed.TotalSeconds:F0}s, {new FileInfo(path).Length / 1024}KB]");

                fellShort |= result.FellShort;
            }

            return fellShort;
        }
    }
}
