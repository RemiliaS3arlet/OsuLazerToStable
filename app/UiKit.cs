using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace OsuLazerToStable;

/// <summary>深色主题配色与字体。</summary>
public static class Theme
{
    public static readonly Color Bg = ColorFromHex("#14161c");
    public static readonly Color Surface = ColorFromHex("#1b1e26");
    public static readonly Color SurfaceAlt = ColorFromHex("#22262f");
    public static readonly Color SurfaceHi = ColorFromHex("#2b3140");
    public static readonly Color Border = ColorFromHex("#2f3542");
    public static readonly Color Text = ColorFromHex("#e6e9f0");
    public static readonly Color TextDim = ColorFromHex("#8f97a8");
    public static readonly Color Accent = ColorFromHex("#ff66aa");
    public static readonly Color Accent2 = ColorFromHex("#3ee0ff");
    public static readonly Color Success = ColorFromHex("#4ade80");
    public static readonly Color Danger = ColorFromHex("#f87171");

    public static readonly Font FontBase = new("Microsoft YaHei UI", 9F);
    public static readonly Font FontBold = new("Microsoft YaHei UI", 9F, FontStyle.Bold);
    public static readonly Font FontSmall = new("Microsoft YaHei UI", 8.25F);
    public static readonly Font FontTitle = new("Microsoft YaHei UI", 20F, FontStyle.Bold);
    public static readonly Font FontSub = new("Microsoft YaHei UI", 9.5F);
    public static readonly Font FontSection = new("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontMono = new("Consolas", 9F);

    public static Color ColorFromHex(string hex) => ColorTranslator.FromHtml(hex);

    public static GraphicsPath Round(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = Math.Max(2, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void StyleText(TextBox tb)
    {
        tb.BorderStyle = BorderStyle.None;
        tb.BackColor = SurfaceAlt;
        tb.ForeColor = Text;
        tb.Font = FontBase;
        tb.Margin = new Padding(0);
    }

    public static void StyleCombo(ComboBox cb)
    {
        cb.FlatStyle = FlatStyle.Flat;
        cb.BackColor = SurfaceAlt;
        cb.ForeColor = Text;
        cb.Font = FontBase;
        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.ItemHeight = 22;
        cb.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using var bg = new SolidBrush(selected ? ColorFromHex("#2e3442") : SurfaceAlt);
            e.Graphics.FillRectangle(bg, e.Bounds);
            var itemText = cb.Items[e.Index]?.ToString() ?? "";
            TextRenderer.DrawText(e.Graphics, itemText, FontBase, e.Bounds, Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    public static void StyleNumeric(NumericUpDown nud)
    {
        nud.BorderStyle = BorderStyle.None;
        nud.BackColor = SurfaceAlt;
        nud.ForeColor = Text;
        nud.Font = FontBase;
    }

    public static void StyleCheck(CheckBox cb)
    {
        cb.ForeColor = Text;
        cb.Font = FontBase;
        cb.FlatStyle = FlatStyle.Standard;
    }

    /// <summary>带 1px 边框的圆角容器。</summary>
    public static Panel BorderPanel(int radius = 10, Color? border = null, Color? fill = null, int pad = 10)
    {
        return new BorderPanel
        {
            Radius = radius,
            BorderColor = border ?? Border,
            FillColor = fill ?? Surface,
            Padding = new Padding(pad),
            Margin = new Padding(0),
        };
    }
}

public class BorderPanel : Panel
{
    public int Radius = 10;
    public Color BorderColor = Theme.Border;
    public Color FillColor = Theme.Surface;

    public BorderPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Theme.Round(rect, Radius);
        using var fill = new SolidBrush(FillColor);
        g.FillPath(fill, path);
        using var pen = new Pen(BorderColor, 1f);
        g.DrawPath(pen, path);
    }
}

/// <summary>圆角按钮（三种风格：主按钮强调色 / 次按钮 / 幽灵）。</summary>
public class RoundedButton : Button
{
    public enum Style { Primary, Secondary, Ghost }

    public Style Variant = Style.Secondary;
    public int Radius = 8;
    private bool _hover, _down;

    public RoundedButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.FontBase;
        Cursor = Cursors.Hand;
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Margin = new Padding(0, 0, 8, 0);
        Height = 32;
        Padding = new Padding(14, 0, 14, 0);
        AutoSize = false;
        AutoSizeMode = AutoSizeMode.GrowOnly;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        Color fill, border, text;
        if (!Enabled)
        {
            fill = Theme.Surface;
            border = Theme.Border;
            text = Theme.TextDim;
        }
        else if (Variant == Style.Primary)
        {
            fill = _down ? Theme.ColorFromHex("#e05497") : _hover ? Theme.ColorFromHex("#ff85b8") : Theme.Accent;
            border = fill;
            text = Theme.ColorFromHex("#24101a");
        }
        else if (Variant == Style.Ghost)
        {
            fill = _down ? Theme.SurfaceHi : _hover ? Theme.SurfaceAlt : Theme.Surface;
            border = Color.Transparent;
            text = Theme.Text;
        }
        else
        {
            fill = _down ? Theme.SurfaceHi : _hover ? Theme.ColorFromHex("#2a3040") : Theme.SurfaceAlt;
            border = _hover ? Theme.ColorFromHex("#454f63") : Theme.Border;
            text = Theme.Text;
        }

        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = Theme.Round(rect, Radius);
        using var fillBrush = new SolidBrush(fill);
        g.FillPath(fillBrush, path);
        if (border.A > 0 && Variant != Style.Primary)
        {
            using var pen = new Pen(border, 1f);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, rect, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}

/// <summary>圆角进度条（支持进度与跑马灯两种模式）。</summary>
public class FlatProgress : Control
{
    private int _value;
    private readonly System.Windows.Forms.Timer _timer;
    private float _phase;

    public int Maximum { get; set; } = 100;
    public int Value
    {
        get => _value;
        set { _value = Math.Max(0, Math.Min(Maximum, value)); Invalidate(); }
    }

    public bool IsMarquee
    {
        get => _timer.Enabled;
        set
        {
            _timer.Enabled = value;
            if (value) _phase = 0;
            Invalidate();
        }
    }

    public FlatProgress()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Height = 8;
        BackColor = Theme.Bg;
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => { _phase += 0.018f; if (_phase > 1.4f) _phase = -0.4f; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        var rect = new Rectangle(0, (Height - 6) / 2, Width - 1, 6);
        using var track = Theme.Round(rect, 3);
        using var trackBrush = new SolidBrush(Theme.SurfaceAlt);
        g.FillPath(trackBrush, track);

        Rectangle fillRect;
        if (IsMarquee)
        {
            int span = Math.Max(60, Width / 4);
            int x = (int)((_phase) * (Width + span)) - span;
            fillRect = new Rectangle(Math.Max(0, x), rect.Y, Math.Min(span, Width - Math.Max(0, x)), rect.Height);
        }
        else
        {
            float ratio = Maximum <= 0 ? 0 : (float)Value / Maximum;
            fillRect = new Rectangle(0, rect.Y, Math.Max(0, (int)((Width - 1) * ratio)), rect.Height);
        }

        if (fillRect.Width > 2)
        {
            using var fillPath = Theme.Round(fillRect, 3);
            using var grad = new LinearGradientBrush(new Rectangle(0, rect.Y, Math.Max(2, Width), rect.Height),
                Theme.Accent, Theme.Accent2, LinearGradientMode.Horizontal);
            g.FillPath(grad, fillPath);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>左侧导航按钮。</summary>
public class NavButton : Control
{
    public bool Selected { get; set; }
    public string Caption = "";
    public string Hint = "";
    private bool _hover;

    public NavButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Width = 168;
        Height = 46;
        Cursor = Cursors.Hand;
        BackColor = Theme.Bg;
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); }
    public event EventHandler? Activated;
    protected override void OnClick(EventArgs e) { Activated?.Invoke(this, e); Invalidate(); base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Bg);

        var rect = new Rectangle(6, 4, Width - 12, Height - 8);
        if (Selected)
        {
            using var path = Theme.Round(rect, 8);
            using var brush = new SolidBrush(Theme.SurfaceAlt);
            g.FillPath(brush, path);
            using var bar = new SolidBrush(Theme.Accent);
            g.FillRectangle(bar, rect.X + 1, rect.Y + 8, 3, rect.Height - 16);
        }
        else if (_hover)
        {
            using var path = Theme.Round(rect, 8);
            using var brush = new SolidBrush(Theme.Surface);
            g.FillPath(brush, path);
        }

        var textColor = Selected ? Theme.Text : _hover ? Theme.Text : Theme.TextDim;
        var font = Selected ? Theme.FontBold : Theme.FontBase;
        var textRect = new Rectangle(rect.X + 16, rect.Y, rect.Width - 24, rect.Height);
        TextRenderer.DrawText(g, Caption, font, textRect, textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>顶部横幅：绘制皮肤图 + 标题/副标题。</summary>
public class BannerPanel : Panel
{
    public Image? BannerImage;
    public string Title = "";
    public string Subtitle = "";

    public BannerPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Height = 112;
        Dock = DockStyle.Fill;
        BackColor = Theme.ColorFromHex("#15161d");
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        if (BannerImage != null)
        {
            // 拉伸铺满，保持比例覆盖
            float scale = Math.Max((float)Width / BannerImage.Width, (float)Height / BannerImage.Height);
            int w = (int)(BannerImage.Width * scale);
            int h = (int)(BannerImage.Height * scale);
            g.DrawImage(BannerImage, new Rectangle((Width - w) / 2, (Height - h) / 2, w, h));
        }
        else
        {
            using var bg = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(2, Width), Math.Max(2, Height)),
                Theme.ColorFromHex("#15161d"), Theme.ColorFromHex("#1e1a2b"), LinearGradientMode.Vertical);
            g.FillRectangle(bg, ClientRectangle);
        }

        // 左侧压暗，保证文字可读
        using var shade = new LinearGradientBrush(new Rectangle(0, 0, 520, Math.Max(2, Height)),
            Color.FromArgb(210, 10, 11, 16), Color.FromArgb(0, 10, 11, 16), LinearGradientMode.Horizontal);
        g.FillRectangle(shade, new Rectangle(0, 0, 520, Height));

        var titleRect = new Rectangle(28, 26, Math.Max(10, Width - 60), 44);
        using var glow = new SolidBrush(Color.FromArgb(60, 255, 102, 170));
        var titleSize = TextRenderer.MeasureText(Title, Theme.FontTitle);
        g.FillEllipse(glow, 22, 34, 10, 10);
        TextRenderer.DrawText(g, Title, Theme.FontTitle, new Rectangle(40, 20, Math.Max(10, Width - 70), 48),
            Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        TextRenderer.DrawText(g, Subtitle, Theme.FontSub, new Rectangle(40, Height - 44, Math.Max(10, Width - 70), 26),
            Color.FromArgb(225, 190, 195, 210), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }
}

/// <summary>拖放区域：虚线圆角框，拖入时高亮。</summary>
public class DropZone : Panel
{
    public bool DragOver { get; set; }
    public string Caption = "";

    public DropZone()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        AllowDrop = true;
        Cursor = Cursors.Hand;
        BackColor = Theme.Bg;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Bg);
        var rect = new Rectangle(2, 2, Width - 5, Height - 5);
        using var path = Theme.Round(rect, 12);

        using var fill = new SolidBrush(DragOver ? Theme.ColorFromHex("#2a1c26") : Theme.Surface);
        g.FillPath(fill, path);

        using var pen = new Pen(DragOver ? Theme.Accent : Theme.ColorFromHex("#46506a"), 1.6f)
        {
            DashStyle = DashStyle.Dash,
        };
        g.DrawPath(pen, path);

        TextRenderer.DrawText(g, Caption, Theme.FontSub, rect,
            DragOver ? Theme.Accent : Theme.TextDim,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }
}
