using System.ComponentModel.DataAnnotations.Schema;

namespace DemoForMuhin.Models;

public class OrderItem
{
    public int OrderId { get; set; }
    public int ProductSizeId { get; set; }
    public ProductSize ProductSize { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal Total => Quantity * UnitPrice;
}
