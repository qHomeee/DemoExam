using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DemoForMuhin.Presentation;

public class StockColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int count && count <= 3 ? Brush.Parse("#ff8080") : Brushes.White;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
