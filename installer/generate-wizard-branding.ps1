Add-Type -AssemblyName System.Drawing

$installerDir = $PSScriptRoot
$assetsBrand = Join-Path $installerDir "..\assets\brand"

# 1. Generate wizard-small.bmp (55x55)
$smallBmp = New-Object System.Drawing.Bitmap(55, 55, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gSmall = [System.Drawing.Graphics]::FromImage($smallBmp)
$gSmall.Clear([System.Drawing.Color]::White)
$gSmall.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSmall.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

$iconPath = Join-Path $assetsBrand "lexora-icon-only.png"
if (Test-Path $iconPath) {
    $srcImg = [System.Drawing.Image]::FromFile($iconPath)
    $gSmall.DrawImage($srcImg, 3, 3, 49, 49)
    $srcImg.Dispose()
}
$gSmall.Dispose()
$smallOut = Join-Path $installerDir "wizard-small.bmp"
$smallBmp.Save($smallOut, [System.Drawing.Imaging.ImageFormat]::Bmp)
$smallBmp.Dispose()
Write-Host "Generated: $smallOut"

# 2. Generate wizard-large.bmp (164x314)
$largeBmp = New-Object System.Drawing.Bitmap(164, 314, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$gLarge = [System.Drawing.Graphics]::FromImage($largeBmp)
$gLarge.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gLarge.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gLarge.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

# Gradient background (Midnight Slate & Royal Indigo)
$rect = New-Object System.Drawing.Rectangle(0, 0, 164, 314)
$c1 = [System.Drawing.Color]::FromArgb(30, 27, 75)   # #1E1B4B
$c2 = [System.Drawing.Color]::FromArgb(15, 23, 42)   # #0F172A
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 90.0)
$gLarge.FillRectangle($brush, $rect)
$brush.Dispose()

# Accent glow / decorative circle
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(50, 56, 189, 248), 2)
$gLarge.DrawEllipse($pen, -30, -30, 220, 220)
$pen.Dispose()

# Draw Lexora app icon centered
if (Test-Path $iconPath) {
    $srcAppIcon = [System.Drawing.Image]::FromFile($iconPath)
    $gLarge.DrawImage($srcAppIcon, 37, 45, 90, 90)
    $srcAppIcon.Dispose()
}

# Typography
$fontTitle = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$brushTitle = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$formatTitle = New-Object System.Drawing.StringFormat
$formatTitle.Alignment = [System.Drawing.StringAlignment]::Center
$rectTitle = New-Object System.Drawing.RectangleF(0, 150, 164, 30)
$gLarge.DrawString("Lexora", $fontTitle, $brushTitle, $rectTitle, $formatTitle)
$fontTitle.Dispose()
$brushTitle.Dispose()

$fontSub = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Regular)
$brushSub = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(203, 213, 225)) # #CBD5E1
$rectSub = New-Object System.Drawing.RectangleF(0, 182, 164, 25)
$gLarge.DrawString("English Learning Manager", $fontSub, $brushSub, $rectSub, $formatTitle)
$fontSub.Dispose()
$brushSub.Dispose()

# Subtle bottom tag
$fontVer = New-Object System.Drawing.Font("Segoe UI", 7, [System.Drawing.FontStyle]::Regular)
$brushVer = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(148, 163, 184)) # #94A3B8
$rectVer = New-Object System.Drawing.RectangleF(0, 290, 164, 20)
$gLarge.DrawString("Windows 11 Edition", $fontVer, $brushVer, $rectVer, $formatTitle)
$fontVer.Dispose()
$brushVer.Dispose()

$gLarge.Dispose()
$largeOut = Join-Path $installerDir "wizard-large.bmp"
$largeBmp.Save($largeOut, [System.Drawing.Imaging.ImageFormat]::Bmp)
$largeBmp.Dispose()
Write-Host "Generated: $largeOut"
