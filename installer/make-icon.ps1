# 绘制应用图标并生成多尺寸 .ico 文件
Add-Type -AssemblyName System.Drawing

function New-IconPng([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

    $s = $size / 1024.0   # 缩放系数（以 1024 设计稿为基准）

    # 圆角矩形深色背景
    $radius = 220 * $s
    $rect = [System.Drawing.RectangleF]::new(0, 0, $size, $size)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    $bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 30, 30, 46))
    $g.FillPath($bg, $path)

    # 仪表盘弧线：起始 135°，扫过 270°，青→紫渐变
    $margin = 200 * $s
    $inner = $size - (2 * $margin)
    $arcRect = [System.Drawing.RectangleF]::new($margin, $margin, $inner, $inner)
    $gradRect = [System.Drawing.RectangleF]::new(0, 0, $size, $size)
    $c1 = [System.Drawing.Color]::FromArgb(255, 79, 195, 247)
    $c2 = [System.Drawing.Color]::FromArgb(255, 124, 108, 240)
    $grad = [System.Drawing.Drawing2D.LinearGradientBrush]::new($gradRect, $c1, $c2, [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
    $penW = [single][Math]::Max(120 * $s, 2)
    $pen = [System.Drawing.Pen]::new($grad, $penW)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen, $arcRect, [single]135, [single]270)

    # 指针（指向右上 45° 高速区）与中心圆点
    $cx = $size / 2.0; $cy = $size / 2.0
    $needleLen = ($size / 2.0) - $margin - (30 * $s)
    $angle = -60 * [Math]::PI / 180
    $nx = $cx + $needleLen * [Math]::Cos($angle)
    $ny = $cy + $needleLen * [Math]::Sin($angle)
    $needlePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [single][Math]::Max(56 * $s, 1.5))
    $needlePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $needlePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($needlePen, [single]$cx, [single]$cy, [single]$nx, [single]$ny)
    $dotR = 70 * $s
    $dot = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillEllipse($dot, [single]($cx - $dotR), [single]($cy - $dotR), [single]($dotR * 2), [single]($dotR * 2))

    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return ,([byte[]]$ms.ToArray())   # 前置逗号防止数组被展开
}

# 生成多尺寸 PNG 并组装 ICO（PNG 压缩条目，Win10/11 支持）
$sizes = @(256, 64, 48, 32, 16)
$images = @{}
foreach ($sz in $sizes) { $images[$sz] = New-IconPng $sz }

$icoPath = "c:\Users\macan\Desktop\ai-optimize\AiOptimize\app.ico"
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
# ICONDIR
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
foreach ($sz in $sizes) {
    $data = [byte[]]$images[$sz]
    $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))  # 宽（0=256）
    $bw.Write([byte]($(if ($sz -ge 256) { 0 } else { $sz })))  # 高
    $bw.Write([byte]0); $bw.Write([byte]0)                     # 调色板/保留
    $bw.Write([uint16]1); $bw.Write([uint16]32)                # 平面/位深
    $bw.Write([uint32]$data.Length); $bw.Write([uint32]$offset)
    $offset += $data.Length
}
foreach ($sz in $sizes) { $bw.Write([byte[]]$images[$sz]) }
$bw.Close(); $fs.Close()

# 同时导出一张预览 PNG
[System.IO.File]::WriteAllBytes("c:\Users\macan\Desktop\ai-optimize\installer\icon-preview.png", [byte[]]$images[256])
Write-Output "ICO 生成完成: $icoPath ($([math]::Round((Get-Item $icoPath).Length/1KB,1)) KB)"
