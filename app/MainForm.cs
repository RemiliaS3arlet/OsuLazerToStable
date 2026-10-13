using System.Text;

namespace OsuLazerToStable;

public class MainForm : Form
{
    private TextBox _lazerBox = null!;
    private TextBox _stableBox = null!;
    private CheckBox _chkMaps = null!;
    private CheckBox _chkCols = null!;
    private CheckBox _chkMerge = null!;
    private CheckBox _chkDry = null!;
    private TextBox _logBox = null!;
    private ProgressBar _progress = null!;
    private Button _btnStart = null!;
    private Button _btnCancel = null!;
    private Button _btnDetect = null!;
    private Label _statusLabel = null!;
    private CancellationTokenSource? _cts;
    private StreamWriter? _fileLog;

    public MainForm()
    {
        Text = "osu!lazer → stable 迁移工具";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(880, 640);
        MinimumSize = new Size(780, 540);
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildUi();
        AutoDetectPaths();
        AppendLog("欢迎使用。点击「刷新检测」查看两边的数据，确认无误后点击「开始迁移」。");
        AppendLog("迁移不会改动 lazer 的任何数据；写入 collection.db 前会自动备份原文件。");
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        // ---- 路径区 ----
        var paths = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3 };
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lazerBox = new TextBox { Dock = DockStyle.Fill };
        _stableBox = new TextBox { Dock = DockStyle.Fill };
        var btnLazer = new Button { Text = "浏览…", AutoSize = true };
        var btnStable = new Button { Text = "浏览…", AutoSize = true };

