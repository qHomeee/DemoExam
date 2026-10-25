using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DemoForMuhin.Models;

public class Product
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public string Subcategory { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageFile { get; set; }
    public decimal Price { get; set; }
    public List<ProductSize> Sizes { get; set; } = new();

    [NotMapped]
    public int TotalQuantity => Sizes.Sum(x => x.AvailableQuantity);

    [NotMapped]
    public string QuantityLabel => TotalQuantity > 5 ? "много" : "мало";
}
