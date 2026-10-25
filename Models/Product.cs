using System.Collections.Generic;

namespace DemoForMuhin.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageFile { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<ProductSize> Sizes { get; set; } = new();
    public List<OrderLine> OrderLines { get; set; } = new();
}
