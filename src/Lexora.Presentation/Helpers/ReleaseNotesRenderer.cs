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
    public static void RenderMarkdown(string? rawText, StackPanel targetContainer, FrameworkElement scope, ILocalizationService? loc = null)
    {
        ArgumentNullException.ThrowIfNull(targetContainer);
        targetContainer.Children.Clear();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            var emptyText = loc?.GetString("Settings_NoReleaseNotes") ?? "Không có ghi chú chi tiết cho bản phát hành này.";
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

            // 1. Horizontal divider
            if (line is "---" or "***" or "___")
            {
                if (!previousWasDivider)
                {
                    var divider = new Border
                    {
                        Height = 1,
                        Background = ThemeResourceHelper.GetThemeBrush(scope, "CardStrokeColorDefaultBrush"),
                        Margin = new Thickness(0, 8, 0, 8),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    targetContainer.Children.Add(divider);
                    previousWasDivider = true;
                }
                continue;
            }

            previousWasDivider = false;

            // 2. Headings (#, ##, ###, ####)
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
                        1 => 16.0,
                        2 => 14.5,
                        _ => 13.5
                    },
                    LineHeight = 22,
                    LineStackingStrategy = LineStackingStrategy.MaxHeight,
                    Foreground = level <= 2
                        ? ThemeResourceHelper.GetThemeBrush(scope, "AccentTextFillColorPrimaryBrush")
                        : ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorPrimaryBrush"),
                    Margin = new Thickness(0, level <= 2 ? 10 : 8, 0, 4),
                    TextWrapping = TextWrapping.Wrap,
                    IsTextSelectionEnabled = true
                };

                targetContainer.Children.Add(headingBlock);
                continue;
            }

            // 3. Bullet list item (- or *)
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
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = ThemeResourceHelper.GetThemeBrush(scope, "AccentTextFillColorPrimaryBrush"),
                    Margin = new Thickness(2, 1, 8, 0),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetColumn(bulletIcon, 0);

                var contentBlock = new TextBlock
                {
                    FontSize = 12.5,
                    LineHeight = 20,
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

            // 4. Regular paragraph
            var paraBlock = new TextBlock
            {
                FontSize = 12.5,
                LineHeight = 22,
                LineStackingStrategy = LineStackingStrategy.MaxHeight,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
                Margin = new Thickness(0, 2, 0, 4)
            };
            AddFormattedInlines(paraBlock, line, scope);
            targetContainer.Children.Add(paraBlock);
        }
    }

    private static void AddFormattedInlines(TextBlock textBlock, string text, FrameworkElement scope)
    {
        var primaryBrush = ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorPrimaryBrush");
        var secondaryBrush = ThemeResourceHelper.GetThemeBrush(scope, "TextFillColorSecondaryBrush");

        int idx = 0;
        while (idx < text.Length)
        {
            int boldStart = text.IndexOf("**", idx, StringComparison.Ordinal);
            if (boldStart == -1)
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

            if (boldStart > idx)
            {
                var regularText = text[idx..boldStart];
                textBlock.Inlines.Add(new Run
                {
                    Text = regularText,
                    Foreground = secondaryBrush
                });
            }

            int boldEnd = text.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
            if (boldEnd == -1)
            {
                // Unclosed **, treat rest as normal text
                var remaining = text[boldStart..];
                textBlock.Inlines.Add(new Run
                {
                    Text = remaining,
                    Foreground = secondaryBrush
                });
                break;
            }

            var boldText = text[(boldStart + 2)..boldEnd];
            var boldRun = new Bold();
            boldRun.Inlines.Add(new Run
            {
                Text = boldText,
                Foreground = primaryBrush
            });
            textBlock.Inlines.Add(boldRun);

            idx = boldEnd + 2;
        }
    }
}

