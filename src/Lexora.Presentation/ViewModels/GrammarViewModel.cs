using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;
using Lexora.Application.Interfaces;

namespace Lexora.Presentation.ViewModels;

public partial class GrammarViewModel : ObservableObject
{
    private readonly IGrammarService _grammarService;

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial GrammarRuleDto? SelectedRule { get; set; }

    public ObservableCollection<GrammarRuleDto> Rules { get; } = new();

    public GrammarViewModel(IGrammarService grammarService)
    {
        _grammarService = grammarService;
    }

    [RelayCommand]
    public async Task LoadRulesAsync()
    {
        IsLoading = true;
        try
        {
            var results = string.IsNullOrWhiteSpace(SearchQuery)
                ? await _grammarService.GetAllRulesAsync()
                : await _grammarService.SearchRulesAsync(SearchQuery);

            Rules.Clear();
            foreach (var r in results)
            {
                Rules.Add(r);
            }

            if (Rules.Count > 0 && SelectedRule == null)
            {
                SelectedRule = Rules[0];
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
