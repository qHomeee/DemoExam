using System.ComponentModel.DataAnnotations.Schema;

namespace DemoForMuhin.Models;

public class ProductSize
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int SizeId { get; set; }
    public ShoeSize Size { get; set; } = null!;
    public int AvailableQuantity { get; set; }

    [NotMapped]
    public string AvailableLabel => $"Размер {Size.Value:0.#} · {AvailableQuantity} пар";
}
