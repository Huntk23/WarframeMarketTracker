using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WarframeMarketTracker.Converters;

public class ValidityToTagConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return isValid ? "Valid" : "Invalid";
        }

        return "Invalid";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}