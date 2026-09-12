using Lexora.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Lexora.Presentation.Views;

public sealed partial class GrammarPage : Page
{
    public GrammarViewModel ViewModel { get; }

    public GrammarPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<GrammarViewModel>();

        Loaded += async (s, e) =>
        {
            await ViewModel.LoadRulesAsync();
        };
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        await ViewModel.LoadRulesAsync();
    }
}
