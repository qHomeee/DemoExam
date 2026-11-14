using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DemoForMuhin.Models;
using DemoForMuhin.Presentation;
using DemoForMuhin.ViewModels;

namespace DemoForMuhin.Views;

public partial class OrderDetailsWindow : Window
{
    private readonly MainViewModel _catalog = null!;
    private readonly int _orderId;
    private readonly ObservableCollection<OrderItem> _items = null!;
    private readonly HashSet<int> _removedSizes = new();
    private bool _busy;

    public OrderDetailsWindow() => InitializeComponent();

    public OrderDetailsWindow(MainViewModel catalog, Order order) : this()
    {
        _catalog = catalog;
        _orderId = order.Id;
        _items = new(order.Items.OrderBy(item => item.ProductSize.Product.Name).ThenBy(item => item.ProductSize.Size.Value));
        DataContext = order;
        ItemList.ItemsSource = _items;
        EditableDate.IsVisible = SaveButton.IsVisible = RemoveButton.IsVisible = catalog.CanEditOrders;
        ReadOnlyDate.IsVisible = !catalog.CanEditOrders;
        OrderDateInput.SelectedDate = order.OrderDate.ToDateTime(TimeOnly.MinValue);
        UpdateTotal();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = _busy;
        base.OnClosing(e);
    }

    private void GoBack(object? sender, RoutedEventArgs e) => Close(false);

    private void SelectItem(object? sender, SelectionChangedEventArgs e) =>
        RemoveButton.IsEnabled = !_busy && ItemList.SelectedItem is not null;

    private async void RemoveItem(object? sender, RoutedEventArgs e)
    {
        if (ItemList.SelectedItem is not OrderItem item)
            return;
        if (_items.Count == 1)
        {
            await MessageWindow.ErrorAsync(this, "В заказе должна остаться позиция. Для отмены удалите заказ целиком из списка.");
            return;
        }
        _removedSizes.Add(item.ProductSizeId);
        _items.Remove(item);
        UpdateTotal();
    }

    private void UpdateTotal() => TotalText.Text = $"Итого: {_items.Sum(item => item.Total):N2} ₽";

    private async void Save(object? sender, RoutedEventArgs e)
    {
        if (OrderDateInput.SelectedDate is not { } date)
        {
            await MessageWindow.ErrorAsync(this, "Укажите дату заказа.");
            return;
        }
        if (_removedSizes.Count > 0 && !await MessageWindow.ConfirmAsync(this,
                "Сохранить изменения? Убранные позиции будут удалены из заказа, пары вернутся в остаток."))
            return;

        _busy = true;
        IsEnabled = false;
        try
        {
            await _catalog.Orders.EditAsync(_orderId, DateOnly.FromDateTime(date), _removedSizes);
        }
        catch (Exception error)
        {
            await MessageWindow.ErrorAsync(this, DatabaseError.Message(error));
            return;
        }
        finally
        {
            _busy = false;
            IsEnabled = true;
        }
        Close(true);
    }
}
