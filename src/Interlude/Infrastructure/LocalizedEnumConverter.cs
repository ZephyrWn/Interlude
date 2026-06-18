using System.Globalization;
using System.Windows.Data;
using Interlude.Services;

namespace Interlude.Infrastructure;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return LocalizationService.TranslateResourceValue(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
