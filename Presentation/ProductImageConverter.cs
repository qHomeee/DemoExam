using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace DemoForMuhin.Presentation;

public class ProductImageConverter : IValueConverter
{
    private readonly Dictionary<string, Bitmap> _cache = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value is string file && !string.IsNullOrWhiteSpace(file) ? Path.GetFileName(file) : "picture.png";
        var uri = new Uri($"avares://DemoForMuhin/Assets/Products/{name}");
        if (!AssetLoader.Exists(uri)) name = "picture.png";
        if (!_cache.TryGetValue(name, out var bitmap))
        {
            using var stream = AssetLoader.Open(new Uri($"avares://DemoForMuhin/Assets/Products/{name}"));
            bitmap = new Bitmap(stream);
            _cache.Add(name, bitmap);
        }
        return bitmap;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
