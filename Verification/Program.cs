using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DemoForMuhin.Data;
using DemoForMuhin.Models;
using DemoForMuhin.Presentation;
using DemoForMuhin.ViewModels;
using DemoForMuhin.Views;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Verification;

public static class VerificationApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia().UseHarfBuzz().WithInterFont()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public static class Program
{
    public static async Task<int> Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        try
        {
            await using (var db = new CatalogDbContext())
            {
                Check(await db.Products.CountAsync() == 31, "31 official products");
                Check(await db.Sizes.CountAsync() == 35, "35 sizes, including fractional values");
                Check(await db.ProductSizes.CountAsync() == 92, "92 stock records");
                Check(await db.ProductSizes.SumAsync(x => x.AvailableQuantity) == 1274, "1274 pairs match the source workbook");
                Check(await db.Sizes.AnyAsync(x => x.Value == 36.5m), "Fractional size 36.5 is preserved");
            }
            using var session = HeadlessUnitTestSession.StartNew(typeof(VerificationApp));
            await session.Dispatch(async () =>
            {
                var vm = new MainViewModel();
                var window = new MainWindow { DataContext = vm };
                window.Show();
                Check(!window.GetVisualDescendants().OfType<Button>().Any(), "The catalog has no load button");
                var load = vm.LoadProductsCommand.ExecutionTask ?? throw new Exception("Opening the window did not start loading");
                await load;
                Check(vm.Products.Count == 31 && !vm.IsLoading, "Opening the window automatically loads real data");
                Check(vm.Products.All(x => x.Category is not null && x.Sizes.Count > 0), "Related categories and stock are loaded");
                Check(vm.Products.Count(x => x.TotalQuantity <= 3) == 3, "Three low-stock models match the source");
                Check(vm.Products.First().Price == 2190m && vm.Products.First().TotalQuantity == 120, "First product price and quantity match Excel");
                var images = new ProductImageConverter();
                foreach (var product in vm.Products)
                    Check(images.Convert(product.ImageFile, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is not null, "Image for product " + product.Id);
                Check(images.Convert("missing.png", typeof(object), null, System.Globalization.CultureInfo.InvariantCulture) is not null, "Missing image uses original picture.png");
                var output = Path.GetFullPath("artifacts");
                Directory.CreateDirectory(output);
                using (var frame = window.CaptureRenderedFrame() ?? throw new Exception("No rendered frame"))
                    frame.Save(Path.Combine(output, "catalog.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                await vm.LoadProductsCommand.ExecuteAsync(null);
                Check(vm.Products.Count == 31, "Repeated loading does not duplicate products");
                var original = Environment.GetEnvironmentVariable("DEMOFORMUHIN_CONNECTION_STRING");
                try
                {
                    Environment.SetEnvironmentVariable("DEMOFORMUHIN_CONNECTION_STRING", "Host=127.0.0.1;Port=1;Database=unavailable;Username=demo;Password=unused;Pooling=false");
                    await vm.LoadProductsCommand.ExecuteAsync(null);
                    Check(vm.Status.StartsWith("Ошибка") && !vm.IsLoading && vm.Products.Count == 31, "Connection error is handled and previous rows remain visible");
                }
                finally { Environment.SetEnvironmentVariable("DEMOFORMUHIN_CONNECTION_STRING", original); }
                await vm.LoadProductsCommand.ExecuteAsync(null);
                Check(vm.Products.Count == 31 && !vm.Status.StartsWith("Ошибка"), "Loading recovers after the connection is restored");
                window.Width = 780;
                window.Height = 500;
                using (var frame = window.CaptureRenderedFrame() ?? throw new Exception("No compact frame"))
                    frame.Save(Path.Combine(output, "catalog-compact.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                window.Close();
                return 0;
            }, CancellationToken.None);
            Console.WriteLine("PASS: all catalog checks; previews saved in artifacts/.");
            Environment.Exit(0);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.GetType().Name}: {exception.Message}");
            Environment.Exit(1);
            return 1;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }
}




