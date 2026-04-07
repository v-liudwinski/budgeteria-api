namespace Budgeteria.Api.Models;

public class PlanCategory
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal MonthlyLimit { get; set; }
    public bool IsEssential { get; set; }

    public FamilyPlan Plan { get; set; } = null!;
}
