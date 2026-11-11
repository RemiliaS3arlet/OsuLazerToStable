Add-Type -AssemblyName System.Drawing

$outDir = 'C:\Users\somer\Documents\deepseek-harness\default-workspace\OsuTransfer\app\assets'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$W = 1200; $H = 300
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# 基础竖向渐变
$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H)
$bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, [System.Drawing.Color]::FromArgb(255, 21, 22, 29), [System.Drawing.Color]::FromArgb(255, 30, 26, 43), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillRectangle($bg, $rect)

function Add-Glow([int]$cx, [int]$cy, [int]$rx, [int]$ry, [int]$a, $color) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddEllipse($cx - $rx, $cy - $ry, $rx * 2, $ry * 2)
    $brush = New-Object System.Drawing.Drawing2D.PathGradientBrush($path)
    $brush.CenterPoint = New-Object System.Drawing.PointF($cx, $cy)
    $brush.CenterColor = [System.Drawing.Color]::FromArgb($a, $color.R, $color.G, $color.B)
    $brush.SurroundColors = @([System.Drawing.Color]::FromArgb(0, $color.R, $color.G, $color.B))
    $g.FillPath($brush, $path)
    $brush.Dispose(); $path.Dispose()
}

# 粉色光晕（左）与青色光晕（右）
Add-Glow 170 160 420 300 95 ([System.Drawing.Color]::FromArgb(255, 255, 102, 170))
Add-Glow 1060 120 380 260 60 ([System.Drawing.Color]::FromArgb(255, 62, 224, 255))
Add-Glow 620 30 300 180 30 ([System.Drawing.Color]::FromArgb(255, 255, 214, 102))

# 斜向细线纹理
$penThin = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(12, 255, 255, 255), 1)
for ($i = -300; $i -lt $W + 300; $i += 46) {
    $g.DrawLine($penThin, $i, 0, $i + $H, $H)
}
$penThin.Dispose()

# 节奏波形（底部）
$rnd = New-Object System.Random(20261111)
$pts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
$n = 120
for ($i = 0; $i -le $n; $i++) {
    $x = $W * $i / $n
    $amp = 26 + [Math]::Abs([Math]::Sin($i * 0.35)) * 46
    $y = 218 - [Math]::Abs([Math]::Sin($i * 0.62 + 0.4)) * $amp
    $pts.Add((New-Object System.Drawing.PointF($x, $y)))
}
$curve = New-Object System.Drawing.Drawing2D.GraphicsPath
$curve.AddCurve($pts.ToArray(), 0.35)
$fill = New-Object System.Drawing.Drawing2D.PathGradientBrush($curve)
$fill.CenterColor = [System.Drawing.Color]::FromArgb(46, 255, 102, 170)
$fill.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 62, 224, 255))
$g.FillPath($fill, $curve)
$wavePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 255, 122, 190), 1.6)
$g.DrawPath($wavePen, $curve)
$wavePen.Dispose(); $fill.Dispose(); $curve.Dispose()

# 光点
for ($i = 0; $i -lt 90; $i++) {
    $x = $rnd.Next(0, $W); $y = $rnd.Next(0, $H)
    $r = 0.6 + $rnd.NextDouble() * 1.8
    $a = 30 + $rnd.Next(0, 120)
    $c = if ($rnd.NextDouble() -lt 0.6) { [System.Drawing.Color]::FromArgb($a, 255, 180, 220) } else { [System.Drawing.Color]::FromArgb($a, 150, 235, 255) }
    $b = New-Object System.Drawing.SolidBrush($c)
    $g.FillEllipse($b, [single]($x - $r), [single]($y - $r), [single]($r * 2), [single]($r * 2))
    $b.Dispose()
}

# 底部压暗 + 顶部微亮
$topRect = New-Object System.Drawing.Rectangle(0, 0, $W, 90)
$shadeTop = New-Object System.Drawing.Drawing2D.LinearGradientBrush($topRect, [System.Drawing.Color]::FromArgb(30, 255, 255, 255), [System.Drawing.Color]::FromArgb(0, 255, 255, 255), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillRectangle($shadeTop, $topRect)
$bottomY = [int]$H - 110
$bottomRect = New-Object System.Drawing.Rectangle(0, $bottomY, $W, 110)
$shadeBottom = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bottomRect, [System.Drawing.Color]::FromArgb(0, 0, 0, 0), [System.Drawing.Color]::FromArgb(120, 8, 8, 12), [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillRectangle($shadeBottom, $bottomRect)
$shadeTop.Dispose(); $shadeBottom.Dispose()

# 底部强调线（用像素点画，避免 Pen 兼容问题）
$edgeY = [int]$H - 1
for ($px = 0; $px -lt $W; $px++) {
    $bmp.SetPixel($px, $edgeY, [System.Drawing.Color]::FromArgb(90, 255, 102, 170))
}

$g.Dispose()
$path = Join-Path $outDir 'banner.png'
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
"saved: $path  $((Get-Item $path).Length) bytes"
