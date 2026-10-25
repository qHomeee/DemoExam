using Avalonia;
using System;
using System.Linq;
using System.Threading.Tasks;
using DemoForMuhin.Data;
using DemoForMuhin.Services;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin;

sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--seed-demo") || args.Contains("--check-db"))
            return RunDatabaseCommandAsync(args.Contains("--seed-demo")).GetAwaiter().GetResult();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task<int> RunDatabaseCommandAsync(bool seed)
    {
        try
        {
            if (seed)
            {
                await using var db = new CatalogDbContext(DatabaseSettings.CreateOptions());
                await DemoData.SeedAsync(db);
            }
            var products = await new CatalogService().LoadAsync(DateOnly.FromDateTime(DateTime.Today));
            Console.WriteLine($"PostgreSQL: прочитано моделей — {products.Count}.");
            foreach (var product in products)
                Console.WriteLine($"#{product.Id} {product.Name}: {product.CurrentPrice:F2} руб., доступно {product.AvailableQuantity}, скидка {product.HasDiscount}");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Ошибка БД ({error.GetType().Name}). Проверьте PostgreSQL и локальную настройку подключения; пароль в журнал не выводится.");
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
