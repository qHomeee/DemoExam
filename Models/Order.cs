using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DemoForMuhin.Models;

public class Order
{
    public int Id { get; set; }
    public DateOnly OrderDate { get; set; }
    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();

    [NotMapped]
    public decimal Total => Items.Sum(item => item.Total);
}
