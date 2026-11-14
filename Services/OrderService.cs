using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DemoForMuhin.Data;
using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DemoForMuhin.Services;

public record OrderRequest(int ProductSizeId, int Quantity, decimal UnitPrice);

public class OrderService(AuthService auth)
{
    public async Task<List<User>> LoadCustomersAsync()
    {
        RequireStaff();
        await using var db = new CatalogDbContext();
        return await db.Users.AsNoTracking().OrderBy(user => user.LastName).ThenBy(user => user.FirstName).ToListAsync();
    }

    public async Task<List<Order>> LoadOrdersAsync()
    {
        RequireStaff();
        await using var db = new CatalogDbContext();
        return await WithDetails(db).AsNoTracking().AsSplitQuery()
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Id)
            .ToListAsync();
    }

    public async Task<int> CreateAsync(int customerId, IReadOnlyList<OrderRequest> requests)
    {
        var currentUser = auth.CurrentUser ?? throw new InvalidOperationException("Войдите, чтобы оформить заказ.");
        if (customerId != currentUser.Id && !auth.CanManageOrders)
            throw new InvalidOperationException("Оформление заказа от имени другого клиента доступно сотрудникам.");
        if (requests.Count == 0 || requests.Any(item => item.Quantity <= 0) ||
            requests.Select(item => item.ProductSizeId).Distinct().Count() != requests.Count)
            throw new InvalidOperationException("Добавьте товарные позиции с целым положительным количеством пар.");

        await using var db = new CatalogDbContext();
        await using var transaction = await BeginOrderChangeAsync(db);
        if (!await db.Users.AnyAsync(user => user.Id == customerId))
            throw new InvalidOperationException("Клиент не найден. Выберите клиента заново.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var soldProducts = await Pricing.ProductsOrderedLastMonthAsync(db, today);
        var requestedIds = requests.Select(item => item.ProductSizeId).ToArray();
        var stock = await db.ProductSizes.Include(item => item.Product).Include(item => item.Size)
            .Where(item => requestedIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id);

        var order = new Order { CustomerId = customerId, OrderDate = today };
        foreach (var request in requests)
        {
            if (!stock.TryGetValue(request.ProductSizeId, out var entry))
                throw new InvalidOperationException("Один из размеров больше недоступен. Обновите заказ.");
            if (entry.AvailableQuantity < request.Quantity)
                throw new InvalidOperationException(
                    $"«{entry.Product.Name}», размер {entry.Size.Value:0.#}: доступно {entry.AvailableQuantity} пар. Уменьшите количество или удалите позицию.");
            var price = Pricing.SalePrice(entry.Product.Price, !soldProducts.Contains(entry.ProductId));
            if (price != request.UnitPrice)
                throw new InvalidOperationException("Цена изменилась. Проверьте новую сумму и подтвердите заказ ещё раз.");

            entry.AvailableQuantity -= request.Quantity;
            order.Items.Add(new OrderItem { ProductSize = entry, Quantity = request.Quantity, UnitPrice = price });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return order.Id;
    }

    public async Task EditAsync(int orderId, DateOnly date, IReadOnlySet<int> removedSizes)
    {
        if (!auth.CanEditOrders)
            throw new InvalidOperationException("Редактирование заказа доступно администратору.");
        if (date == default)
            throw new InvalidOperationException("Укажите дату заказа.");

        await using var db = new CatalogDbContext();
        await using var transaction = await BeginOrderChangeAsync(db);
        var order = await FindOrderAsync(db, orderId);
        var removed = order.Items.Where(item => removedSizes.Contains(item.ProductSizeId)).ToList();
        if (removed.Count != removedSizes.Count)
            throw new InvalidOperationException("Состав заказа изменился. Откройте заказ повторно.");
        if (removed.Count == order.Items.Count)
            throw new InvalidOperationException("В заказе должна остаться позиция. Для отмены удалите весь заказ.");

        order.OrderDate = date;
        ReturnToStock(removed);
        db.OrderItems.RemoveRange(removed);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeleteAsync(int orderId)
    {
        RequireStaff();
        await using var db = new CatalogDbContext();
        await using var transaction = await BeginOrderChangeAsync(db);
        var order = await FindOrderAsync(db, orderId);
        ReturnToStock(order.Items);
        db.Orders.Remove(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private void RequireStaff()
    {
        if (!auth.CanManageOrders)
            throw new InvalidOperationException("Список заказов доступен менеджеру и администратору.");
    }

    private static IQueryable<Order> WithDetails(CatalogDbContext db) => db.Orders
        .Include(order => order.Customer)
        .Include(order => order.Items).ThenInclude(item => item.ProductSize).ThenInclude(stock => stock.Product)
        .Include(order => order.Items).ThenInclude(item => item.ProductSize).ThenInclude(stock => stock.Size);

    private static async Task<Order> FindOrderAsync(CatalogDbContext db, int id) =>
        await db.Orders.Include(order => order.Items).ThenInclude(item => item.ProductSize)
            .SingleOrDefaultAsync(order => order.Id == id)
            ?? throw new InvalidOperationException("Заказ уже удалён. Обновите список заказов.");

    private static void ReturnToStock(IEnumerable<OrderItem> items)
    {
        foreach (var item in items)
            item.ProductSize.AvailableQuantity += item.Quantity;
    }

    private static async Task<IDbContextTransaction> BeginOrderChangeAsync(CatalogDbContext db)
    {
        var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // Сериализуем изменения заказов, чтобы два покупателя не списали один и тот же остаток.
            await db.Database.ExecuteSqlRawAsync("LOCK TABLE catalog.orders IN SHARE ROW EXCLUSIVE MODE");
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
