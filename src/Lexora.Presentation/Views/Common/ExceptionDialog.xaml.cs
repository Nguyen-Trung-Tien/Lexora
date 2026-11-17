using System.Diagnostics;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Lexora.Presentation.Views.Common;

public sealed partial class ExceptionDialog : ContentDialog
{
    public ILocalizationService Loc { get; }
    private readonly string _diagnosticText;

    public ExceptionDialog(
        ILocalizationService localizationService,
        Exception exception,
        string source,
        string? friendlyMessage = null)
    {
        Loc = localizationService;
        InitializeComponent();

        Title = Loc.GetString("ExceptionDialog_Title");
        CloseButtonText = Loc.GetString("ExceptionDialog_Close");

        FriendlyMessageBar.Title = source;
        FriendlyMessageBar.Message = string.IsNullOrWhiteSpace(friendlyMessage)
            ? exception.Message
            : friendlyMessage;

        _diagnosticText =
            $"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\n" +
            $"Source: {source}\n" +
            $"Exception: {exception.GetType().FullName}\n" +
            $"Message: {exception.Message}\n\n" +
            $"StackTrace:\n{exception.StackTrace}\n" +
            (exception.InnerException != null ? $"\nInnerException:\n{exception.InnerException}" : string.Empty);

        TechnicalDetailsBox.Text = _diagnosticText;
    }

    private void CopyDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(_diagnosticText);
            Clipboard.SetContent(dataPackage);
            CopyButtonText.Text = Loc.GetString("ExceptionDialog_Copied");
        }
        catch
        {
            // Ignore clipboard lock errors
        }
    }

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexora");
            Directory.CreateDirectory(logDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = logDir,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore shell launch failures
        }
    }
}
