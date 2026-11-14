using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DemoForMuhin.Models;
using DemoForMuhin.Presentation;
using DemoForMuhin.ViewModels;

namespace DemoForMuhin.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext!;
    private bool _openingProduct;

    public MainWindow() => InitializeComponent();

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await ViewModel.LoadProductsCommand.ExecuteAsync(null);
    }

    private async void OpenProduct(object? sender, RoutedEventArgs e)
    {
        if (_openingProduct || sender is not Button { DataContext: Product product })
            return;
        _openingProduct = true;
        try
        {
            var current = await ViewModel.LoadProductAsync(product.Id);
            if (!IsVisible)
                return;
            if (current is null)
                await MessageWindow.ErrorAsync(this, "Модель больше недоступна. Выберите другой товар.");
            else
                await new ProductWindow(current, ViewModel.IsSignedIn, ViewModel.Cart).ShowDialog(this);
        }
        catch (Exception error)
        {
            if (IsVisible)
                await MessageWindow.ErrorAsync(this, DatabaseError.Message(error));
        }
        finally
        {
            _openingProduct = false;
        }
    }

    private async void OpenCart(object? sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsSignedIn)
            return;
        var staffOrder = ViewModel.Cart.IsStaffOrder;
        if (!await new CartWindow(ViewModel).ShowDialog<bool>(this))
            return;
        await ViewModel.LoadProductsCommand.ExecuteAsync(null);
        if (staffOrder)
            await ShowOrdersAsync();
    }

    private async void OpenOrders(object? sender, RoutedEventArgs e) => await ShowOrdersAsync();

    private async Task ShowOrdersAsync()
    {
        if (!ViewModel.CanManageOrders)
            return;
        var create = await new OrdersWindow(ViewModel).ShowDialog<bool>(this);
        await ViewModel.LoadProductsCommand.ExecuteAsync(null);
        if (!create)
            return;
        if (ViewModel.Cart.Lines.Count > 0 && !await MessageWindow.ConfirmAsync(this,
                "Начать новый заказ? Текущая корзина будет очищена."))
            return;
        ViewModel.Cart.StartStaffOrder();
        ViewModel.Status = "Новый заказ. Выберите товары в каталоге, затем откройте корзину и укажите клиента.";
    }

    private async void SignOut(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.Cart.Lines.Count > 0 && !await MessageWindow.ConfirmAsync(this,
                "Выйти? Неоформленный заказ будет очищен."))
            return;
        ViewModel.SignOutCommand.Execute(null);
    }
}
