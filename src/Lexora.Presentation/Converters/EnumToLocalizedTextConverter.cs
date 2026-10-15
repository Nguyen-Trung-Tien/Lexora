using Lexora.Domain.Enums;
using Lexora.Presentation.Services;
using Microsoft.UI.Xaml.Data;

namespace Lexora.Presentation.Converters;

public sealed partial class EnumToLocalizedTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is null) return string.Empty;

        return value switch
        {
            PartOfSpeech pos => LocalizationHelper.FormatPartOfSpeech(pos),
            LearningStatus status => LocalizationHelper.FormatStatus(status),
            StorageLearningStatus storageStatus => LocalizationHelper.FormatStorageStatus(storageStatus),
            ExamType exam => LocalizationHelper.FormatExamType(exam),
            CefrLevel cefr => LocalizationHelper.FormatCefrLevel(cefr),
            ReviewRating rating => LocalizationHelper.FormatRating(rating),
            string str => str,
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
