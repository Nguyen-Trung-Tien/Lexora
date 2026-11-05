$files = Get-ChildItem -Path "src/Lexora.Presentation/Views" -Recurse -Filter "*.xaml"
$issues = @()

foreach ($file in $files) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '<Button(?:\s|/?>)') {
            # Collect full button opening tag
            $tag = ""
            $hasStyle = $false
            $hasCornerRadius = $false
            $hasBorderThickness = $false
            $hasBackground = $false
            $hasCursor = $false
            $hasToolTip = $false
            $contentOnSameLine = ""

            for ($j = $i; $j -lt [Math]::Min($lines.Count, $i + 20); $j++) {
                $tag += " " + $lines[$j].Trim()
                if ($lines[$j] -match 'Style\s*=') { $hasStyle = $true }
                if ($lines[$j] -match 'CornerRadius\s*=') { $hasCornerRadius = $true }
                if ($lines[$j] -match 'BorderThickness\s*=') { $hasBorderThickness = $true }
                if ($lines[$j] -match 'Background\s*=') { $hasBackground = $true }
                if ($lines[$j] -match 'CursorHelper\.Cursor\s*=') { $hasCursor = $true }
                if ($lines[$j] -match 'ToolTipService\.ToolTip\s*=') { $hasToolTip = $true }
                if ($lines[$j] -match '>') { 
                    break 
                }
            }

            # Check for inline custom buttons that do not inherit from a Fluent ThemeResource style
            if ($tag -match 'Background="Transparent"' -and -not $hasStyle) {
                $issues += [PSCustomObject]@{
                    File = $file.Name
                    Line = $i + 1
                    Type = "TransparentNoStyle"
                    Description = "Inline Transparent button without SubtleButtonStyle"
                    Tag = $tag.Trim()
                }
            } elseif ($hasBackground -and -not $hasStyle) {
                $issues += [PSCustomObject]@{
                    File = $file.Name
                    Line = $i + 1
                    Type = "CustomBackgroundNoStyle"
                    Description = "Inline Background/Border without Style={ThemeResource ...}"
                    Tag = $tag.Trim()
                }
            } elseif (-not $hasStyle -and -not $hasCornerRadius) {
                $issues += [PSCustomObject]@{
                    File = $file.Name
                    Line = $i + 1
                    Type = "MissingStyleAndRadius"
                    Description = "Default unstyled button without Style or CornerRadius"
                    Tag = $tag.Trim()
                }
            }
        }
    }
}

$issues | Format-Table -Property File, Line, Type, Description -AutoSize
Write-Host "Total inconsistent buttons: $($issues.Count)"
