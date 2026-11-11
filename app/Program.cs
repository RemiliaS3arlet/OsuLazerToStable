using System.Globalization;
using System.Text;

namespace OsuLazerToStable;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        // 无头界面预览：把窗口渲染成 PNG（用于检查 UI 效果）
        if (args.Length > 0 && args[0] == "--ui-preview")
            return UiPreview(args.Length > 1 ? args[1] : "ui-preview.png",
                args.Length > 2 ? int.Parse(args[2]) : 0,
                args.Length > 3 ? args[3] : null,
                args.Length > 4 ? int.Parse(args[4]) : 1060,
                args.Length > 5 ? int.Parse(args[5]) : 720);

        if (args.Any(a => a is "--cli" or "-cli"))
            return CliRunner.Run(args);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    private static int UiPreview(string outputPath, int pageIndex, string? prefillSearch, int width = 1060, int height = 720)
    {
        try
        {
            // 预览用 DPI-unaware 模式，保证截图 1:1 且与物理像素一致
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using var form = new MainForm
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(10, 10),
                ShowInTaskbar = false,
                TopMost = true,
            };
            form.ClientSize = new Size(width, height);
            form.Show();
            form.Activate();
            form.SetPreviewPage(pageIndex);
            for (int i = 0; i < 40; i++)
            {
                Application.DoEvents();
                Thread.Sleep(25);
            }

            if (!string.IsNullOrWhiteSpace(prefillSearch))
            {
                form.PreviewSearch(prefillSearch);
                for (int i = 0; i < 80; i++)
                {
                    Application.DoEvents();
                    Thread.Sleep(50);
                }
            }

            // 用 PrintWindow 抓取窗口内容（不受遮挡影响，且能渲染自绘控件）
            IntPtr hwnd = form.Handle;
            using var full = new Bitmap(form.Width, form.Height);
            using (var g = Graphics.FromImage(full))
            {
                IntPtr hdc = g.GetHdc();
                try { PrintWindow(hwnd, hdc, 2u); } // PW_RENDERFULLCONTENT
                finally { g.ReleaseHdc(hdc); }
            }

            // 裁掉窗口边框，只保留客户区
            int borderX = Math.Max(0, (form.Width - form.ClientSize.Width) / 2);
            int topH = Math.Max(0, form.Height - form.ClientSize.Height - borderX);
            var crop = new Rectangle(borderX, topH, form.ClientSize.Width, form.ClientSize.Height);
            using (var client = full.Clone(crop, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                client.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            form.Close();
            Console.WriteLine("UI preview saved: " + Path.GetFullPath(outputPath));
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("UI preview failed: " + e);
            return 1;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
}

internal static class CliRunner
{
    public static int Run(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* 输出重定向时控制台不可用 */ }

        // ---- sayobot 试听自检 ----
        string? previewArg = GetArg(args, "--sayobot-preview-test");
        if (previewArg != null)
            return RunPreviewTestAsync(previewArg).GetAwaiter().GetResult();

        // ---- sayobot 搜索子命令 ----
        string? searchArg = GetArg(args, "--sayobot-search");
        if (searchArg != null)
            return RunSayobotSearchAsync(searchArg).GetAwaiter().GetResult();

        // ---- sayobot 下载子命令 ----
        string? dlArg = GetArg(args, "--sayobot-dl");
        if (dlArg != null)
            return RunSayobotDlAsync(args, dlArg).GetAwaiter().GetResult();

        // ---- 倍速生成子命令 ----
        string? rateInput = GetArg(args, "--rate");
        if (rateInput != null)
            return RunRate(args, rateInput);

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
                case "--only-missing": opts.OnlyMissing = true; break;
                case "--all-maps": opts.OnlyMissing = false; break;
                case "--detect-only": opts.DetectOnly = true; break;
            }
        }

        if (lazer.Length == 0)
            lazer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "osu");
        if (stable.Length == 0)
            stable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");

        Console.WriteLine($"lazer: {lazer}");
        Console.WriteLine($"stable: {stable}");
        Console.WriteLine($"模式: {(opts.DryRun ? "演练" : "正式")} | 谱面:{(opts.TransferMaps ? "是" : "否")} 收藏夹:{(opts.TransferCollections ? "是" : "否")} 合并:{(opts.MergeCollections ? "是" : "否")} 仅缺失:{(opts.OnlyMissing ? "是" : "否")} 仅检测:{(opts.DetectOnly ? "是" : "否")}");

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
                $"== 差异: stable 缺失 {sum.MissingSets} 集 / 已有 {sum.ExistingSets} 集" +
                $" | 完成: 谱面集 {sum.CopiedSets}/{sum.TotalSets}（跳过 {sum.SkippedSets}）, 文件 {sum.CopiedFiles}, " +
                $"收藏夹新增 {sum.CollectionsWritten}, 收藏引用 {sum.CollectionHashesWritten}（缺失 {sum.CollectionHashesMissing}）");
            if (sum.MissingListPath.Length > 0)
                Console.WriteLine($"缺失清单: {sum.MissingListPath}");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("失败: " + e.Message);
            return 1;
        }
    }

    private static int RunRate(string[] args, string input)
    {
        double speed = double.Parse(GetArg(args, "--speed") ?? "1.25", CultureInfo.InvariantCulture);
        string? outArg = GetArg(args, "--out");
        string baseName = System.Text.RegularExpressions.Regex.Replace(
            Path.GetFileNameWithoutExtension(input), @"^\d+\s+", "");
        string output = outArg ?? Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(input))!,
            $"({speed:0.00}) {baseName}.osz");

        Console.WriteLine($"输入: {input}");
        Console.WriteLine($"倍速: {speed:0.00}x");
        Console.WriteLine($"输出: {output}");
        try
        {
            RateChanger.Generate(input, speed, output, m => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {m}"), !args.Contains("--keep-bg"));
            Console.WriteLine("生成成功: " + output);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("生成失败: " + e.Message);
            return 1;
        }
    }

    private static async Task<int> RunPreviewTestAsync(string sidText)
    {
        try
        {
            if (!long.TryParse(sidText, out long sid))
            {
                Console.WriteLine("sid 无效");
                return 1;
            }
            string path = await SayobotClient.DownloadPreviewAsync(sid, CancellationToken.None);
            Console.WriteLine($"试听文件: {path} ({new FileInfo(path).Length} bytes)");
            using var reader = new NAudio.Vorbis.VorbisWaveReader(path);
            Console.WriteLine($"解码成功: {reader.TotalTime:m\\:ss} / {reader.WaveFormat.SampleRate}Hz / {reader.WaveFormat.Channels}ch");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("试听自检失败: " + e.Message);
            return 1;
        }
    }

    private static async Task<int> RunSayobotSearchAsync(string query)
    {
        try
        {
            var (keyword, filters) = SearchQuery.Parse(query);
            var raw = await SayobotClient.SearchWithDifficultiesAsync(keyword, 20, CancellationToken.None);
            var items = raw.Where(x => filters.Count == 0 || SearchQuery.Matches(x.Set, x.Diff, filters)).ToList();
            string filterText = filters.Count > 0 ? $"（筛选 {SearchQuery.Describe(filters)}）" : "";
            Console.WriteLine($"搜索「{keyword}」{filterText} → 命中 {items.Count}/{raw.Count} 个难度");
            foreach (var (set, diff) in items.Take(40))
            {
                Console.WriteLine(
                    $"{set.Artist} - {set.Title} [{diff.Version}] | {SayobotClient.ModeText(diff.Mode)} | " +
                    $"{SayobotClient.StatusText(set.Approved)} | OD {diff.OD:0.##} HP {diff.HP:0.##} ★{diff.Star:0.##} | sid {set.Sid}");
            }
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("搜索失败: " + e.Message);
            return 1;
        }
    }

    private static async Task<int> RunSayobotDlAsync(string[] args, string input)
    {
        if (!SayobotClient.TryParseSid(input, out long sid))
        {
            Console.WriteLine("无法解析谱面集 ID: " + input);
            return 1;
        }
        string dir = GetArg(args, "--dl-dir") ?? Directory.GetCurrentDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            var info = await SayobotClient.GetMapInfoAsync(sid, CancellationToken.None);
            string fname = TransferEngine.SanitizeName($"{info.Sid} {info.Artist} - {info.Title}.osz");
            string path = Path.Combine(dir, fname);
            Console.WriteLine($"谱面: {info.Artist} - {info.Title}（sid {info.Sid}）");
            Console.WriteLine($"保存: {path}");
            long lastPct = -1;
            await SayobotClient.DownloadAsync(sid, path, (done, total) =>
            {
                if (total > 0)
                {
                    long pct = done * 100 / total;
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        Console.Write($"\r{Math.Min(100, pct)}%   ");
                    }
                }
            }, CancellationToken.None, source => Console.WriteLine($"\n使用下载源: {source}"));
            Console.WriteLine();
            Console.WriteLine("下载完成: " + path);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("下载失败: " + e.Message);
            return 1;
        }
    }

    private static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return null;
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



