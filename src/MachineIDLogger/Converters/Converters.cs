using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MachineIDLogger.Converters;

public class ByteFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ulong bytes)
        {
            if (bytes == 0) return "0 B";
            double gb = (double)bytes / (1024 * 1024 * 1024);
            if (gb >= 1.0) return $"{gb:F1} GB";
            double mb = (double)bytes / (1024 * 1024);
            return $"{mb:F1} MB";
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw rejection();

    private static NotImplementedException rejection() => new();
}

public class DiagnosticStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string s = value?.ToString()?.ToUpperInvariant() ?? "";
        return s switch
        {
            "PASS" or "OK" or "SUCCESS" => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),   // Green
            "WARNING" or "WARN" => new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)),          // Amber
            "ERROR" or "CRITICAL" or "FAIL" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),// Red
            _ => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))                           // Slate
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b;
        if (value is bool flag)
        {
            b = flag;
        }
        else if (value is string s)
        {
            b = !string.IsNullOrWhiteSpace(s);
        }
        else
        {
            b = value != null;
        }

        if (Invert) b = !b;

        // If target property requires a boolean (e.g. InfoBar.IsOpen), return bool directly!
        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return b;
        }

        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class StringToBoolConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = !string.IsNullOrWhiteSpace(value?.ToString());
        if (Invert) b = !b;
        return b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class PercentageFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d) return $"{d:F1}%";
        if (value is float f) return $"{f:F1}%";
        if (value is int i) return $"{i}%";
        if (value is uint u) return $"{u}%";
        if (value is ulong ul) return $"{ul}%";
        return $"{value}%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class DeviceCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int count) return $"{count:N0} Devices";
        return $"{value} Devices";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class BooleanNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class DiagnosticButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isRunning && isRunning ? "Running Diagnostics..." : "Run Diagnostics";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

