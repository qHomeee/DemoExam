using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DemoForMuhin.Models;
using DemoForMuhin.Presentation;
using DemoForMuhin.ViewModels;

namespace DemoForMuhin.Views;

public partial class OrdersWindow : Window
{
    private readonly MainViewModel _catalog = null!;
    private bool _busy;

    public OrdersWindow() => InitializeComponent();

    public OrdersWindow(MainViewModel catalog) : this()
    {
        _catalog = catalog;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        await ReloadAsync();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = _busy;
        base.OnClosing(e);
    }

    private async Task ReloadAsync()
    {
        SetBusy(true);
        try
        {
            var selectedId = (OrderList.SelectedItem as Order)?.Id;
            var orders = await _catalog.Orders.LoadOrdersAsync();
            OrderList.ItemsSource = orders;
            OrderList.SelectedItem = orders.FirstOrDefault(order => order.Id == selectedId);
            StatusText.Text = orders.Count == 0 ? "Заказов пока нет." : $"Заказов: {orders.Count}";
        }
        catch (Exception error)
        {
            OrderList.ItemsSource = null;
            StatusText.Text = DatabaseError.Message(error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OpenOrder(object? sender, RoutedEventArgs e) => await ShowOrderAsync();

    private async void OpenFromList(object? sender, TappedEventArgs e) => await ShowOrderAsync();

    private async Task ShowOrderAsync()
    {
        if (_busy || OrderList.SelectedItem is not Order order)
            return;

        if (await new OrderDetailsWindow(_catalog, order).ShowDialog<bool>(this))
            await ReloadAsync();
    }

    private async void DeleteOrder(object? sender, RoutedEventArgs e)
    {
        if (OrderList.SelectedItem is not Order order || !await MessageWindow.ConfirmAsync(this,
                $"Удалить заказ №{order.Id}? Заказ будет удалён, пары вернутся в остаток."))
            return;
        SetBusy(true);
        try
        {
            await _catalog.Orders.DeleteAsync(order.Id);
        }
        catch (Exception error)
        {
            await MessageWindow.ErrorAsync(this, DatabaseError.Message(error));
        }
        finally
        {
            SetBusy(false);
        }
        await ReloadAsync();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        OrderList.IsEnabled = !busy;
        CreateButton.IsEnabled = !busy;
        UpdateSelection();
    }

    private void SelectOrder(object? sender, SelectionChangedEventArgs e) => UpdateSelection();

    private void UpdateSelection() =>
        DeleteButton.IsEnabled = DetailsButton.IsEnabled = !_busy && OrderList.SelectedItem is not null;

    private void CreateOrder(object? sender, RoutedEventArgs e) => Close(true);
    private void GoBack(object? sender, RoutedEventArgs e) => Close(false);
}
