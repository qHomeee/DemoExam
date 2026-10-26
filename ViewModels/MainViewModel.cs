using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoForMuhin.Data;
using DemoForMuhin.Models;
using DemoForMuhin.Services;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    public event Action? SignOutRequested;
    public string CurrentUserName => _authService.CurrentUser?.FullName ?? "Гость";
    public string CurrentRoleName => _authService.CurrentUser?.Role.Name ?? "Просмотр каталога";
    public string SignOutCaption => _authService.CurrentUser is null ? "Войти" : "Выйти";

    public MainViewModel(AuthService authService) => _authService = authService;

    [RelayCommand]
    private void SignOut() => SignOutRequested?.Invoke();

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
