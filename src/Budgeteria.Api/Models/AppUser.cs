namespace Budgeteria.Api.Models;

public class AppUser
{
    public string Id { get; set; } = string.Empty; // Auth0 sub claim (e.g. "auth0|abc123")
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
