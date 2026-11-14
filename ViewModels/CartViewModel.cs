using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DemoForMuhin.Models;
using DemoForMuhin.Services;

namespace DemoForMuhin.ViewModels;

public record CartLine(int ProductSizeId, string Name, string Manufacturer, decimal Size,
    int Available, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

public class CartViewModel : ObservableObject
{
    private readonly User? _currentUser;

    public CartViewModel(User? currentUser) => Customer = _currentUser = currentUser;

    public ObservableCollection<CartLine> Lines { get; } = new();
    public string Caption => Lines.Count == 0 ? "Корзина" : $"Корзина ({Lines.Count})";
    public decimal Total => Lines.Sum(line => line.Total);
    public bool IsStaffOrder { get; private set; }
    public User? Customer { get; set; }

    public void StartStaffOrder()
    {
        Clear();
        IsStaffOrder = true;
        Customer = null;
    }

    public void Add(Product product, ProductSize stock, int quantity)
    {
        var existing = Lines.FirstOrDefault(line => line.ProductSizeId == stock.Id);
        var totalQuantity = (existing?.Quantity ?? 0) + (long)quantity;
        if (totalQuantity > stock.AvailableQuantity)
            throw new InvalidOperationException($"На размер {stock.Size.Value:0.#} доступно {stock.AvailableQuantity} пар с учётом уже добавленных в корзину.");

        var line = new CartLine(stock.Id, product.Name, product.Manufacturer, stock.Size.Value,
            stock.AvailableQuantity, (int)totalQuantity, product.SalePrice);
        if (existing is null)
            Lines.Add(line);
        else
            Lines[Lines.IndexOf(existing)] = line;
        NotifyChanges();
    }

    public void ChangeQuantity(CartLine line, int quantity)
    {
        if (quantity > line.Available)
            throw new InvalidOperationException($"Доступно {line.Available} пар. Уменьшите количество или удалите позицию.");
        Lines[Lines.IndexOf(line)] = line with { Quantity = quantity };
        NotifyChanges();
    }

    public void Remove(CartLine line)
    {
        Lines.Remove(line);
        NotifyChanges();
    }

    public void Clear()
    {
        Lines.Clear();
        IsStaffOrder = false;
        Customer = _currentUser;
        NotifyChanges();
    }

    public List<OrderRequest> Requests() => Lines.Select(line => new OrderRequest(line.ProductSizeId, line.Quantity, line.UnitPrice)).ToList();

    public void Refresh(IReadOnlyList<Product> products)
    {
        var stock = products.SelectMany(product => product.Sizes.Select(size => (product, size)))
            .ToDictionary(pair => pair.size.Id);
        for (var index = 0; index < Lines.Count; index++)
        {
            var line = Lines[index];
            var updated = stock.TryGetValue(line.ProductSizeId, out var pair)
                ? line with { Available = pair.size.AvailableQuantity, UnitPrice = pair.product.SalePrice }
                : line with { Available = 0 };
            if (updated != line)
                Lines[index] = updated;
        }
        NotifyChanges();
    }

    private void NotifyChanges()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Caption));
    }
}
