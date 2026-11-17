#nullable enable
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexora.Application.DTOs;

namespace Lexora.Presentation.ViewModels;

public partial class VocabularyViewModel
{
    public ObservableCollection<ContextUnitDto> PagedContextUnits { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnitPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasUnitNextPage))]
    [NotifyPropertyChangedFor(nameof(UnitPageDisplayInfo))]
    public partial int UnitCurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnitTotalPages))]
    [NotifyPropertyChangedFor(nameof(HasUnitPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasUnitNextPage))]
    [NotifyPropertyChangedFor(nameof(UnitPageDisplayInfo))]
    public partial int UnitPageSize { get; set; } = 12;

    public int UnitTotalPages => FilteredContextUnits.Count == 0
        ? 1
        : (int)Math.Ceiling((double)FilteredContextUnits.Count / Math.Max(1, UnitPageSize));

    public bool HasUnitPreviousPage => UnitCurrentPage > 1;
    public bool HasUnitNextPage => UnitCurrentPage < UnitTotalPages;
    public string UnitPageDisplayInfo => $"{UnitCurrentPage} / {UnitTotalPages}";

    public void RefreshUnitPagination(bool resetToFirstPage = false)
    {
        if (resetToFirstPage)
        {
            UnitCurrentPage = 1;
        }
        else if (UnitCurrentPage > UnitTotalPages)
        {
            UnitCurrentPage = UnitTotalPages;
        }

        PagedContextUnits.Clear();
        var skip = (UnitCurrentPage - 1) * UnitPageSize;
        foreach (var unit in FilteredContextUnits.Skip(skip).Take(UnitPageSize))
        {
            PagedContextUnits.Add(unit);
        }

        OnPropertyChanged(nameof(UnitTotalPages));
        OnPropertyChanged(nameof(HasUnitPreviousPage));
        OnPropertyChanged(nameof(HasUnitNextPage));
        OnPropertyChanged(nameof(UnitPageDisplayInfo));
    }

    public void ChangeUnitPageSize(int newSize)
    {
        if (newSize <= 0 || UnitPageSize == newSize) return;
        UnitPageSize = newSize;
        RefreshUnitPagination(resetToFirstPage: true);
    }

    [RelayCommand]
    public void FirstUnitPage()
    {
        if (!HasUnitPreviousPage) return;
        UnitCurrentPage = 1;
        RefreshUnitPagination();
    }

    [RelayCommand]
    public void PreviousUnitPage()
    {
        if (!HasUnitPreviousPage) return;
        UnitCurrentPage--;
        RefreshUnitPagination();
    }

    [RelayCommand]
    public void NextUnitPage()
    {
        if (!HasUnitNextPage) return;
        UnitCurrentPage++;
        RefreshUnitPagination();
    }

    [RelayCommand]
    public void LastUnitPage()
    {
        if (!HasUnitNextPage) return;
        UnitCurrentPage = UnitTotalPages;
        RefreshUnitPagination();
    }
}
