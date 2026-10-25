using System.Threading.Tasks;
using DemoForMuhin.Data;
using DemoForMuhin.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoForMuhin.Services;

public class AuthService
{
    public User? CurrentUser { get; private set; }

    public async Task<bool> LoginAsync(string login)
    {
        CurrentUser = null;
        if (string.IsNullOrWhiteSpace(login))
            return false;

        var normalizedLogin = login.Trim();
        await using var db = new CatalogDbContext();
        CurrentUser = await db.Users.AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Login == normalizedLogin);
        return CurrentUser is not null;
    }

    public void Logout() => CurrentUser = null;
}
