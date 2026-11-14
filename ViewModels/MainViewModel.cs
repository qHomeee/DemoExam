using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoForMuhin.Data;
using DemoForMuhin.Models;
using DemoForMuhin.Presentation;
using DemoForMuhin.Services;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.ViewModels;

public record CategoryFilter(int? Id, string Name);

public partial class MainViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private List<Product> _catalog = new();

    public event Action? SignOutRequested;
    public OrderService Orders { get; }
    public string CurrentUserName => _authService.CurrentUser?.FullName ?? "Гость";
    public string CurrentRoleName => _authService.CurrentUser?.Role.Name ?? "Просмотр каталога";
    public string SignOutCaption => IsSignedIn ? "Выйти" : "Войти";
    public bool IsSignedIn => _authService.CurrentUser is not null;
    public bool CanManageOrders => _authService.CanManageOrders;
    public bool CanEditOrders => _authService.CanEditOrders;
    public CartViewModel Cart { get; }

    public MainViewModel(AuthService authService)
    {
        _authService = authService;
        Orders = new OrderService(authService);
        Cart = new CartViewModel(authService.CurrentUser);
        Cart.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(CartViewModel.Caption))
                OnPropertyChanged(nameof(CartCaption));
        };
    }

    public string CartCaption => Cart.Caption;
    public IReadOnlyList<string> SortOptions { get; } = new[] { "Без сортировки", "Сначала дешевле", "Сначала дороже" };

    [ObservableProperty]
    private IReadOnlyList<CategoryFilter> _categories = Array.Empty<CategoryFilter>();

    [ObservableProperty]
    private CategoryFilter? _selectedCategory;

    [ObservableProperty]
    private int _sortIndex;

    [ObservableProperty]
    private string? _search;

    [ObservableProperty]
    private IReadOnlyList<Product> _products = Array.Empty<Product>();

    [ObservableProperty]
    private string _status = "Загрузка каталога…";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadProductsCommand))]
    private bool _isLoading;

    [RelayCommand]
    private void SignOut() => SignOutRequested?.Invoke();

    partial void OnSelectedCategoryChanged(CategoryFilter? value) => ApplyFilters();
    partial void OnSortIndexChanged(int value) => ApplyFilters();
    partial void OnSearchChanged(string? value) => ApplyFilters();

    public async Task<Product?> LoadProductAsync(int id)
    {
        await using var db = new CatalogDbContext();
        var product = await ProductQuery(db).SingleOrDefaultAsync(entry => entry.Id == id);
        if (product is not null)
        {
            var soldProducts = await Pricing.ProductsOrderedLastMonthAsync(db, DateOnly.FromDateTime(DateTime.Today));
            product.HasDiscount = !soldProducts.Contains(id);
        }
        return product;
    }

    private bool CanLoadProducts() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanLoadProducts))]
    private async Task LoadProductsAsync()
    {
        IsLoading = true;
        Status = "Загрузка каталога…";
        try
        {
            await using var db = new CatalogDbContext();
            var catalog = await ProductQuery(db).OrderBy(product => product.Id).ToListAsync();
            var categories = await db.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync();
            var soldProducts = await Pricing.ProductsOrderedLastMonthAsync(db, DateOnly.FromDateTime(DateTime.Today));
            foreach (var product in catalog)
                product.HasDiscount = !soldProducts.Contains(product.Id);

            var categoryId = SelectedCategory?.Id;
            var filters = new List<CategoryFilter> { new(null, "Все категории") };
            filters.AddRange(categories.Select(category => new CategoryFilter(category.Id, category.Name)));
            _catalog = catalog;
            Categories = filters;
            SelectedCategory = filters.FirstOrDefault(category => category.Id == categoryId) ?? filters[0];
            Cart.Refresh(catalog);
            ApplyFilters();
        }
        catch (Exception error)
        {
            Status = DatabaseError.Message(error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static IQueryable<Product> ProductQuery(CatalogDbContext db) => db.Products.AsNoTracking()
        .Include(item => item.Category)
        .Include(item => item.Sizes).ThenInclude(item => item.Size)
        .AsSplitQuery();

    private void ApplyFilters()
    {
        IEnumerable<Product> result = _catalog;
        if (IsSignedIn)
        {
            if (SelectedCategory?.Id is int id)
                result = result.Where(item => item.CategoryId == id);
            if (Search?.Trim() is { Length: > 0 } search)
                result = result.Where(item => item.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    item.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase));
            result = SortIndex switch
            {
                1 => result.OrderBy(item => item.SalePrice),
                2 => result.OrderByDescending(item => item.SalePrice),
                _ => result
            };
        }
        Products = result.ToList();
        Status = Products.Count == 0 ? "Товары не найдены." : $"Показано моделей: {Products.Count} из {_catalog.Count}";
    }
}
