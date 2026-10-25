using System;
using System.Collections.Generic;

namespace DemoForMuhin.Models;

public class Order
{
    public int Id { get; set; }
    public DateOnly OrderedOn { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderLine> Lines { get; set; } = new();
}
