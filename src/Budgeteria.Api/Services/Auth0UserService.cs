using System.Security.Claims;
using Budgeteria.Api.Data;
using Budgeteria.Api.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Budgeteria.Api.Services;

public class Auth0UserService(BudgeteriaDbContext db)
{
    public async Task<AppUser> GetOrCreateUser(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("No sub claim found");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == sub);
        if (user is not null) return user;

        user = new AppUser
        {
            Id = sub,
            Name = principal.FindFirstValue("name")
                ?? principal.FindFirstValue(ClaimTypes.Name)
                ?? "",
            Email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? "",
            Avatar = principal.FindFirstValue("picture")
        };

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Race condition: two concurrent requests for the same new user — retry lookup by sub
            db.ChangeTracker.Clear();
            user = await db.Users.FirstOrDefaultAsync(u => u.Id == sub);
            if (user is null) throw;
        }

        return user;
    }
}
