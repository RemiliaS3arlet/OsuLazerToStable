using System.Text;

namespace OsuLazerToStable;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Any(a => a is "--cli" or "-cli"))
            return CliRunner.Run(args);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}

internal static class CliRunner
{
    public static int Run(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* 输出重定向时控制台不可用 */ }

        string lazer = string.Empty, stable = string.Empty;
        var opts = new TransferOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--lazer" when i + 1 < args.Length: lazer = args[++i]; break;
                case "--stable" when i + 1 < args.Length: stable = args[++i]; break;
                case "--maps": opts.TransferMaps = true; break;
                case "--no-maps": opts.TransferMaps = false; break;
                case "--collections": opts.TransferCollections = true; break;
                case "--no-collections": opts.TransferCollections = false; break;
                case "--dry-run": opts.DryRun = true; break;
                case "--merge": opts.MergeCollections = true; break;
                case "--no-merge": opts.MergeCollections = false; break;
            }
        }

        if (lazer.Length == 0)
            lazer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "osu");
        if (stable.Length == 0)
            stable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");

        Console.WriteLine($"lazer: {lazer}");
        Console.WriteLine($"stable: {stable}");
        Console.WriteLine($"模式: {(opts.DryRun ? "演练" : "正式")} | 谱面:{(opts.TransferMaps ? "是" : "否")} 收藏夹:{(opts.TransferCollections ? "是" : "否")} 合并:{(opts.MergeCollections ? "是" : "否")}");

        if (args.Contains("--selftest-colldb"))
            return SelfTestCollectionDb(stable);

        var engine = new TransferEngine
        {
            Log = m => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {m}"),
            Progress = (cur, total) =>
            {
                if (cur % 500 == 0 || cur == total)
                    Console.Write($"\r文件进度 {cur}/{total}   ");
            },
        };

        try
        {
            var sum = engine.Run(lazer, stable, opts, CancellationToken.None);
            Console.WriteLine();
            Console.WriteLine(
                $"== 完成: 谱面集 {sum.CopiedSets}/{sum.TotalSets}（跳过 {sum.SkippedSets}）, 文件 {sum.CopiedFiles}, " +
                $"收藏夹新增 {sum.CollectionsWritten}, 收藏引用 {sum.CollectionHashesWritten}（缺失 {sum.CollectionHashesMissing}）");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("失败: " + e.Message);
            return 1;
        }
    }

    /// <summary>读取 stable 的 collection.db，原样写回临时文件并做字节比对，验证读写格式与 stable 一致。</summary>
    private static int SelfTestCollectionDb(string stableDir)
    {
        string dbPath = Path.Combine(stableDir, "collection.db");
        if (!File.Exists(dbPath))
        {
            Console.WriteLine("未找到 collection.db，跳过自检");
            return 0;
        }

        Console.WriteLine("读取: " + dbPath);
        var db = CollectionDbFile.Read(dbPath);
        Console.WriteLine($"版本 {db.Version}, {db.Collections.Count} 个收藏夹");

        string tmp = Path.Combine(AppContext.BaseDirectory, "colldb-selftest.bin");
        db.Write(tmp);

        byte[] a = File.ReadAllBytes(dbPath);
        byte[] b = File.ReadAllBytes(tmp);
        bool same = a.SequenceEqual(b);
        Console.WriteLine($"往返字节一致: {same}（{a.Length} vs {b.Length} 字节）");
        if (!same)
        {
            int limit = Math.Min(a.Length, b.Length);
            int diff = -1;
            for (int i = 0; i < limit; i++)
                if (a[i] != b[i]) { diff = i; break; }
            if (diff < 0) diff = limit;
            Console.WriteLine($"首个差异位置: {diff}" +
                              (diff < a.Length ? $"（原 {a[diff]:X2} vs 新 {b[diff]:X2}）" : "（长度不同）"));
        }
        File.Delete(tmp);
        return same ? 0 : 1;
    }
}
