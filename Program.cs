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
        if (args.Contains("--import-data") || args.Contains("--check-db"))
            return RunDatabaseCommandAsync(args.Contains("--import-data")).GetAwaiter().GetResult();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task<int> RunDatabaseCommandAsync(bool import)
    {
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new System.Text.UTF8Encoding(false)) { AutoFlush = true });
        try
        {
            await using var db = new CatalogDbContext();
            if (import)
            {
                var script = Path.Combine(AppContext.BaseDirectory, "Database", "catalog.sql");
                await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(script));
            }
            var count = await db.Products.CountAsync();
            var stockRows = await db.ProductSizes.CountAsync();
            var pairs = await db.ProductSizes.SumAsync(x => x.AvailableQuantity);
            Console.WriteLine($"PostgreSQL: {count} моделей, {stockRows} записей остатков, {pairs} пар.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Ошибка БД ({error.GetType().Name}). Проверьте подключение и выполните Scripts/Setup-Database.ps1.");
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect().WithInterFont().LogToTrace();
}
