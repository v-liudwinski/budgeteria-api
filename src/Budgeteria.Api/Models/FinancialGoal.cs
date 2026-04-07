namespace Budgeteria.Api.Models;

public class FinancialGoal
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string Deadline { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium"; // short, medium, long
    public decimal MonthlyContribution { get; set; }

    public FamilyPlan Plan { get; set; } = null!;
}
