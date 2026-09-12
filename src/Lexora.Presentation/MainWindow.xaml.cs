using Lexora.Presentation.Views;
using Microsoft.UI.Xaml;

namespace Lexora.Presentation;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RootFrame.Navigate(typeof(ShellPage));
    }
}
