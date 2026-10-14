using System.Text;

namespace OsuLazerToStable;

public class MainForm : Form
{
    // ---- 迁移页 ----
    private TextBox _lazerBox = null!;
    private TextBox _stableBox = null!;
    private CheckBox _chkMaps = null!;
    private CheckBox _chkOnlyMissing = null!;
    private CheckBox _chkCols = null!;
    private CheckBox _chkMerge = null!;
    private CheckBox _chkDry = null!;
    private CheckBox _chkRemoveBg = null!;
    private Button _btnDetect = null!;
    private Button _btnDetectDiff = null!;
    private Button _btnStart = null!;
    private Button _btnCancel = null!;
    private Label _statusLabel = null!;

    // ---- 倍速页 ----
    private Panel _dropPanel = null!;
    private Label _rateFileLabel = null!;
    private NumericUpDown _rateSpeed = null!;
    private Button _btnGenerate = null!;
    private Button _btnOpenFolder = null!;
    private Label _rateStatusLabel = null!;
    private string _rateInput = string.Empty;
    private string _lastOutput = string.Empty;

    private TextBox _logBox = null!;
    private ProgressBar _progress = null!;
    private CancellationTokenSource? _cts;
    private StreamWriter? _fileLog;

    public MainForm()
    {
        Text = "osu!lazer → stable 迁移工具";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 700);
        MinimumSize = new Size(800, 600);
        Font = new Font("Microsoft YaHei UI", 9F);
        BuildUi();
        AutoDetectPaths();
        AppendLog("欢迎使用。迁移页：迁移谱面/收藏夹、检测差异；倍速页：拖入 .osz 生成变速新谱面。");
        AppendLog("迁移不会改动 lazer 的任何数据；写入 collection.db 前会自动备份原文件。");
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        Controls.Add(root);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildMigrateTab());
        tabs.TabPages.Add(BuildRateTab());
        root.Controls.Add(tabs, 0, 0);

        _progress = new ProgressBar { Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
        root.Controls.Add(_progress, 0, 1);

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
    }

    // ==================== 迁移页 ====================

    private TabPage BuildMigrateTab()
    {
        var page = new TabPage("谱面迁移") { Padding = new Padding(8) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(panel);

        // 路径区
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
        panel.Controls.Add(paths, 0, 0);

        // 选项区
        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 6, 0, 0),
        };
        _chkMaps = new CheckBox { Text = "迁移谱面", Checked = true, AutoSize = true };
        _chkOnlyMissing = new CheckBox { Text = "仅迁移 stable 缺失的谱面（推荐）", Checked = true, AutoSize = true };
        _chkCols = new CheckBox { Text = "迁移收藏夹", Checked = true, AutoSize = true };
        _chkMerge = new CheckBox { Text = "同名收藏夹合并", Checked = true, AutoSize = true };
        _chkDry = new CheckBox { Text = "演练模式（不写任何文件）", Checked = false, AutoSize = true };
        _statusLabel = new Label { Text = "尚未检测", AutoSize = true, Margin = new Padding(16, 4, 0, 0) };
        options.Controls.Add(_chkMaps);
        options.Controls.Add(_chkOnlyMissing);
        options.Controls.Add(_chkCols);
        options.Controls.Add(_chkMerge);
        options.Controls.Add(_chkDry);
        options.Controls.Add(_statusLabel);
        panel.Controls.Add(options, 0, 1);

        // 按钮区
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        _btnDetect = new Button { Text = "刷新检测", AutoSize = true };
        _btnDetectDiff = new Button { Text = "检测差异", AutoSize = true, Margin = new Padding(10, 0, 0, 0) };
        _btnStart = new Button { Text = "开始迁移", AutoSize = true, Width = 130, Height = 34, Margin = new Padding(16, 0, 0, 0) };
        _btnCancel = new Button { Text = "取消", AutoSize = true, Height = 34, Enabled = false, Margin = new Padding(10, 0, 0, 0) };
        actions.Controls.Add(_btnDetect);
        actions.Controls.Add(_btnDetectDiff);
        actions.Controls.Add(_btnStart);
        actions.Controls.Add(_btnCancel);
        panel.Controls.Add(actions, 0, 2);

        _btnDetect.Click += async (_, _) => await DetectAsync();
        _btnDetectDiff.Click += async (_, _) => await DetectDiffAsync();
        _btnStart.Click += async (_, _) => await StartTransferAsync();
        _btnCancel.Click += (_, _) => _cts?.Cancel();

        return page;
    }

    // ==================== 倍速页 ====================

    private TabPage BuildRateTab()
    {
        var page = new TabPage("倍速生成") { Padding = new Padding(8) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(panel);

        // 拖放区
        _dropPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 250, 252),
            AllowDrop = true,
            Cursor = Cursors.Hand,
        };
        var dropLabel = new Label
        {
            Text = "把 .osz / .osu 谱面文件拖到这里\n（或点击选择文件）",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei UI", 11F),
        };
        _dropPanel.Controls.Add(dropLabel);
        dropLabel.Click += (_, _) => PickOsz();
        _dropPanel.Click += (_, _) => PickOsz();
        _dropPanel.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                && ((string[])e.Data.GetData(DataFormats.FileDrop)!).Any(f =>
                    f.EndsWith(".osz", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)))
                e.Effect = DragDropEffects.Copy;
        };
        _dropPanel.DragDrop += (_, e) =>
        {
            var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            string? osz = files.FirstOrDefault(f =>
                f.EndsWith(".osz", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));
            if (osz != null) SetRateInput(osz);
        };
        panel.Controls.Add(_dropPanel, 0, 0);

        // 已选文件
        _rateFileLabel = new Label { Text = "未选择文件", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
        panel.Controls.Add(_rateFileLabel, 0, 1);

        // 倍速选择
        var speedRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 6, 0, 0), WrapContents = true };
        speedRow.Controls.Add(new Label { Text = "倍速:", AutoSize = true, Margin = new Padding(0, 4, 6, 0) });
        _rateSpeed = new NumericUpDown
        {
            Minimum = 0.50m,
            Maximum = 2.00m,
            DecimalPlaces = 2,
            Increment = 0.05m,
            Value = 1.25m,
            Width = 90,
        };
        speedRow.Controls.Add(_rateSpeed);
        speedRow.Controls.Add(new Label { Text = "×（范围 0.50 ~ 2.00，精确到 0.01）", AutoSize = true, Margin = new Padding(6, 4, 0, 0) });
        _chkRemoveBg = new CheckBox { Text = "移除背景图（曲绘，推荐）", Checked = true, AutoSize = true, Margin = new Padding(16, 0, 0, 0) };
        speedRow.Controls.Add(_chkRemoveBg);
        _btnGenerate = new Button { Text = "生成变速谱面", AutoSize = true, Height = 34, Margin = new Padding(16, 0, 0, 0) };
        _btnOpenFolder = new Button { Text = "打开所在文件夹", AutoSize = true, Height = 34, Enabled = false, Margin = new Padding(10, 0, 0, 0) };
        speedRow.Controls.Add(_btnGenerate);
        speedRow.Controls.Add(_btnOpenFolder);
        panel.Controls.Add(speedRow, 0, 2);

        _btnGenerate.Click += async (_, _) => await GenerateRateAsync();
        _btnOpenFolder.Click += (_, _) =>
        {
            if (_lastOutput.Length > 0 && File.Exists(_lastOutput))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_lastOutput}\"");
        };

        // 状态与说明
        _rateStatusLabel = new Label
        {
            Text = "",
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
            ForeColor = Color.DimGray,
        };
        panel.Controls.Add(_rateStatusLabel, 0, 3);

        var help = new Label
        {
            Text = "说明：变速会把音频与谱面节奏整体改变（0.50x ~ 2.00x），生成的新文件以 (0.XX) 为前缀另存，不影响原谱面。\r\n" +
                   "支持拖入 .osz 或单个 .osu 文件（.osu 会自动附带同目录下的音频与背景图）。\r\n" +
                   "生成的谱面会被标记为全新本地谱面：ID 清零、标题/难度名带倍速后缀、清空 Source、Tags 末尾随机加一个字母、" +
                   "并移除曲绘——生成谱面与原谱面没有任何相同文件，绕过 stable 的更新检测。\r\n" +
                   "音频变速需要 ffmpeg.exe：请将其放到本程序同目录（或加入系统 PATH），否则无法生成正确的变速谱面。",
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 6, 0, 0),
        };
        panel.Controls.Add(help, 0, 4);

        UpdateFfmpegHint();
        return page;
    }

    private void PickOsz()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "osu! 谱面文件 (*.osz;*.osu)|*.osz;*.osu|osu! 谱面包 (*.osz)|*.osz|osu! 谱面 (*.osu)|*.osu|所有文件 (*.*)|*.*",
            Title = "选择要变速的 .osz / .osu 谱面",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            SetRateInput(dlg.FileName);
    }

    private void SetRateInput(string path)
    {
        _rateInput = path;
        _rateFileLabel.Text = "已选择: " + Path.GetFileName(path);
        _rateStatusLabel.Text = "";
        AppendLog($"[倍速] 已选择 {path}");
    }

    private void UpdateFfmpegHint()
    {
        string ff = RateChanger.FindFfmpeg();
        _rateStatusLabel.Text = ff.Length > 0
            ? $"ffmpeg: 已找到（{ff}）"
            : "⚠ 未找到 ffmpeg.exe —— 生成变速谱面需要它（放到程序同目录或加入 PATH）";
    }

    private async Task GenerateRateAsync()
    {
        if (_cts != null) return;
        if (_rateInput.Length == 0 || !File.Exists(_rateInput))
        {
            MessageBox.Show(this, "请先拖入或选择一个 .osz 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        double speed = (double)_rateSpeed.Value;
        if (speed < 0.5 || speed > 2.0)
        {
            MessageBox.Show(this, "倍速必须在 0.50 ~ 2.00 之间。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string prefix = $"({speed:0.00})";
        // 去掉原文件名开头的谱面集 ID（如 "2606612 "），避免 stable 用文件名建目录时与原谱面混淆
        string baseName = System.Text.RegularExpressions.Regex.Replace(
            Path.GetFileNameWithoutExtension(_rateInput), @"^\d+\s+", "");
        string defaultName = $"{prefix} {baseName}.osz";
        using var sfd = new SaveFileDialog
        {
            Filter = "osu! 谱面包 (*.osz)|*.osz",
            FileName = defaultName,
            InitialDirectory = Path.GetDirectoryName(_rateInput),
            Title = "选择变速谱面的保存位置",
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        if (RateChanger.FindFfmpeg().Length == 0 && Math.Abs(speed - 1.0) > 0.0001)
        {
            var answer = MessageBox.Show(this,
                "未找到 ffmpeg.exe，无法对音频变速。\r\n生成的谱面节奏会加快，但音乐仍是原速（不同步）。\r\n\r\n仍要生成吗？",
                "缺少 ffmpeg", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;
        }

        try
        {
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
            _fileLog = new StreamWriter(
                Path.Combine(AppContext.BaseDirectory, "logs", $"rate-{DateTime.Now:yyyyMMdd-HHmmss}.log"),
                append: true, Encoding.UTF8) { AutoFlush = true };
        }
        catch
        {
            _fileLog = null;
        }

        _cts = new CancellationTokenSource();
        _btnGenerate.Enabled = false;
        _btnOpenFolder.Enabled = false;
        _progress.Value = 0;
        _progress.Style = ProgressBarStyle.Marquee;
        AppendLog($"========== 开始生成 {prefix} 倍速谱面 ==========");

        string output = sfd.FileName;
        await Task.Run(() =>
        {
            try
            {
                RateChanger.Generate(_rateInput, speed, output, msg => BeginInvoke(() => AppendLog(msg)), _chkRemoveBg.Checked);
                _lastOutput = output;
                BeginInvoke(() =>
                {
                    AppendLog($"✅ 生成成功: {output}");
                    _btnOpenFolder.Enabled = true;
                    _progress.Style = ProgressBarStyle.Blocks;
                    _progress.Value = 0;
                    _btnGenerate.Enabled = true;
                    _cts?.Dispose();
                    _cts = null;
                    _fileLog?.Dispose();
                    _fileLog = null;
                    MessageBox.Show(this, $"变速谱面已生成:\r\n{output}", "生成成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch (Exception e)
            {
                BeginInvoke(() =>
                {
                    AppendLog("[错误] " + e.Message);
                    _progress.Style = ProgressBarStyle.Blocks;
                    _progress.Value = 0;
                    _btnGenerate.Enabled = true;
                    _cts?.Dispose();
                    _cts = null;
                    _fileLog?.Dispose();
                    _fileLog = null;
                });
            }
        });
    }

    // ==================== 迁移逻辑 ====================

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

    private async Task DetectDiffAsync()
    {
        if (_cts != null) return;
        string lazer = _lazerBox.Text.Trim();
        string stable = _stableBox.Text.Trim();
        _btnDetectDiff.Enabled = false;
        _statusLabel.Text = "差异检测中…";
        AppendLog("========== 差异检测 ==========");

        var opts = new TransferOptions { TransferMaps = true, TransferCollections = false, DetectOnly = true, DryRun = true };
        var engine = new TransferEngine
        {
            Log = msg => BeginInvoke(() => AppendLog(msg)),
            Progress = (_, _) => { },
        };

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        await Task.Run(() =>
        {
            try
            {
                var sum = engine.Run(lazer, stable, opts, token);
                BeginInvoke(() =>
                {
                    _statusLabel.Text = $"lazer 有 {sum.MissingSets} 个谱面集在 stable 中缺失（stable 已有 {sum.ExistingSets} 个）";
                    if (sum.MissingListPath.Length > 0)
                        AppendLog($"缺失清单: {sum.MissingListPath}");
                    _btnDetectDiff.Enabled = true;
                    _cts?.Dispose();
                    _cts = null;
                });
            }
            catch (Exception e)
            {
                BeginInvoke(() =>
                {
                    _statusLabel.Text = "差异检测失败";
                    AppendLog("[错误] " + e.Message);
                    _btnDetectDiff.Enabled = true;
                    _cts?.Dispose();
                    _cts = null;
                });
            }
        });
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
            OnlyMissing = _chkOnlyMissing.Checked,
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
        _btnDetect.Enabled = false;
        _btnDetectDiff.Enabled = false;
        _btnCancel.Enabled = true;
        _progress.Value = 0;
        _progress.Style = ProgressBarStyle.Blocks;
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
                BeginInvoke(() => OnFinished(sum, cancelled: false));
            }
            catch (OperationCanceledException)
            {
                BeginInvoke(() => OnFinished(null, cancelled: true));
            }
            catch (Exception e)
            {
                BeginInvoke(() => OnError(e));
            }
        });
    }

    private void OnFinished(TransferSummary? sum, bool cancelled)
    {
        if (cancelled)
        {
            AppendLog("[已取消] 迁移被用户取消。已完成的文件会保留，请重新运行以继续。");
        }
        else if (sum != null)
        {
            AppendLog($"== 差异检测: lazer 缺失 {sum.MissingSets} 集 / stable 已有 {sum.ExistingSets} 集");
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
        _btnDetect.Enabled = true;
        _btnDetectDiff.Enabled = true;
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
