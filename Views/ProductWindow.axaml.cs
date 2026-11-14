using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DemoForMuhin.Models;
using DemoForMuhin.ViewModels;

namespace DemoForMuhin.Views;

public partial class ProductWindow : Window
{
    private readonly Product _product = null!;
    private readonly CartViewModel _cart = null!;

    public ProductWindow() => InitializeComponent();

    public ProductWindow(Product product, bool canOrder, CartViewModel cart) : this()
    {
        DataContext = _product = product;
        _cart = cart;
        var sizes = product.Sizes.Where(size => size.AvailableQuantity > 0).OrderBy(size => size.Size.Value).ToList();
        SizePicker.ItemsSource = sizes;
        SizePicker.SelectedIndex = sizes.Count == 1 ? 0 : -1;
        SizePicker.IsVisible = sizes.Count > 0;
        Unavailable.IsVisible = sizes.Count == 0;
        PurchaseControls.IsVisible = canOrder && sizes.Count > 0;
    }

    private void GoBack(object? sender, RoutedEventArgs e) => Close();

    private async void AddToCart(object? sender, RoutedEventArgs e)
    {
        if (SizePicker.SelectedItem is not ProductSize size || !int.TryParse(QuantityInput.Text, out var quantity) || quantity <= 0)
        {
            await MessageWindow.ErrorAsync(this, "Выберите доступный размер и введите целое количество пар больше нуля.");
            return;
        }

        try
        {
            _cart.Add(_product, size, quantity);
            Close();
        }
        catch (InvalidOperationException error)
        {
            await MessageWindow.ErrorAsync(this, error.Message);
        }
    }
}
