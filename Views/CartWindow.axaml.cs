using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DemoForMuhin.Models;
using DemoForMuhin.Presentation;
using DemoForMuhin.ViewModels;

namespace DemoForMuhin.Views;

public partial class CartWindow : Window
{
    private readonly MainViewModel _catalog = null!;
    private CartViewModel Cart => _catalog.Cart;
    private bool _busy;

    public CartWindow() => InitializeComponent();

    public CartWindow(MainViewModel catalog) : this()
    {
        _catalog = catalog;
        DataContext = Cart;
        CustomerName.Text = $"Клиент: {Cart.Customer?.FullName}";
        CustomerName.IsVisible = !Cart.IsStaffOrder;
        CustomerPicker.IsVisible = Cart.IsStaffOrder;
        CustomerPicker.SelectionChanged += (_, _) => Cart.Customer = CustomerPicker.SelectedItem as User;
        Cart.Lines.CollectionChanged += OnLinesChanged;
        UpdateControls();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (!Cart.IsStaffOrder)
            return;

        SetBusy(true);
        try
        {
            var selectedId = Cart.Customer?.Id;
            var customers = await _catalog.Orders.LoadCustomersAsync();
            CustomerPicker.ItemsSource = customers;
            CustomerPicker.SelectedItem = customers.FirstOrDefault(user => user.Id == selectedId);
        }
        catch (Exception error)
        {
            await MessageWindow.ErrorAsync(this, DatabaseError.Message(error));
        }
        finally
        {
            SetBusy(false);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Cart.Lines.CollectionChanged -= OnLinesChanged;
        base.OnClosed(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = _busy;
        base.OnClosing(e);
    }

    private void SelectLine(object? sender, SelectionChangedEventArgs e)
    {
        QuantityInput.Text = CartList.SelectedItem is CartLine line ? line.Quantity.ToString() : string.Empty;
        UpdateControls();
    }

    private async void ChangeQuantity(object? sender, RoutedEventArgs e)
    {
        if (CartList.SelectedItem is not CartLine line)
            return;
        if (!int.TryParse(QuantityInput.Text, out var quantity) || quantity <= 0)
        {
            await MessageWindow.ErrorAsync(this, "Введите целое количество пар больше нуля.");
            return;
        }
        try
        {
            var index = CartList.SelectedIndex;
            Cart.ChangeQuantity(line, quantity);
            CartList.SelectedIndex = index;
        }
        catch (InvalidOperationException error)
        {
            await MessageWindow.ErrorAsync(this, error.Message);
        }
    }

    private void RemoveLine(object? sender, RoutedEventArgs e)
    {
        if (CartList.SelectedItem is CartLine line)
            Cart.Remove(line);
    }

    private void GoBack(object? sender, RoutedEventArgs e) => Close(false);

    private async void ClearOrder(object? sender, RoutedEventArgs e)
    {
        if (await MessageWindow.ConfirmAsync(this, "Отменить заказ? Корзина будет очищена."))
        {
            Cart.Clear();
            Close(false);
        }
    }

    private async void ConfirmOrder(object? sender, RoutedEventArgs e)
    {
        if (Cart.Customer is not { } customer)
        {
            await MessageWindow.ErrorAsync(this, "Выберите клиента для заказа.");
            return;
        }

        SetBusy(true);
        int orderId;
        try
        {
            orderId = await _catalog.Orders.CreateAsync(customer.Id, Cart.Requests());
        }
        catch (Exception error)
        {
            await _catalog.LoadProductsCommand.ExecuteAsync(null);
            await MessageWindow.ErrorAsync(this, DatabaseError.Message(error));
            return;
        }
        finally
        {
            SetBusy(false);
        }

        Cart.Clear();
        await MessageWindow.InfoAsync(this, $"Заказ №{orderId} оформлен.");
        Close(true);
    }

    private void OnLinesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => UpdateControls();

    private void SetBusy(bool busy)
    {
        _busy = busy;
        IsEnabled = !busy;
        UpdateControls();
    }

    private void UpdateControls()
    {
        EmptyMessage.IsVisible = Cart.Lines.Count == 0;
        LineControls.IsEnabled = !_busy && CartList.SelectedItem is not null;
        ConfirmButton.IsEnabled = !_busy && Cart.Lines.Count > 0;
        ClearButton.IsEnabled = !_busy && (Cart.Lines.Count > 0 || Cart.IsStaffOrder);
    }
}
