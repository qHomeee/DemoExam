using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoForMuhin.Data;
using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private string _status = "Загрузка каталога...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadProductsCommand))]
    private bool _isLoading;

    private bool CanLoadProducts() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanLoadProducts))]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        Status = "Загрузка из PostgreSQL...";
        try
        {
            await using var db = new CatalogDbContext();
            // Include загружает связанные категории и остатки по размерам.
            var products = await db.Products.AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Sizes)
                .OrderBy(x => x.Id)
                .ToListAsync();
            Products = new ObservableCollection<Product>(products);
            Status = $"Загружено товаров: {Products.Count}. Данные из PostgreSQL.";
        }
        catch (Exception)
        {
            Status = "Ошибка загрузки. Проверьте PostgreSQL и файл .local/connection-string.txt. Затем перезапустите приложение.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}


