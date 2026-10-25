using Avalonia;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DemoForMuhin.Data;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin;

sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        if (args.Contains("--check-db"))
            return CheckDatabaseAsync().GetAwaiter().GetResult();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task<int> CheckDatabaseAsync()
    {
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false)) { AutoFlush = true });
        try
        {
            await using var db = new CatalogDbContext();

            var count = await db.Products.CountAsync();
            var stockRows = await db.ProductSizes.CountAsync();
            var pairs = await db.ProductSizes.SumAsync(x => x.AvailableQuantity);
            Console.WriteLine($"PostgreSQL: {count} моделей, {stockRows} записей остатков, {pairs} пар.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Ошибка БД ({error.GetType().Name})");
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect().WithInterFont().LogToTrace();
}
