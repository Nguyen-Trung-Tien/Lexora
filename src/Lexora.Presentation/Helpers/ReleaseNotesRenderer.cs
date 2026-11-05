using Lexora.Presentation.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System.Text.RegularExpressions;

namespace Lexora.Presentation.Helpers;

/// <summary>
/// Renders structured Markdown-formatted release notes into native WinUI 3 controls.
/// Guarantees high visual fidelity, WCAG contrast compliance, and full Dark/Light theme adaptability.
/// </summary>
public static class ReleaseNotesRenderer
{
    public static void RenderMarkdown(
        string? rawText,
        StackPanel targetContainer,
        FrameworkElement scope,
        ILocalizationService? loc = null,
        bool skipTopLevelTitle = false)
    {
        ArgumentNullException.ThrowIfNull(targetContainer);
        targetContainer.Children.Clear();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            var emptyText = loc?.GetString("Settings_NoReleaseNotes") ?? "No release notes available for this version.";
            var emptyBlock = new TextBlock
            {
                Text = emptyText,
                FontSize = 12.5,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorTertiaryBrush"),
                Margin = new Thickness(0, 4, 0, 4)
            };
            targetContainer.Children.Add(emptyBlock);
            return;
        }

        var lines = rawText.Replace("\r\n", "\n").Split('\n');
        bool previousWasDivider = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Skip empty lines, but avoid multiple consecutive vertical gaps
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            // Skip top-level title if caller already presents title in dialog/card header
            if (skipTopLevelTitle)
            {
                if (line.StartsWith("# ") && !line.StartsWith("## "))
                {
                    continue;
                }
                if (line.StartsWith("> ") && (line.Contains("Release Date") || line.Contains("Ngày phát hành") || line.Contains("Version") || line.Contains("Phiên bản")))
                {
                    continue;
                }
            }

