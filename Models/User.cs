using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DemoForMuhin.Models;

public class User
{
    public int Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string Login { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    [NotMapped]
    public string FullName => string.Join(" ", new[] { LastName, FirstName, Patronymic }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}
