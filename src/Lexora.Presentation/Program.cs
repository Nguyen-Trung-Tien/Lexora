using System.Runtime.InteropServices;
using Microsoft.Windows.AppLifecycle;

namespace Lexora.Presentation;

public static class Program
{
    private const string SingleInstanceKey = "Lexora_Desktop_SingleInstance_Key";

    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        bool isFirstInstance = DecideRedirection();
        if (isFirstInstance)
        {
            Microsoft.UI.Xaml.Application.Start((p) => new App());
        }
    }

    private static bool DecideRedirection()
    {
        bool isFirstInstance = false;
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var mainInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);

        if (mainInstance.IsCurrent)
        {
            isFirstInstance = true;
            mainInstance.Activated += OnAppActivated;
        }
        else
        {
            // Already running! Grant permission to bring the existing window to the foreground
            AllowSetForegroundWindow((uint)mainInstance.ProcessId);
            mainInstance.RedirectActivationToAsync(appArgs).AsTask().Wait();
        }

        return isFirstInstance;
    }

    public static bool PendingActivation { get; set; }

    private static void OnAppActivated(object? sender, AppActivationArguments args)
    {
        // When a second instance redirects to us, restore and activate MainWindow
        if (App.Current?.MainWindow is MainWindow window)
        {
            window.BringToForeground();
        }
        else
        {
            PendingActivation = true;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);
}