            // 1. Horizontal divider
            if (line is "---" or "***" or "___")
            {
                if (!previousWasDivider)
                {
                    var divider = new Border
                    {
                        Height = 1,
                        Background = ThemeResourceHelper.GetThemeBrush(scope, "CardStrokeColorDefaultBrush"),
                        Margin = new Thickness(0, 6, 0, 6),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    targetContainer.Children.Add(divider);
                    previousWasDivider = true;
                }
                continue;
            }

            previousWasDivider = false;

            // 2. Blockquote (> text)
            if (line.StartsWith("> "))
            {
                var quoteContent = line[2..].Trim();
                var quoteBorder = new Border
                {
                    Background = ThemeResourceHelper.GetThemeBrush(scope, "SubtleFillColorSecondaryBrush"),
                    BorderBrush = ThemeResourceHelper.GetThemeBrush(scope, "AccentFillColorDefaultBrush"),
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    CornerRadius = new CornerRadius(0, 6, 6, 0),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 3, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var quoteText = new TextBlock
                {
                    FontSize = 12,
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    Foreground = ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorSecondaryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                };
                AddFormattedInlines(quoteText, quoteContent, scope);
                quoteBorder.Child = quoteText;
                targetContainer.Children.Add(quoteBorder);
                continue;
            }

            // 3. Headings (#, ##, ###, ####)
            if (line.StartsWith('#'))
            {
                int level = 0;
                while (level < line.Length && line[level] == '#') level++;
                var headingText = line[level..].Trim();

                var headingBlock = new TextBlock
                {
                    Text = headingText,
                    FontWeight = level <= 2 ? FontWeights.Bold : FontWeights.SemiBold,
                    FontSize = level switch
                    {
                        1 => 15.5,
                        2 => 14.0,
                        _ => 13.0
                    },
                    LineHeight = 20,
                    LineStackingStrategy = LineStackingStrategy.MaxHeight,
                    Foreground = level <= 2
                        ? ThemeResourceHelper.GetThemeBrush(scope, "AccentTextFillColorPrimaryBrush")
                        : ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorPrimaryBrush"),
                    Margin = new Thickness(0, level <= 2 ? 8 : 6, 0, 3),
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                };

                targetContainer.Children.Add(headingBlock);
                continue;
            }

            // 4. Bullet list item (- or * or +)
            if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
            {
                var content = line[2..].Trim();

                var itemGrid = new Grid
                {
                    Margin = new Thickness(0, 2, 0, 3)
                };
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var bulletIcon = new TextBlock
                {
                    Text = "•",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = ThemeResourceHelper.GetThemeBrush(scope, "AccentTextFillColorPrimaryBrush"),
                    Margin = new Thickness(2, 1, 8, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(bulletIcon, 0);

                var contentBlock = new TextBlock
                {
                    FontSize = 12.0,
                    LineHeight = 19,
                    LineStackingStrategy = LineStackingStrategy.MaxHeight,
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true,
                    VerticalAlignment = VerticalAlignment.Center
                };
                AddFormattedInlines(contentBlock, content, scope);
                Grid.SetColumn(contentBlock, 1);

                itemGrid.Children.Add(bulletIcon);
                itemGrid.Children.Add(contentBlock);
                targetContainer.Children.Add(itemGrid);
                continue;
            }

            // 5. Regular paragraph
            var paraBlock = new TextBlock
            {
                FontSize = 12.0,
                LineHeight = 20,
                LineStackingStrategy = LineStackingStrategy.MaxHeight,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                Margin = new Thickness(0, 2, 0, 3)
            };
            AddFormattedInlines(paraBlock, line, scope);
            targetContainer.Children.Add(paraBlock);
        }
    }

    private static void AddFormattedInlines(TextBlock textBlock, string text, FrameworkElement scope)
    {
        var primaryBrush = ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorPrimaryBrush");
        var secondaryBrush = ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorSecondaryBrush");
        var accentBrush = ThemeResourceHelper.GetThemeBrush(scope, "AccentTextFillColorPrimaryBrush");

        int idx = 0;
        while (idx < text.Length)
        {
            int boldStart = text.IndexOf("**", idx, StringComparison.Ordinal);
            int codeStart = text.IndexOf('`', idx);

            // Determine whichever special formatting comes first
            if (boldStart != -1 && (codeStart == -1 || boldStart < codeStart))
            {
                if (boldStart > idx)
                {
                    textBlock.Inlines.Add(new Run
                    {
                        Text = text[idx..boldStart],
                        Foreground = secondaryBrush
                    });
                }

                int boldEnd = text.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
                if (boldEnd == -1)
                {
                    textBlock.Inlines.Add(new Run
                    {
                        Text = text[boldStart..],
                        Foreground = secondaryBrush
                    });
                    break;
                }

                var boldRun = new Bold();
                boldRun.Inlines.Add(new Run
                {
                    Text = text[(boldStart + 2)..boldEnd],
                    Foreground = primaryBrush
                });
                textBlock.Inlines.Add(boldRun);
                idx = boldEnd + 2;
            }
            else if (codeStart != -1)
            {
                if (codeStart > idx)
                {
                    textBlock.Inlines.Add(new Run
                    {
                        Text = text[idx..codeStart],
                        Foreground = secondaryBrush
                    });
                }

                int codeEnd = text.IndexOf('`', codeStart + 1);
                if (codeEnd == -1)
                {
                    textBlock.Inlines.Add(new Run
                    {
                        Text = text[codeStart..],
                        Foreground = secondaryBrush
                    });
                    break;
                }

                textBlock.Inlines.Add(new Run
                {
                    Text = text[(codeStart + 1)..codeEnd],
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas, Cascadia Code"),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = accentBrush
                });
                idx = codeEnd + 1;
            }
            else
            {
                var remaining = text[idx..];
                if (!string.IsNullOrEmpty(remaining))
                {
                    textBlock.Inlines.Add(new Run
                    {
                        Text = remaining,
                        Foreground = secondaryBrush
                    });
                }
                break;
            }
        }
    }
}

