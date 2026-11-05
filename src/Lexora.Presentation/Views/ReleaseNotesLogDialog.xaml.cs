using Lexora.Application.Interfaces;
using Lexora.Presentation.Helpers;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

/// <summary>
/// Elegant Fluent 2 dialog for viewing, navigating, and reading offline release notes across versions.
/// </summary>
public sealed partial class ReleaseNotesLogDialog : ContentDialog, ILocalizableView, IThemeAwareView
{
    private readonly IReadOnlyList<ReleaseNoteItem> _notes;
    private readonly ILocalizationService _loc;
    private readonly string? _initialVersion;

    public ReleaseNotesLogDialog(
        IReadOnlyList<ReleaseNoteItem> notes,
        ILocalizationService loc,
        string? initialVersion = null)
    {
        InitializeComponent();
        _notes = notes ?? Array.Empty<ReleaseNoteItem>();
        _loc = loc ?? throw new ArgumentNullException(nameof(loc));
        _initialVersion = initialVersion;

        Loaded += ReleaseNotesLogDialog_Loaded;
        ApplyLocalization();
        ApplyThemeResources();
    }

    public ILocalizationService Loc => _loc;

    public void OnLanguageChanged()
    {
        ApplyLocalization();
    }

    public void OnThemeChanged()
    {
        ApplyThemeResources();
    }

    public void ApplyLocalization()
    {
        Title = _loc.GetString("ReleaseNotes_DialogTitle");
        PrimaryButtonText = _loc.GetString("ReleaseNotes_CloseButton");

        if (DialogSubtitleText != null)
        {
            DialogSubtitleText.Text = _loc.GetString("ReleaseNotes_DialogSubtitle");
        }

        if (SelectVersionLabel != null)
        {
            SelectVersionLabel.Text = _loc.GetString("ReleaseNotes_SelectVersion");
        }

        if (CurrentVersionBadgeText != null)
        {
            CurrentVersionBadgeText.Text = _loc.GetString("ReleaseNotes_CurrentBadge");
        }

        if (NotesContentLabel != null)
        {
            NotesContentLabel.Text = _loc.GetString("ReleaseNotes_NotesContentLabel");
        }

        if (OfflineNoteText != null)
        {
            OfflineNoteText.Text = _loc.GetString("ReleaseNotes_OfflineNote");
        }
    }

    public void ApplyThemeResources()
    {
        // Re-render markdown on theme switch to ensure correct brush bindings
        if (VersionComboBox?.SelectedItem is ReleaseNoteItem selectedNote)
        {
            RenderSelectedNote(selectedNote);
        }
    }

    private void ReleaseNotesLogDialog_Loaded(object sender, RoutedEventArgs e)
    {
        PopulateVersionComboBox();
    }

    private void PopulateVersionComboBox()
    {
        VersionComboBox.Items.Clear();

        if (_notes.Count == 0)
        {
            ReleaseTitleText.Text = _loc.GetString("Settings_NoReleaseNotes");
            ReleaseSummaryText.Text = string.Empty;
            NotesContainer.Children.Clear();
            return;
        }

        int selectedIndex = 0;
        var currentBadgeSuffix = _loc.GetString("ReleaseNotes_CurrentSuffix");

        for (int i = 0; i < _notes.Count; i++)
        {
            var note = _notes[i];
            var display = note.IsCurrent
                ? $"v{note.Version} — {note.ReleaseDate} {currentBadgeSuffix}"
                : $"v{note.Version} — {note.ReleaseDate}";

            var cboItem = new ComboBoxItem
            {
                Content = display,
                Tag = note
            };

            VersionComboBox.Items.Add(cboItem);

            if (!string.IsNullOrWhiteSpace(_initialVersion) &&
                string.Equals(note.Version, _initialVersion.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
            {
                selectedIndex = i;
            }
            else if (string.IsNullOrWhiteSpace(_initialVersion) && note.IsCurrent)
            {
                selectedIndex = i;
            }
        }

        if (VersionComboBox.Items.Count > 0)
        {
            VersionComboBox.SelectedIndex = selectedIndex;
        }
    }

    private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VersionComboBox.SelectedItem is ComboBoxItem item && item.Tag is ReleaseNoteItem note)
        {
            RenderSelectedNote(note);
        }
    }

    private void RenderSelectedNote(ReleaseNoteItem note)
    {
        ReleaseTitleText.Text = note.Title;
        ReleaseDateText.Text = note.ReleaseDate;
        ReleaseSummaryText.Text = note.Summary;

        CurrentVersionBadge.Visibility = note.IsCurrent
            ? Visibility.Visible
            : Visibility.Collapsed;

        ReleaseNotesRenderer.RenderMarkdown(note.MarkdownContent, NotesContainer, this, _loc, skipTopLevelTitle: true);
    }
}

