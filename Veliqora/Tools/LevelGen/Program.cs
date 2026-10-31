using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Veliqora.Core.Levels;
using Veliqora.Core.Solver;

// Offline level pipeline:
//   dotnet run -c Release --project Tools/LevelGen -- build    [--count 200] [--seed N] [--out <levels.json>] [--report <csv>]
//   dotnet run -c Release --project Tools/LevelGen -- validate [--in <levels.json>] [--report <csv>]
internal static class Program
{
    private static int Main(string[] args)
    {
        string cmd = args.Length > 0 ? args[0] : "build";
        string root = FindProjectRoot();
        string levelsPath = Arg(args, "--out", Arg(args, "--in", Path.Combine(root, "Assets/_Veliqora/Resources/Veliqora/Levels/levels.json")));
        string reportPath = Arg(args, "--report", Path.Combine(root, "Docs", cmd == "validate" ? "level_validation_report.csv" : "level_generation_report.csv"));
        int count = int.Parse(Arg(args, "--count", LevelCatalogBuilder.DefaultLevelCount.ToString()));
        ulong seed = ulong.Parse(Arg(args, "--seed", LevelCatalogBuilder.DefaultBaseSeed.ToString()));

        var sw = Stopwatch.StartNew();
        if (cmd == "build")
        {
            var report = new List<BuildReportRow>();
            var catalog = LevelCatalogBuilder.Build(seed, count, report, (n, total) =>
            {
                var r = report[report.Count - 1];
                Console.WriteLine($"L{n:000} {r.Tier,-12} colors={r.Colors} bottles={r.Bottles} len={r.SolutionLength,3} min={(r.MinimumMoves >= 0 ? r.MinimumMoves.ToString() : "-"),3} nodes={r.Nodes,7} ms={r.Ms,5} br={r.Branching:0.0} {r.Note}");
            });
            Directory.CreateDirectory(Path.GetDirectoryName(levelsPath));
            File.WriteAllText(levelsPath, catalog.ToJson());
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, LevelCatalogBuilder.ToCsv(report));
            Console.WriteLine($"Built {catalog.Levels.Count} levels in {sw.Elapsed.TotalSeconds:0.0}s -> {levelsPath}");
            return 0;
        }
        if (cmd == "validate")
        {
            var catalog = LevelCatalog.FromJson(File.ReadAllText(levelsPath));
            var rows = LevelCatalogBuilder.Validate(catalog, rerunSolver: true, SolverOptions.Validation);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, LevelCatalogBuilder.ToCsv(rows));
            int pass = rows.FindAll(r => r.Passed).Count;
            foreach (var r in rows) if (!r.Passed) Console.WriteLine($"FAIL L{r.Number:000}: {r.Note}");
            Console.WriteLine($"Validated {rows.Count} levels: {pass} PASS, {rows.Count - pass} FAIL in {sw.Elapsed.TotalSeconds:0.0}s -> {reportPath}");
            return pass == rows.Count ? 0 : 1;
        }
        Console.Error.WriteLine("Unknown command " + cmd);
        return 2;
    }

    private static string Arg(string[] args, string name, string fallback)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "_Veliqora"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
