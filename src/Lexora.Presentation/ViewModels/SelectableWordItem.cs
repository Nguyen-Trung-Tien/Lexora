using CommunityToolkit.Mvvm.ComponentModel;
using Lexora.Application.DTOs;

namespace Lexora.Presentation.ViewModels;

public partial class SelectableWordItem : ObservableObject
{
    public WordDto Word { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SelectableWordItem(WordDto word, bool isSelected = false)
    {
        Word = word;
        IsSelected = isSelected;
    }
}
