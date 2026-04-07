namespace Budgeteria.Api.Models;

public class FamilyPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public string CurrencySymbol { get; set; } = "$";
    public string CurrencyLocale { get; set; } = "en-US";
    public decimal MonthlyIncome { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<PlanCategory> Categories { get; set; } = [];
    public List<FinancialGoal> Goals { get; set; } = [];
    public List<FamilyMember> Members { get; set; } = [];
}
