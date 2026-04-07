namespace Budgeteria.Api.Models;

public class FamilyMember
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string Role { get; set; } = "member"; // admin, member
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Invitation flow
    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpiry { get; set; }

    public FamilyPlan Plan { get; set; } = null!;
}
