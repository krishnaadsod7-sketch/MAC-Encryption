using System.Globalization;

namespace MacE.Helpers;

/// <summary>
/// Converts a boolean to a Color.
/// When parameter is 'Primary': True -> Active Indigo/Purple, False -> Subdued/Border.
/// Otherwise: True -> Red (error), False -> Gray.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var param = parameter?.ToString();
        var isTrue = value is bool b && b;

        if (param == "Primary")
        {
            return isTrue ? Color.FromArgb("#4F46E5") : Color.FromArgb("#6B7280");
        }

        if (isTrue)
            return Colors.Red;

        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts a string to a boolean.
/// Non-null, non-empty string → True; otherwise → False.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrEmpty(s);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Inverts a boolean value. True → False, False → True.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