        paths.Controls.Add(new Label { Text = "lazer 数据目录", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) }, 0, 0);
        paths.Controls.Add(_lazerBox, 1, 0);
        paths.Controls.Add(btnLazer, 2, 0);
        paths.Controls.Add(new Label { Text = "stable 安装目录", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 0) }, 0, 1);
        paths.Controls.Add(_stableBox, 1, 1);
        paths.Controls.Add(btnStable, 2, 1);

        btnLazer.Click += (_, _) => PickFolder(_lazerBox, "选择 osu!lazer 数据目录（含 client.realm 和 files 的目录）");
        btnStable.Click += (_, _) => PickFolder(_stableBox, "选择 osu!stable 安装目录（含 osu!.exe 的目录）");
        root.Controls.Add(paths, 0, 0);

        // ---- 选项区 ----
        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 6, 0, 0),
        };
        _chkMaps = new CheckBox { Text = "迁移谱面", Checked = true, AutoSize = true };
        _chkCols = new CheckBox { Text = "迁移收藏夹", Checked = true, AutoSize = true };
        _chkMerge = new CheckBox { Text = "同名收藏夹合并（否则以 lazer 为准）", Checked = true, AutoSize = true };
        _chkDry = new CheckBox { Text = "演练模式（不写任何文件）", Checked = false, AutoSize = true };
        _btnDetect = new Button { Text = "刷新检测", AutoSize = true, Margin = new Padding(12, 0, 0, 0) };
        _statusLabel = new Label { Text = "尚未检测", AutoSize = true, Margin = new Padding(16, 4, 0, 0) };

        options.Controls.Add(_chkMaps);
        options.Controls.Add(_chkCols);
        options.Controls.Add(_chkMerge);
        options.Controls.Add(_chkDry);
        options.Controls.Add(_btnDetect);
        options.Controls.Add(_statusLabel);
        root.Controls.Add(options, 0, 1);

        _btnDetect.Click += async (_, _) => await DetectAsync();

        // ---- 按钮区 ----
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        _btnStart = new Button { Text = "开始迁移", AutoSize = true, Width = 130, Height = 34 };
        _btnCancel = new Button { Text = "取消", AutoSize = true, Height = 34, Enabled = false, Margin = new Padding(10, 0, 0, 0) };
        _progress = new ProgressBar { Width = 420, Height = 24, Anchor = AnchorStyles.Left, Margin = new Padding(14, 6, 0, 0) };
        actions.Controls.Add(_btnStart);
        actions.Controls.Add(_btnCancel);
        actions.Controls.Add(_progress);
        root.Controls.Add(actions, 0, 2);

        _btnStart.Click += async (_, _) => await StartTransferAsync();
        _btnCancel.Click += (_, _) => _cts?.Cancel();

        // ---- 日志区 ----
        _logBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            Margin = new Padding(0, 8, 0, 0),
        };
        root.Controls.Add(_logBox, 0, 3);
        root.SetRowSpan(_logBox, 2);
    }

    private void PickFolder(TextBox box, string description)
    {
        using var dlg = new FolderBrowserDialog { Description = description, UseDescriptionForTitle = true };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            box.Text = dlg.SelectedPath;
    }

    private void AutoDetectPaths()
    {
        string lazer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "osu");
        if (!File.Exists(Path.Combine(lazer, "client.realm")))
        {
            string alt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu");
            if (File.Exists(Path.Combine(alt, "client.realm"))) lazer = alt;
        }
        _lazerBox.Text = lazer;

        string stable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
        if (!File.Exists(Path.Combine(stable, "osu!.exe")))
        {
            foreach (string cand in new[] { @"D:\osu!", @"E:\osu!", @"C:\osu!", @"D:\Games\osu!", @"E:\Games\osu!" })
                if (File.Exists(Path.Combine(cand, "osu!.exe")))
                {
                    stable = cand;
                    break;
                }
        }
        _stableBox.Text = stable;
    }

    private async Task DetectAsync()
    {
        string lazer = _lazerBox.Text.Trim();
        string stable = _stableBox.Text.Trim();
        _btnDetect.Enabled = false;
        _statusLabel.Text = "检测中…";
        try
        {
            var result = await Task.Run(() =>
            {
                var l = LazerData.Read(lazer);
                int ids = Directory.Exists(Path.Combine(stable, "Songs"))
                    ? StableData.GetExistingOnlineIds(Path.Combine(stable, "Songs")).Count
                    : -1;
                return (l, ids);
            });

            string songInfo = result.ids < 0
                ? "Songs 目录不存在"
                : $"stable 已有 {result.ids} 个曲目文件夹";
            _statusLabel.Text =
                $"lazer: {result.l.Sets.Count} 个谱面集 / {result.l.Collections.Count} 个收藏夹（{result.l.DeletedSets} 个已删除未计） | {songInfo}";
            AppendLog($"检测完成: lazer {result.l.Sets.Count} 集 / {result.l.Collections.Count} 收藏夹; {songInfo}");
        }
        catch (Exception e)
        {
            _statusLabel.Text = "检测失败";
            AppendLog("[错误] " + e.Message);
        }
        finally
        {
            _btnDetect.Enabled = true;
        }
    }

    private async Task StartTransferAsync()
    {
        if (_cts != null) return;

        string lazer = _lazerBox.Text.Trim();
        string stable = _stableBox.Text.Trim();
        var opts = new TransferOptions
        {
            TransferMaps = _chkMaps.Checked,
            TransferCollections = _chkCols.Checked,
            MergeCollections = _chkMerge.Checked,
            DryRun = _chkDry.Checked,
        };

        if (!opts.TransferMaps && !opts.TransferCollections)
        {
            MessageBox.Show(this, "请至少勾选一项要迁移的内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
            _fileLog = new StreamWriter(
                Path.Combine(AppContext.BaseDirectory, "logs", $"transfer-{DateTime.Now:yyyyMMdd-HHmmss}.log"),
                append: true, Encoding.UTF8) { AutoFlush = true };
        }
        catch
        {
            _fileLog = null;
        }

        _cts = new CancellationTokenSource();
        _btnStart.Enabled = false;
        _btnCancel.Enabled = true;
        _progress.Value = 0;
        AppendLog("========== 开始迁移 ==========");

        var engine = new TransferEngine
        {
            Log = msg => BeginInvoke(() => AppendLog(msg)),
            Progress = (cur, total) => BeginInvoke(() =>
            {
                _progress.Maximum = Math.Max(1, total);
                _progress.Value = Math.Min(cur, _progress.Maximum);
            }),
        };

        var token = _cts.Token;
        await Task.Run(() =>
        {
            try
            {
                var sum = engine.Run(lazer, stable, opts, token);
                BeginInvoke(() => OnFinished(sum, opts.DryRun, cancelled: false));
            }
            catch (OperationCanceledException)
            {
                BeginInvoke(() => OnFinished(null, opts.DryRun, cancelled: true));
            }
            catch (Exception e)
            {
                BeginInvoke(() => OnError(e));
            }
        });
    }

    private void OnFinished(TransferSummary? sum, bool dryRun, bool cancelled)
    {
        if (cancelled)
        {
            AppendLog("[已取消] 迁移被用户取消。已完成的文件会保留，请重新运行以继续。");
        }
        else if (sum != null)
        {
            AppendLog($"== 汇总: 谱面集 {sum.CopiedSets}/{sum.TotalSets}（跳过 {sum.SkippedSets}）" +
                      $"，文件 {sum.CopiedFiles}（{FormatBytes(sum.CopiedBytes)}）" +
                      $"，收藏夹 {sum.CollectionsWritten} 个，收藏引用 {sum.CollectionHashesWritten}（lazer 中缺失未收录 {sum.CollectionHashesMissing}）");
            if (sum.CollectionBackupPath.Length > 0)
                AppendLog($"原 collection.db 备份: {sum.CollectionBackupPath}");
        }
        ResetButtons();
    }

    private void OnError(Exception e)
    {
        AppendLog("[错误] " + e.Message);
        ResetButtons();
    }

    private void ResetButtons()
    {
        _btnStart.Enabled = true;
        _btnCancel.Enabled = false;
        _cts?.Dispose();
        _cts = null;
        _fileLog?.Dispose();
        _fileLog = null;
    }

    private void AppendLog(string msg)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        _logBox.AppendText(line + Environment.NewLine);
        try { _fileLog?.WriteLine(line); } catch { }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F1} MB";
        return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
    }
}
