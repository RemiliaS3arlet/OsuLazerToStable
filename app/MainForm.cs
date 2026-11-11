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
    private RoundedButton _btnDetect = null!;
    private RoundedButton _btnDetectDiff = null!;
    private RoundedButton _btnStart = null!;
    private RoundedButton _btnCancel = null!;
    private Label _statusLabel = null!;

    // ---- 倍速页 ----
    private DropZone _dropPanel = null!;
    private Label _rateFileLabel = null!;
    private NumericUpDown _rateSpeed = null!;
    private RoundedButton _btnGenerate = null!;
    private RoundedButton _btnOpenFolder = null!;
    private Label _rateStatusLabel = null!;
    private string _rateInput = string.Empty;
    private string _lastOutput = string.Empty;

    // ---- 段位计算页 ----
    private ComboBox _danMode = null!;
    private ComboBox _danCourse = null!;
    private ComboBox _danLevel = null!;
    private readonly TextBox[] _danSong = new TextBox[4];
    private readonly NumericUpDown[] _danPass = new NumericUpDown[4];
    private readonly Label[] _danInd = new Label[4];
    private readonly Label[] _danIndExact = new Label[4];
    private bool _danLoading;

    // ---- 谱面下载页 ----
    private TextBox _dlBox = null!;
    private TextBox _dlDirBox = null!;
    private TextBox _dlSearchBox = null!;
    private RoundedButton _btnSearch = null!;
    private ListView _searchList = null!;
    private ComboBox _sortCombo = null!;
    private RoundedButton _btnSortDir = null!;
    private PictureBox _coverBox = null!;
    private Label _coverInfo = null!;
    private long _coverSid = -1;
    private readonly Dictionary<long, Image> _coverCache = new();
    private readonly HashSet<long> _coverRequested = new();
    private List<(SayobotSearchSet Set, SayobotDifficulty Diff)> _searchItems = new();
    private List<SearchFilter> _searchFilters = new();
    private bool _sortDesc = true;
    private RoundedButton _btnPreview = null!;
    private ToolTip _previewTip = null!;
    private bool _previewPaused;
    private NAudio.Wave.WaveOutEvent? _audioOut;
    private NAudio.Vorbis.VorbisWaveReader? _vorbisReader;
    private Label _dlStatus = null!;
    private RoundedButton _btnDl = null!;
    private RoundedButton _btnDlOpenFolder = null!;
    private string _dlLastFile = string.Empty;

    private FlatProgress _progress = null!;
    private Panel _contentHost = null!;
    private readonly List<Panel> _pages = new();
    private readonly List<NavButton> _nav = new();
    private CancellationTokenSource? _cts;
    private StreamWriter? _fileLog;
    private Label _statusStrip = null!;
    private Label _statSets = null!;
    private Label _statCols = null!;
    private Label _statMissing = null!;

    public MainForm()
    {
        Text = "osu! Lazer → stable 工具箱";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1060, 720);
        MinimumSize = new Size(1046, 660);
        BackColor = Theme.Bg;
        Font = Theme.FontBase;
        ForeColor = Theme.Text;
        BuildUi();
        AutoDetectPaths();
        AppendLog("欢迎使用。迁移页：迁移谱面/收藏夹、检测差异；倍速页：拖入 .osz/.osu 生成变速谱面；");
        AppendLog("谱面下载页：从 osu.sayobot.cn 下载 .osz；段位计算页：4K/6K/7K 段位单曲 ACC 计算。");
        AppendLog("迁移不会改动 lazer 的任何数据；写入 collection.db 前会自动备份原文件。");
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Bg,
            Padding = new Padding(0),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        Controls.Add(root);

        // ---- 顶部横幅 ----
        var banner = new BannerPanel
        {
            Title = "osu! Lazer → stable 工具箱",
            Subtitle = "谱面迁移  ·  收藏夹同步  ·  倍速生成  ·  sayobot 下载  ·  段位 ACC 计算",
            BannerImage = LoadBanner(),
        };
        root.Controls.Add(banner, 0, 0);

        // ---- 主体：左侧导航 + 内容区 ----
        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Bg,
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(main, 0, 1);

        var navWrap = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(10, 6, 6, 6) };
        main.Controls.Add(navWrap, 0, 0);

        var navFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Bg,
            Padding = new Padding(0),
            Margin = new Padding(0),
        };
        navWrap.Controls.Add(navFlow);
        navWrap.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Theme.Bg });

        string[] navNames = { "谱面迁移", "倍速生成", "谱面下载", "段位计算" };
        for (int i = 0; i < navNames.Length; i++)
        {
            var nb = new NavButton { Caption = navNames[i], Width = 168, Height = 46 };
            int index = i;
            nb.Activated += (_, _) => SelectPage(index);
            navFlow.Controls.Add(nb);
            _nav.Add(nb);
        }

        _contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        main.Controls.Add(_contentHost, 1, 0);
        AddPage(BuildMigratePage());
        AddPage(BuildRatePage());
        AddPage(BuildDownloadPage());
        AddPage(BuildDanPage());

        // ---- 底部：进度条 + 单行状态 ----
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Bg,
            Padding = new Padding(16, 0, 16, 10),
        };
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(bottom, 0, 2);

        _progress = new FlatProgress { Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 4) };
        bottom.Controls.Add(_progress, 0, 0);

        _statusStrip = new Label
        {
            Text = "",
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(2, 0, 0, 0),
        };
        bottom.Controls.Add(_statusStrip, 0, 1);

        SelectPage(0);
    }

    public void SetPreviewPage(int idx)
    {
        if (idx >= 0 && idx < _pages.Count) SelectPage(idx);
    }

    /// <summary>供界面预览使用：同步执行一次搜索并填充结果列表。</summary>
    public void PreviewSearch(string query)
    {
        try
        {
            _dlSearchBox.Text = query;
            var items = Task.Run(() => SayobotClient.SearchWithDifficultiesAsync(query, 20, CancellationToken.None))
                .GetAwaiter().GetResult();
            _searchItems = items;
            FillSearchResults();
            _dlStatus.Text = $"找到 {items.Count} 个难度（{query}）";
            if (_searchList.Items.Count > 0) _searchList.Items[0].Selected = true;
        }
        catch (Exception e)
        {
            _dlStatus.Text = "搜索失败: " + e.Message;
        }
    }

    private static Image? LoadBanner()
    {
        try
        {
            using var s = typeof(MainForm).Assembly.GetManifestResourceStream("OsuLazerToStable.assets.banner.png");
            if (s == null) return null;
            return Image.FromStream(s);
        }
        catch
        {
            return null;
        }
    }

    private void AddPage(Panel page)
    {
        page.Dock = DockStyle.Fill;
        page.Visible = false;
        page.BackColor = Theme.Bg;
        _contentHost.Controls.Add(page);
        _pages.Add(page);
    }

    private void SelectPage(int idx)
    {
        for (int i = 0; i < _pages.Count; i++)
            _pages[i].Visible = i == idx;
        for (int i = 0; i < _nav.Count; i++)
        {
            _nav[i].Selected = i == idx;
            _nav[i].Invalidate();
        }
        if (idx < _pages.Count) _pages[idx].BringToFront();
    }

    private static Label SectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = Theme.FontSection,
            ForeColor = Theme.Text,
            Margin = new Padding(2, 0, 0, 6),
        };
    }

    private static Label PageHint(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Height = 34,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
        };
    }

    private static Panel Wrap(Control inner, int radius = 8, int padH = 10, int padV = 5)
    {
        var wrap = new BorderPanel
        {
            Dock = DockStyle.Fill,
            Radius = radius,
            BorderColor = Theme.ColorFromHex("#333a4a"),
            FillColor = Theme.SurfaceAlt,
            Padding = new Padding(padH, padV, padH, padV),
            Margin = new Padding(0),
        };
        inner.Dock = DockStyle.Fill;
        wrap.Controls.Add(inner);
        return wrap;
    }

    private static RoundedButton MakeButton(string text, RoundedButton.Style style = RoundedButton.Style.Secondary, int width = 0)
    {
        return new RoundedButton
        {
            Text = text,
            Variant = style,
            Width = width > 0 ? width : TextRenderer.MeasureText(text, Theme.FontBase).Width + 40,
            Height = 32,
            Dock = DockStyle.None,
        };
    }

    private static (Panel Tile, Label Value) MakeStatTile(string caption)
    {
        var tile = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 12);
        tile.Dock = DockStyle.Fill;
        tile.Margin = new Padding(0, 0, 12, 0);
        var value = new Label
        {
            Text = "-",
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold),
            ForeColor = Theme.Text,
            Margin = new Padding(0),
            Height = 34,
        };
        var cap = new Label
        {
            Text = caption,
            AutoSize = true,
            Dock = DockStyle.Bottom,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
        };
        tile.Controls.Add(value);
        tile.Controls.Add(cap);
        return (tile, value);
    }

    // ==================== 迁移页 ====================

    private Panel BuildMigratePage()
    {
        var page = new Panel { Padding = new Padding(16, 14, 16, 14) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8, BackColor = Theme.Bg };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(panel);

        panel.Controls.Add(SectionTitle("谱面迁移"), 0, 0);

        // 路径卡片
        var pathCard = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 14);
        pathCard.Dock = DockStyle.Fill;
        pathCard.AutoSize = true;
        var paths = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, BackColor = Color.Transparent };
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        paths.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        paths.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _lazerBox = new TextBox();
        Theme.StyleText(_lazerBox);
        _stableBox = new TextBox();
        Theme.StyleText(_stableBox);
        var btnLazer = MakeButton("浏览…", RoundedButton.Style.Ghost);
        var btnStable = MakeButton("浏览…", RoundedButton.Style.Ghost);

        paths.Controls.Add(new Label { Text = "lazer 目录", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.TextDim }, 0, 0);
        paths.Controls.Add(Wrap(_lazerBox), 1, 0);
        paths.Controls.Add(new Label { Text = "stable 目录", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.TextDim, Margin = new Padding(0, 10, 0, 0) }, 0, 1);
        var stableWrap = Wrap(_stableBox);
        stableWrap.Margin = new Padding(0, 10, 0, 0);
        paths.Controls.Add(stableWrap, 1, 1);

        var lazerBtnWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        lazerBtnWrap.Controls.Add(btnLazer);
        var stableBtnWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 10, 0, 0) };
        stableBtnWrap.Controls.Add(btnStable);
        paths.Controls.Add(lazerBtnWrap, 2, 0);
        paths.Controls.Add(stableBtnWrap, 2, 1);
        btnLazer.Click += (_, _) => PickFolder(_lazerBox, "选择 osu!lazer 数据目录（含 client.realm 和 files 的目录）");
        btnStable.Click += (_, _) => PickFolder(_stableBox, "选择 osu!stable 安装目录（含 osu!.exe 的目录）");
        pathCard.Controls.Add(paths);
        panel.Controls.Add(pathCard, 0, 1);

        // 选项卡片
        var optCard = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 12);
        optCard.Dock = DockStyle.Fill;
        optCard.AutoSize = true;
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.Transparent };
        _chkMaps = new CheckBox { Text = "迁移谱面", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 18, 4) };
        _chkOnlyMissing = new CheckBox { Text = "仅迁移缺失谱面（推荐）", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 18, 4) };
        _chkCols = new CheckBox { Text = "迁移收藏夹", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 18, 4) };
        _chkMerge = new CheckBox { Text = "同名收藏夹合并", Checked = true, AutoSize = true, Margin = new Padding(0, 4, 18, 4) };
        _chkDry = new CheckBox { Text = "演练模式（不写文件）", Checked = false, AutoSize = true, Margin = new Padding(0, 4, 18, 4) };
        foreach (var cb in new[] { _chkMaps, _chkOnlyMissing, _chkCols, _chkMerge, _chkDry })
        {
            Theme.StyleCheck(cb);
            options.Controls.Add(cb);
        }
        _statusLabel = new Label
        {
            Text = "尚未检测",
            AutoSize = true,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(4, 7, 0, 4),
        };
        options.Controls.Add(_statusLabel);
        optCard.Controls.Add(options);
        panel.Controls.Add(optCard, 0, 2);

        // 按钮行
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 5, BackColor = Theme.Bg, Margin = new Padding(0, 10, 0, 0) };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _btnDetect = MakeButton("刷新检测");
        _btnDetectDiff = MakeButton("检测差异");
        _btnStart = MakeButton("开始迁移", RoundedButton.Style.Primary);
        _btnCancel = MakeButton("取消");
        _btnCancel.Enabled = false;
        foreach (var b in new[] { _btnDetect, _btnDetectDiff, _btnStart, _btnCancel })
        {
            b.Dock = DockStyle.Fill;
            b.Margin = new Padding(0, 0, 8, 0);
        }
        actions.Controls.Add(_btnDetect, 0, 0);
        actions.Controls.Add(_btnDetectDiff, 1, 0);
        actions.Controls.Add(_btnStart, 2, 0);
        actions.Controls.Add(_btnCancel, 3, 0);
        panel.Controls.Add(actions, 0, 3);

        // 数据统计卡片
        var statsRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, BackColor = Theme.Bg, Margin = new Padding(0, 14, 0, 0) };
        statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
        var (tileSets, vSets) = MakeStatTile("lazer 谱面集");
        var (tileCols, vCols) = MakeStatTile("lazer 收藏夹");
        var (tileMissing, vMissing) = MakeStatTile("stable 缺失谱面集");
        tileMissing.Margin = new Padding(0);
        _statSets = vSets;
        _statCols = vCols;
        _statMissing = vMissing;
        statsRow.Controls.Add(tileSets, 0, 0);
        statsRow.Controls.Add(tileCols, 1, 0);
        statsRow.Controls.Add(tileMissing, 2, 0);
        panel.Controls.Add(statsRow, 0, 4);

        // 说明
        var help = new Label
        {
            Text = "只读访问 lazer 数据，绝不修改 lazer 的任何文件；写入 collection.db 前自动备份。\r\n" +
                   "「刷新检测」读取两边数据；「检测差异」列出 lazer 中有、stable 中缺失的谱面集并生成清单。",
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(2, 12, 0, 0),
        };
        panel.Controls.Add(help, 0, 5);

        // 工作流程提示卡
        var flowCard = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 12);
        flowCard.Dock = DockStyle.Fill;
        flowCard.AutoSize = true;
        flowCard.Margin = new Padding(0, 22, 0, 0);
        var steps = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.Transparent };
        string[] stepTexts =
        {
            "① 确认 lazer / stable 路径",
            "② 刷新检测 或 检测差异",
            "③ 勾选内容 → 开始迁移",
            "④ 重启 osu!stable 生效",
        };
        foreach (var step in stepTexts)
        {
            steps.Controls.Add(new Label
            {
                Text = step,
                AutoSize = true,
                ForeColor = Theme.TextDim,
                Font = Theme.FontSmall,
                Margin = new Padding(2, 4, 28, 4),
            });
        }
        flowCard.Controls.Add(steps);
        panel.Controls.Add(flowCard, 0, 6);

        _btnDetect.Click += async (_, _) => await DetectAsync();
        _btnDetectDiff.Click += async (_, _) => await DetectDiffAsync();
        _btnStart.Click += async (_, _) => await StartTransferAsync();
        _btnCancel.Click += (_, _) => _cts?.Cancel();
        return page;
    }

    // ==================== 倍速页 ====================

    private Panel BuildRatePage()
    {
        var page = new Panel { Padding = new Padding(16, 14, 16, 14) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, BackColor = Theme.Bg };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(panel);

        panel.Controls.Add(SectionTitle("倍速生成"), 0, 0);

        _dropPanel = new DropZone
        {
            Dock = DockStyle.Fill,
            Caption = "把 .osz / .osu 谱面文件拖到这里\n（或点击选择文件）",
        };
        _dropPanel.Click += (_, _) => PickOsz();
        _dropPanel.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                && ((string[])e.Data.GetData(DataFormats.FileDrop)!).Any(f =>
                    f.EndsWith(".osz", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)))
            {
                e.Effect = DragDropEffects.Copy;
                _dropPanel.DragOver = true;
                _dropPanel.Invalidate();
            }
        };
        _dropPanel.DragLeave += (_, _) =>
        {
            _dropPanel.DragOver = false;
            _dropPanel.Invalidate();
        };
        _dropPanel.DragDrop += (_, e) =>
        {
            _dropPanel.DragOver = false;
            _dropPanel.Invalidate();
            var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;
            string? osz = files.FirstOrDefault(f =>
                f.EndsWith(".osz", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));
            if (osz != null) SetRateInput(osz);
        };
        var dropWrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 8), BackColor = Theme.Bg };
        dropWrap.Controls.Add(_dropPanel);
        panel.Controls.Add(dropWrap, 0, 1);

        _rateFileLabel = new Label
        {
            Text = "未选择文件",
            AutoSize = true,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(2, 2, 0, 6),
        };
        panel.Controls.Add(_rateFileLabel, 0, 2);

        var speedRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 6, BackColor = Theme.Bg, Margin = new Padding(0, 4, 0, 0) };
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        speedRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        speedRow.Controls.Add(new Label { Text = "倍速", AutoSize = true, ForeColor = Theme.TextDim, Anchor = AnchorStyles.Left, Margin = new Padding(2, 0, 0, 0) }, 0, 0);
        _rateSpeed = new NumericUpDown
        {
            Minimum = 0.50m,
            Maximum = 2.00m,
            DecimalPlaces = 2,
            Increment = 0.05m,
            Value = 1.25m,
            Width = 86,
            Dock = DockStyle.Fill,
        };
        Theme.StyleNumeric(_rateSpeed);
        speedRow.Controls.Add(_rateSpeed, 1, 0);
        speedRow.Controls.Add(new Label { Text = "（0.50 ~ 2.00，精确 0.01）", AutoSize = true, ForeColor = Theme.TextDim, Font = Theme.FontSmall, Anchor = AnchorStyles.Left, Margin = new Padding(8, 0, 0, 0) }, 2, 0);
        _chkRemoveBg = new CheckBox { Text = "移除曲绘", Checked = true, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 0, 0) };
        Theme.StyleCheck(_chkRemoveBg);
        speedRow.Controls.Add(_chkRemoveBg, 3, 0);
        _btnGenerate = MakeButton("生成变速谱面", RoundedButton.Style.Primary);
        _btnGenerate.Dock = DockStyle.Fill;
        _btnGenerate.Margin = new Padding(0, 0, 8, 0);
        speedRow.Controls.Add(_btnGenerate, 4, 0);
        _btnOpenFolder = MakeButton("打开文件夹", RoundedButton.Style.Ghost);
        _btnOpenFolder.Dock = DockStyle.Fill;
        _btnOpenFolder.Enabled = false;
        _btnOpenFolder.Margin = new Padding(0, 0, 0, 0);
        speedRow.Controls.Add(_btnOpenFolder, 5, 0);
        panel.Controls.Add(speedRow, 0, 3);

        _rateStatusLabel = new Label
        {
            Text = "",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(2, 10, 0, 0),
        };
        panel.Controls.Add(_rateStatusLabel, 0, 4);

        var help = new Label
        {
            Text = "变速会把音频与谱面节奏整体改变（0.50x ~ 2.00x），新文件以 (0.XX) 为前缀另存，不影响原谱面。\r\n" +
                   "生成的谱面被视为全新本地谱面：ID 清零、标题/难度名带后缀、清空 Source、Tags 随机字母、移除曲绘。\r\n" +
                   "音频变速需要 ffmpeg.exe（发布包已附带 / 或放到程序同目录）。",
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(2, 12, 0, 0),
        };
        panel.Controls.Add(help, 0, 5);

        _btnGenerate.Click += async (_, _) => await GenerateRateAsync();
        _btnOpenFolder.Click += (_, _) =>
        {
            if (_lastOutput.Length > 0 && File.Exists(_lastOutput))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_lastOutput}\"");
        };
        UpdateFfmpegHint();
        return page;
    }

    // ==================== 谱面下载页 ====================

    private Panel BuildDownloadPage()
    {
        var page = new Panel { Padding = new Padding(16, 14, 16, 14) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, BackColor = Theme.Bg };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.Controls.Add(panel);

        panel.Controls.Add(SectionTitle("谱面下载 · osu.sayobot.cn"), 0, 0);

        // 搜索行
        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 5, BackColor = Theme.Bg };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        searchRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _dlSearchBox = new TextBox { PlaceholderText = "关键词 + 官网语法，例如 blue zenith stars>6 mode=mania" };
        Theme.StyleText(_dlSearchBox);
        _dlSearchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchSayobotAsync(); }
        };
        _btnSearch = MakeButton("搜索", RoundedButton.Style.Primary);
        _btnSearch.Dock = DockStyle.Fill;
        _btnSearch.Margin = new Padding(0, 0, 8, 0);
        _btnSearch.Click += async (_, _) => await SearchSayobotAsync();
        searchRow.Controls.Add(new Label { Text = "搜索谱面", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.TextDim }, 0, 0);
        searchRow.Controls.Add(Wrap(_dlSearchBox), 1, 0);
        var searchBtnWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        searchBtnWrap.Controls.Add(_btnSearch);
        searchRow.Controls.Add(searchBtnWrap, 2, 0);
        _sortCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        Theme.StyleCombo(_sortCombo);
        _sortCombo.Items.AddRange(new object[]
        {
            "相关度", "标题", "艺术家", "难度(★)", "收藏数", "游玩数", "更新时间", "状态",
        });
        _sortCombo.SelectedIndex = 0;
        _sortCombo.SelectedIndexChanged += (_, _) => { if (_searchItems.Count > 0) FillSearchResults(); };
        _sortCombo.Margin = new Padding(10, 0, 6, 0);
        searchRow.Controls.Add(_sortCombo, 3, 0);
        _btnSortDir = MakeButton("↓ 降序", RoundedButton.Style.Ghost);
        _btnSortDir.Dock = DockStyle.Fill;
        _btnSortDir.Margin = new Padding(0);
        _btnSortDir.Click += (_, _) =>
        {
            _sortDesc = !_sortDesc;
            _btnSortDir.Text = _sortDesc ? "↓ 降序" : "↑ 升序";
            if (_searchItems.Count > 0) FillSearchResults();
        };
        var sortDirWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        sortDirWrap.Controls.Add(_btnSortDir);
        searchRow.Controls.Add(sortDirWrap, 4, 0);
        panel.Controls.Add(searchRow, 0, 1);

        // 搜索结果
        _searchList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Font = Theme.FontSmall,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            HideSelection = false,
            OwnerDraw = true,
        };
        // 用一个空白 ImageList 撑起行高，便于显示封面缩略图
        var rowHeight = new ImageList { ImageSize = new Size(48, 34), ColorDepth = ColorDepth.Depth32Bit };
        var blank = new Bitmap(48, 34);
        using (var g = Graphics.FromImage(blank)) g.Clear(Theme.Surface);
        rowHeight.Images.Add(blank);
        _searchList.SmallImageList = rowHeight;

        _searchList.DrawColumnHeader += (_, e) =>
        {
            using var bg = new SolidBrush(Theme.SurfaceAlt);
            e.Graphics.FillRectangle(bg, e.Bounds);
            var textRect = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", Theme.FontSmall, textRect, Theme.TextDim,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        _searchList.DrawSubItem += (_, e) =>
        {
            using var bg = new SolidBrush(e.Item.Selected ? Theme.SurfaceHi : (e.ItemIndex % 2 == 0 ? Theme.Surface : Theme.ColorFromHex("#1e222c")));
            e.Graphics.FillRectangle(bg, e.Bounds);

            if (e.ColumnIndex == 0)
            {
                if (e.Item.Tag is long sidL && _coverCache.TryGetValue(sidL, out var img) && img != null)
                {
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    int h = e.Bounds.Height - 4;
                    int w = (int)(h * 3.0);
                    if (w > e.Bounds.Width - 4) w = e.Bounds.Width - 4;
                    int x = e.Bounds.X + (e.Bounds.Width - w) / 2;
                    int y = e.Bounds.Y + 2;
                    e.Graphics.DrawImage(img, new Rectangle(x, y, w, h));
                }
                return;
            }

            var textRect = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", Theme.FontSmall, textRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        _searchList.DrawItem += (_, _) => { };
        _searchList.Columns.Add("封面", 50);
        _searchList.Columns.Add("谱面", 176);
        _searchList.Columns.Add("难度", 116);
        _searchList.Columns.Add("模式", 48);
        _searchList.Columns.Add("状态", 84);
        _searchList.Columns.Add("OD", 36);
        _searchList.Columns.Add("HP", 36);
        _searchList.Columns.Add("星级", 42);
        _searchList.SelectedIndexChanged += (_, _) =>
        {
            if (_searchList.SelectedItems.Count == 0) return;
            var item = _searchList.SelectedItems[0];
            _dlBox.Text = item.Tag?.ToString() ?? "";
            _dlStatus.Text = $"已选择：{item.SubItems[1].Text} [{item.SubItems[2].Text}]（点「下载 .osz」或双击直接下载）";
            if (item.Tag is long sid)
            {
                StopPreview();
                _coverSid = sid;
                _btnPreview.Enabled = true;
                ShowCover(sid);
            }
        };
        _searchList.Resize += (_, _) => ResizeSearchColumns();
        _searchList.DoubleClick += async (_, _) =>
        {
            if (_searchList.SelectedItems.Count == 0) return;
            _dlBox.Text = _searchList.SelectedItems[0].Tag?.ToString() ?? "";
            await DownloadSayobotAsync();
        };

        var resultsRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        resultsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        resultsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        resultsRow.Controls.Add(_searchList, 0, 0);

        var coverPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(10, 0, 0, 0),
        };
        coverPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        coverPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        coverPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        _coverBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Theme.ColorFromHex("#151923"),
            Margin = new Padding(0),
        };
        _coverInfo = new Label
        {
            Text = "搜索后显示封面",
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Padding = new Padding(0, 4, 0, 0),
            Margin = new Padding(0),
        };
        _btnPreview = MakeButton("▶", RoundedButton.Style.Secondary);
        _btnPreview.Dock = DockStyle.Fill;
        _btnPreview.Font = new Font("Segoe UI Symbol", 11F, FontStyle.Bold);
        _btnPreview.Margin = new Padding(0, 4, 0, 4);
        _btnPreview.Enabled = false;
        _previewTip = new ToolTip();
        _previewTip.SetToolTip(_btnPreview, "播放 / 暂停");
        _btnPreview.Click += async (_, _) => await TogglePreviewAsync();

        coverPanel.Controls.Add(_coverBox, 0, 0);
        coverPanel.Controls.Add(_btnPreview, 0, 1);
        coverPanel.Controls.Add(_coverInfo, 0, 2);
        resultsRow.Controls.Add(coverPanel, 1, 0);

        var listCard = Theme.BorderPanel(10, Theme.Border, Theme.Surface, 10);
        listCard.Dock = DockStyle.Fill;
        listCard.Margin = new Padding(0, 8, 0, 0);
        listCard.MinimumSize = new Size(0, 180);
        listCard.Controls.Add(resultsRow);
        panel.Controls.Add(listCard, 0, 2);

        // 下载卡片
        var card = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 14);
        card.Dock = DockStyle.Fill;
        card.AutoSize = true;
        card.Margin = new Padding(0, 10, 0, 0);
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, BackColor = Color.Transparent };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _dlBox = new TextBox { PlaceholderText = "谱面集 ID（如 2606612）或 sayobot 链接" };
        Theme.StyleText(_dlBox);
        var btnWeb = MakeButton("打开网页", RoundedButton.Style.Ghost);
        btnWeb.Click += (_, _) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://osu.sayobot.cn/") { UseShellExecute = true });
        rows.Controls.Add(new Label { Text = "谱面集 ID / 链接", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.TextDim }, 0, 0);
        rows.Controls.Add(Wrap(_dlBox), 1, 0);
        var webWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        webWrap.Controls.Add(btnWeb);
        rows.Controls.Add(webWrap, 2, 0);

        _dlDirBox = new TextBox { Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") };
        Theme.StyleText(_dlDirBox);
        var btnDir = MakeButton("浏览…", RoundedButton.Style.Ghost);
        btnDir.Click += (_, _) => PickFolder(_dlDirBox, "选择 .osz 保存目录");
        var dirWrap = Wrap(_dlDirBox);
        dirWrap.Margin = new Padding(0, 10, 0, 0);
        rows.Controls.Add(new Label { Text = "保存目录", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Theme.TextDim, Margin = new Padding(0, 10, 0, 0) }, 0, 1);
        rows.Controls.Add(dirWrap, 1, 1);
        var dirBtnWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = new Padding(0, 10, 0, 0) };
        dirBtnWrap.Controls.Add(btnDir);
        rows.Controls.Add(dirBtnWrap, 2, 1);
        card.Controls.Add(rows);
        panel.Controls.Add(card, 0, 3);

        var btnRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, BackColor = Theme.Bg, Margin = new Padding(0, 12, 0, 0) };
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _btnDl = MakeButton("下载 .osz", RoundedButton.Style.Primary);
        _btnDlOpenFolder = MakeButton("打开所在文件夹", RoundedButton.Style.Ghost);
        _btnDlOpenFolder.Enabled = false;
        _btnDl.Dock = DockStyle.Fill;
        _btnDl.Margin = new Padding(0, 0, 8, 0);
        _btnDlOpenFolder.Dock = DockStyle.Fill;
        _btnDlOpenFolder.Margin = new Padding(0, 0, 12, 0);
        _dlStatus = new Label
        {
            Text = "来源：osu.sayobot.cn（小夜镜像站）",
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
        };
        btnRow.Controls.Add(_btnDl, 0, 0);
        btnRow.Controls.Add(_btnDlOpenFolder, 1, 0);
        btnRow.Controls.Add(_dlStatus, 2, 0);
        panel.Controls.Add(btnRow, 0, 4);

        var help = new Label
        {
            Text = "搜索支持 osu! 官网语法：stars>6、od<8、ar>9、bpm>180、length<180、mode=mania、status=ranked、creator=名字。\r\n" +
                   "结果含封面、难度、模式、状态、OD / HP / 星级；单击选择、双击下载，也可直接输入谱面集 ID 或链接下载。",
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(2, 12, 0, 0),
        };
        panel.Controls.Add(help, 0, 5);

        _btnDl.Click += async (_, _) => await DownloadSayobotAsync();
        _btnDlOpenFolder.Click += (_, _) =>
        {
            if (_dlLastFile.Length > 0 && File.Exists(_dlLastFile))
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_dlLastFile}\"");
        };
        return page;
    }

    private async Task SearchSayobotAsync()
    {
        if (_cts != null) return;
        string query = _dlSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            MessageBox.Show(this, "请输入要搜索的歌曲名 / 艺术家 / 谱师。\n" + SearchQuery.ExampleText,
                "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (keyword, filters) = SearchQuery.Parse(query);
        if (keyword.Length == 0)
        {
            MessageBox.Show(this, "请至少输入一个关键词（过滤条件不能单独使用）。\n" + SearchQuery.ExampleText,
                "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _cts = new CancellationTokenSource();
        _btnSearch.Enabled = false;
        _progress.IsMarquee = true;
        _dlStatus.Text = $"搜索「{keyword}」中…";
        AppendLog($"[搜索] {query}");

        var token = _cts.Token;
        try
        {
            var items = await SayobotClient.SearchWithDifficultiesAsync(keyword, 20, token,
                (done, total) => BeginInvoke(() => _dlStatus.Text = $"搜索「{keyword}」… 已读取 {done}/{total} 个谱面集"));
            _searchItems = items;
            _searchFilters = filters;
            FillSearchResults();
            int shown = CountFiltered();
            string filterText = filters.Count > 0 ? $"（筛选 {SearchQuery.Describe(filters)}）" : "";
            _dlStatus.Text = $"找到 {shown} 个难度{filterText}（关键词 {keyword}）";
            AppendLog($"[搜索] {query} → 命中 {shown}/{items.Count}");
        }
        catch (OperationCanceledException)
        {
            _dlStatus.Text = "搜索已取消";
        }
        catch (Exception e)
        {
            _dlStatus.Text = "搜索失败";
            AppendLog("[搜索错误] " + e.Message);
            MessageBox.Show(this, "搜索失败：" + e.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _progress.IsMarquee = false;
            _progress.Value = 0;
            _btnSearch.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private static readonly double[] SearchColumnWeights = { 0.085, 0.325, 0.215, 0.075, 0.135, 0.055, 0.055, 0.055 };

    /// <summary>按当前列表宽度重新分配各列宽度（窗口缩放时自适应）。</summary>
    private void ResizeSearchColumns()
    {
        if (_searchList == null || _searchList.Columns.Count == 0) return;
        int avail = _searchList.ClientSize.Width - 4;
        if (avail < 240) return;
        if (_searchList.Items.Count > 0) avail -= 20; // 预留垂直滚动条宽度

        int count = _searchList.Columns.Count;
        int remaining = avail;
        for (int i = 0; i < count; i++)
        {
            int w = i == count - 1
                ? remaining
                : Math.Max(40, (int)(avail * SearchColumnWeights[i]));
            w = Math.Max(40, w);
            _searchList.Columns[i].Width = w;
            remaining -= w;
        }

        // 兜底：若总宽仍超出（滚动条出现/DPI 差异），等比压缩，避免出现横向滚动条
        int total = 0;
        foreach (ColumnHeader col in _searchList.Columns) total += col.Width;
        int limit = _searchList.ClientSize.Width - 4;
        if (total > limit && limit > 100)
        {
            double k = (double)limit / total;
            foreach (ColumnHeader col in _searchList.Columns)
                col.Width = Math.Max(30, (int)(col.Width * k));
        }
    }

    private IEnumerable<(SayobotSearchSet Set, SayobotDifficulty Diff)> FilteredItems =>
        _searchItems.Where(x => _searchFilters.Count == 0 || SearchQuery.Matches(x.Set, x.Diff, _searchFilters));

    private int CountFiltered() => FilteredItems.Count();

    private void FillSearchResults()
    {
        var source = FilteredItems.ToList();
        // 相关度：保持接口返回顺序；其余按所选字段排序（方向可切换）
        IList<(SayobotSearchSet Set, SayobotDifficulty Diff)> ordered;
        if (_sortCombo.SelectedIndex == 0)
        {
            ordered = source;
        }
        else
        {
            IEnumerable<(SayobotSearchSet Set, SayobotDifficulty Diff)> query = _sortCombo.SelectedIndex switch
            {
                1 => source.OrderBy(x => x.Set.Title, StringComparer.OrdinalIgnoreCase),
                2 => source.OrderBy(x => x.Set.Artist, StringComparer.OrdinalIgnoreCase),
                3 => source.OrderBy(x => x.Diff.Star),
                4 => source.OrderBy(x => x.Set.FavouriteCount),
                5 => source.OrderBy(x => x.Set.PlayCount),
                6 => source.OrderBy(x => x.Set.LastUpdate),
                7 => source.OrderBy(x => x.Set.Approved),
                _ => source.OrderBy(x => x.Set.Order),
            };
            if (!_sortDesc) query = query.Reverse();
            ordered = query.GroupBy(x => x.Set.Sid)
                .SelectMany(g => g.OrderByDescending(x => x.Diff.Star))
                .ToList();
        }

        _searchList.BeginUpdate();
        _searchList.Items.Clear();
        foreach (var (set, diff) in ordered)
        {
            var lvi = new ListViewItem(string.Empty) { Tag = set.Sid, ImageIndex = 0 };
            lvi.SubItems.Add($"{set.Artist} - {set.Title}");
            lvi.SubItems.Add(diff.Version);
            lvi.SubItems.Add(SayobotClient.ModeText(diff.Mode));
            lvi.SubItems.Add(SayobotClient.StatusText(set.Approved));
            lvi.SubItems.Add(diff.OD.ToString("0.##"));
            lvi.SubItems.Add(diff.HP.ToString("0.##"));
            lvi.SubItems.Add(diff.Star.ToString("0.##"));
            lvi.SubItems.Add(set.Sid.ToString());
            _searchList.Items.Add(lvi);
        }
        _searchList.EndUpdate();

        foreach (long sid in ordered.Select(x => x.Set.Sid).Distinct())
            RequestCover(sid);

        ResizeSearchColumns();
        _searchList.Invalidate();
    }

    private void ShowCover(long sid)
    {
        RequestCover(sid);
        if (_coverCache.TryGetValue(sid, out var img) && img != null)
        {
            _coverBox.Image = img;
        }
        else
        {
            _coverBox.Image = null;
        }

        var diff = _searchItems.FirstOrDefault(x => x.Set.Sid == sid);
        if (diff.Set != null)
        {
            var set = diff.Set;
            string plays = set.PlayCount >= 10000 ? $"{set.PlayCount / 10000.0:0.#}万" : set.PlayCount.ToString();
            _coverInfo.Text = $"{set.Artist} - {set.Title}\r\n谱师 {set.Creator}\r\n{SayobotClient.StatusText(set.Approved)}\r\n♥ {set.FavouriteCount}  ·  ▶ {plays}";
        }
    }

    private void RequestCover(long sid)
    {
        if (!_coverRequested.Add(sid)) return;
        _ = Task.Run(async () =>
        {
            var img = await SayobotClient.GetCoverAsync(sid, CancellationToken.None);
            if (img == null) return;
            try
            {
                BeginInvoke(() =>
                {
                    _coverCache[sid] = img;
                    _searchList.Invalidate();
                    if (_coverSid == sid) _coverBox.Image = img;
                });
            }
            catch
            {
                // 窗口已关闭
            }
        });
    }

    // ---------- 试听 ----------

    private void StopPreview()
    {
        try { _audioOut?.Stop(); } catch { }
        try { _audioOut?.Dispose(); } catch { }
        _audioOut = null;
        try { _vorbisReader?.Dispose(); } catch { }
        _vorbisReader = null;
        _previewPaused = false;
        if (_btnPreview != null) _btnPreview.Text = "▶";
    }

    private bool IsPreviewing => _audioOut != null;

    private async Task TogglePreviewAsync()
    {
        // 已有播放器：在「播放 ⇄ 暂停」之间切换
        if (IsPreviewing)
        {
            try
            {
                if (_previewPaused)
                {
                    _audioOut!.Play();
                    _previewPaused = false;
                    _dlStatus.Text = "播放中…";
                }
                else
                {
                    _audioOut!.Pause();
                    _previewPaused = true;
                    _dlStatus.Text = "已暂停";
                }
            }
            catch (Exception e)
            {
                AppendLog("[试听错误] " + e.Message);
            }
            _btnPreview.Text = _previewPaused ? "▶" : "⏸";
            return;
        }

        long sid = _coverSid > 0 ? _coverSid : (SayobotClient.TryParseSid(_dlBox.Text, out long parsed) ? parsed : -1);
        if (sid <= 0)
        {
            MessageBox.Show(this, "请先在搜索结果中选择一个谱面。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _btnPreview.Enabled = false;
            _dlStatus.Text = "加载音频…";
            string path = await SayobotClient.DownloadPreviewAsync(sid, CancellationToken.None);

            _vorbisReader = new NAudio.Vorbis.VorbisWaveReader(path);
            _audioOut = new NAudio.Wave.WaveOutEvent();
            _audioOut.PlaybackStopped += (_, _) =>
            {
                try
                {
                    BeginInvoke(() =>
                    {
                        StopPreview();
                        _dlStatus.Text = "播放结束";
                    });
                }
                catch
                {
                    // 窗口已关闭
                }
            };
            _audioOut.Init(_vorbisReader);
            _audioOut.Play();
            _previewPaused = false;
            _btnPreview.Text = "⏸";
            _dlStatus.Text = "播放中…";
        }
        catch (Exception e)
        {
            StopPreview();
            _dlStatus.Text = "播放失败";
            AppendLog("[试听错误] " + e.Message);
            MessageBox.Show(this, "播放失败：" + e.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnPreview.Enabled = _coverSid > 0;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopPreview();
        base.OnFormClosing(e);
    }

    // ==================== 段位计算页 ====================

    private Panel BuildDanPage()
    {
        var page = new Panel { Padding = new Padding(16, 14, 16, 14) };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, BackColor = Theme.Bg };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(panel);

        panel.Controls.Add(SectionTitle("段位单曲 ACC 计算 · 4K / 6K / 7K"), 0, 0);

        var selCard = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 12);
        selCard.Dock = DockStyle.Fill;
        selCard.AutoSize = true;
        var selRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.Transparent };
        _danMode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 76 };
        _danCourse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 264 };
        _danLevel = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 168 };
        Theme.StyleCombo(_danMode);
        Theme.StyleCombo(_danCourse);
        Theme.StyleCombo(_danLevel);
        selRow.Controls.Add(new Label { Text = "键位", AutoSize = true, ForeColor = Theme.TextDim, Margin = new Padding(0, 8, 6, 0) });
        selRow.Controls.Add(_danMode);
        selRow.Controls.Add(new Label { Text = "课程", AutoSize = true, ForeColor = Theme.TextDim, Margin = new Padding(14, 8, 6, 0) });
        selRow.Controls.Add(_danCourse);
        selRow.Controls.Add(new Label { Text = "等级", AutoSize = true, ForeColor = Theme.TextDim, Margin = new Padding(14, 8, 6, 0) });
        selRow.Controls.Add(_danLevel);
        selCard.Controls.Add(selRow);
        panel.Controls.Add(selCard, 0, 1);

        // 歌曲表
        var tableCard = Theme.BorderPanel(12, Theme.Border, Theme.Surface, 10);
        tableCard.Dock = DockStyle.Fill;
        tableCard.AutoSize = true;
        tableCard.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, BackColor = Color.Transparent };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        for (int i = 0; i < 4; i++) table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        string[] heads = { "曲名", "通過時ACC", "個別ACC", "精确值" };
        for (int c = 0; c < heads.Length; c++)
        {
            table.Controls.Add(new Label
            {
                Text = heads[c],
                AutoSize = true,
                Font = Theme.FontBold,
                ForeColor = Theme.TextDim,
                Margin = new Padding(2, 2, 2, 8),
            }, c, 0);
        }

        for (int i = 0; i < 4; i++)
        {
            _danSong[i] = new TextBox { ReadOnly = true, BorderStyle = BorderStyle.None };
            _danSong[i].BackColor = Theme.ColorFromHex("#151923");
            _danSong[i].ForeColor = Theme.ColorFromHex("#cdd6e6");
            _danSong[i].Font = Theme.FontBase;
            var songWrap = Wrap(_danSong[i], 6, 8, 4);
            songWrap.Margin = new Padding(0, 0, 6, 6);

            _danPass[i] = new NumericUpDown { Minimum = 0, Maximum = 100, DecimalPlaces = 2, Increment = 0.01m, Width = 104 };
            Theme.StyleNumeric(_danPass[i]);
            var passWrap = Wrap(_danPass[i], 6, 4, 3);
            passWrap.Margin = new Padding(0, 0, 6, 6);

            _danInd[i] = new Label
            {
                Text = "-",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Theme.Accent,
                Font = Theme.FontBold,
                Margin = new Padding(4, 7, 0, 6),
            };
            _danIndExact[i] = new Label
            {
                Text = "",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Theme.TextDim,
                Font = Theme.FontSmall,
                Margin = new Padding(4, 9, 0, 6),
            };

            table.Controls.Add(songWrap, 0, i + 1);
            table.Controls.Add(passWrap, 1, i + 1);
            table.Controls.Add(_danInd[i], 2, i + 1);
            table.Controls.Add(_danIndExact[i], 3, i + 1);
            int idx = i;
            _danPass[idx].ValueChanged += (_, _) => RecomputeDan();
        }
        tableCard.Controls.Add(table);
        panel.Controls.Add(tableCard, 0, 2);

        var help = new Label
        {
            Text = "数据来源：4 6 7k段位 個別ACC計算 by gyoza_goki & Blaite1016（8 个课程、127 个等级）。\r\n" +
                   "填入各曲目结束时的通過時ACC（累计要求），自动计算每首单曲所需的個別ACC。",
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Theme.TextDim,
            Font = Theme.FontSmall,
            Margin = new Padding(2, 12, 0, 0),
        };
        panel.Controls.Add(help, 0, 3);

        _danMode.SelectedIndexChanged += (_, _) => OnDanModeChanged();
        _danCourse.SelectedIndexChanged += (_, _) => OnDanCourseChanged();
        _danLevel.SelectedIndexChanged += (_, _) => OnDanLevelChanged();
        InitDan();
        return page;
    }

    // ==================== 下载逻辑 ====================

    private async Task DownloadSayobotAsync()
    {
        if (_cts != null) return;
        if (!SayobotClient.TryParseSid(_dlBox.Text, out long sid))
        {
            MessageBox.Show(this, "请输入有效的谱面集 ID 或 sayobot 链接。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string dir = _dlDirBox.Text.Trim();
        if (dir.Length == 0) dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        try { Directory.CreateDirectory(dir); }
        catch (Exception e)
        {
            MessageBox.Show(this, "无法创建保存目录: " + e.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _cts = new CancellationTokenSource();
        _btnDl.Enabled = false;
        _btnDlOpenFolder.Enabled = false;
        _dlStatus.Text = "查询谱面信息…";
        _progress.IsMarquee = true;
        AppendLog($"[下载] 查询谱面集 {sid} 的信息…");

        var token = _cts.Token;
        await Task.Run(async () =>
        {
            try
            {
                var info = await SayobotClient.GetMapInfoAsync(sid, token);
                string fname = TransferEngine.SanitizeName($"{info.Sid} {info.Artist} - {info.Title}.osz");
                string path = Path.Combine(dir, fname);
                BeginInvoke(() =>
                {
                    AppendLog($"[下载] {info.Artist} - {info.Title} → {path}");
                    _dlStatus.Text = "下载中…";
                    _progress.IsMarquee = false;
                });
                await SayobotClient.DownloadAsync(sid, path, (done, total) => BeginInvoke(() =>
                {
                    if (total > 0)
                    {
                        _progress.Maximum = (int)Math.Max(1, total);
                        _progress.Value = (int)Math.Min(done, _progress.Maximum);
                    }
                }), token);
                _dlLastFile = path;
                BeginInvoke(() => OnDownloadDone(path));
            }
            catch (Exception e)
            {
                BeginInvoke(() =>
                {
                    _dlStatus.Text = "下载失败";
                    AppendLog("[下载错误] " + e.Message);
                    _progress.IsMarquee = false;
                    _progress.Value = 0;
                    _btnDl.Enabled = true;
                    _cts?.Dispose();
                    _cts = null;
                });
            }
        });
    }

    private void OnDownloadDone(string path)
    {
        AppendLog($"✅ 下载完成: {path}");
        _dlStatus.Text = "下载完成";
        _progress.Value = 0;
        _btnDl.Enabled = true;
        _btnDlOpenFolder.Enabled = true;
        _cts?.Dispose();
        _cts = null;

        var answer = MessageBox.Show(this,
            "谱面下载完成！\r\n\r\n是否需要进行倍速修改？", "下载完成",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer == DialogResult.Yes)
        {
            SetRateInput(path);
            SelectPage(1);
        }
    }

    // ==================== 段位计算逻辑 ====================

    private void InitDan()
    {
        _danMode.Items.AddRange(DanCoursesData.Modes.Keys.OrderBy(k => k switch { "4k" => 0, "6k" => 1, _ => 2 }).ToArray());
        _danMode.SelectedIndex = 0;
    }

    private void OnDanModeChanged()
    {
        _danLoading = true;
        _danCourse.Items.Clear();
        _danLevel.Items.Clear();
        if (_danMode.SelectedItem is string mode && DanCoursesData.Modes.TryGetValue(mode, out var courses))
        {
            foreach (var c in courses) _danCourse.Items.Add(c.Name);
            _danCourse.SelectedIndex = 0;
        }
        _danLoading = false;
        OnDanCourseChanged();
    }

    private void OnDanCourseChanged()
    {
        _danLoading = true;
        _danLevel.Items.Clear();
        DanCourse? course = GetSelectedDanCourse();
        if (course != null)
        {
            foreach (var lv in course.Levels) _danLevel.Items.Add(lv.Name);
            _danLevel.SelectedIndex = 0;
        }
        _danLoading = false;
        OnDanLevelChanged();
    }

    private DanCourse? GetSelectedDanCourse()
    {
        if (_danMode.SelectedItem is not string mode) return null;
        if (!DanCoursesData.Modes.TryGetValue(mode, out var courses)) return null;
        if (_danCourse.SelectedIndex < 0 || _danCourse.SelectedIndex >= courses.Count) return null;
        return courses[_danCourse.SelectedIndex];
    }

    private DanLevel? GetSelectedDanLevel()
    {
        var course = GetSelectedDanCourse();
        if (course == null || _danLevel.SelectedIndex < 0 || _danLevel.SelectedIndex >= course.Levels.Count) return null;
        return course.Levels[_danLevel.SelectedIndex];
    }

    private void OnDanLevelChanged()
    {
        var lv = GetSelectedDanLevel();
        _danLoading = true;
        for (int i = 0; i < 4; i++)
        {
            _danSong[i].Text = lv != null && i < lv.Songs.Length ? lv.Songs[i] : "";
            _danPass[i].Value = lv != null && i < lv.Acc.Length && lv.Acc[i].HasValue ? (decimal)lv.Acc[i]!.Value : 0;
        }
        _danLoading = false;
        RecomputeDan();
    }

    private void RecomputeDan()
    {
        if (_danLoading) return;
        var lv = GetSelectedDanLevel();
        if (lv == null) return;

        var x = new double[4];
        for (int i = 0; i < 4; i++) x[i] = (double)_danPass[i].Value;

        // 個別ACC：ind[0]=X0; ind[i] = (T[i]*X[i] - T[i-1]*X[i-1]) / (T[i]-T[i-1])
        bool allZero = x.All(v => v <= 0);
        for (int i = 0; i < 4; i++)
        {
            if (allZero)
            {
                _danInd[i].Text = "-";
                _danIndExact[i].Text = "";
                continue;
            }
            double ind = i == 0 ? x[0] : (lv.Cum[i] * x[i] - lv.Cum[i - 1] * x[i - 1]) / (lv.Cum[i] - lv.Cum[i - 1]);
            _danInd[i].Text = ind.ToString("0.00");
            _danIndExact[i].Text = ind.ToString("0.##########");
        }
    }

    // ==================== 倍速逻辑 ====================

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
        _rateFileLabel.ForeColor = Theme.Text;
        _rateStatusLabel.Text = "";
        AppendLog($"[倍速] 已选择 {path}");
    }

    private void UpdateFfmpegHint()
    {
        string ff = RateChanger.FindFfmpeg();
        _rateStatusLabel.Text = ff.Length > 0
            ? "✓ ffmpeg 已就绪（音频变速可用）"
            : "⚠ 未找到 ffmpeg.exe —— 生成变速谱面需要它（放到程序同目录或加入 PATH）";
        _rateStatusLabel.ForeColor = ff.Length > 0 ? Theme.TextDim : Theme.Danger;
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
                "未找到 ffmpeg.exe，无法对音频变速。\r\n生成的谱面节奏会改变，但音乐仍是原速（不同步）。\r\n\r\n仍要生成吗？",
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
        _progress.IsMarquee = true;
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
                    _progress.IsMarquee = false;
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
                    _progress.IsMarquee = false;
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
            _statSets.Text = result.l.Sets.Count.ToString();
            _statCols.Text = result.l.Collections.Count.ToString();
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
                    _statMissing.Text = sum.MissingSets.ToString();
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
        _progress.IsMarquee = false;
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
        if (_statusStrip != null)
            _statusStrip.Text = line;
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






















