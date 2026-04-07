using Budgeteria.Api.Dtos;
using Budgeteria.Api.Models;

namespace Budgeteria.Api.Services;

public static class PlanAnalysisService
{
    public static PlanAnalysisDto Analyze(FamilyPlan plan, List<Expense> expenses)
    {
        var totalLimits = plan.Categories.Sum(c => c.MonthlyLimit);
        var savingsRate = plan.MonthlyIncome > 0
            ? Math.Round((plan.MonthlyIncome - totalLimits) / plan.MonthlyIncome * 100, 1)
            : 0;

        var riskAreas = new List<string>();
        foreach (var cat in plan.Categories)
        {
            var spent = expenses.Where(e => e.CategoryId == cat.Id).Sum(e => e.Amount);
            if (spent > cat.MonthlyLimit)
                riskAreas.Add($"{cat.Name} is over budget by {spent - cat.MonthlyLimit:F2}");
        }

        var suggestions = new List<string>();
        if (savingsRate < 20)
            suggestions.Add("Consider increasing your savings rate to at least 20%");
        if (!plan.Categories.Any(c => c.IsEssential))
            suggestions.Add("Mark essential categories to prioritize spending");
        if (plan.Goals.Count == 0)
            suggestions.Add("Set financial goals to stay motivated");

        var goalFeasibility = plan.Goals.Select(g => new GoalFeasibilityDto(
            g.Id,
            g.MonthlyContribution > 0 && g.TargetAmount > 0,
            g.MonthlyContribution > 0
                ? null
                : "Increase monthly contribution to reach this goal"
        )).ToList();

        var overallScore = Math.Min(100, Math.Max(0,
            50 + savingsRate * 0.5m + (plan.Goals.Count > 0 ? 10 : 0) - riskAreas.Count * 10));

        return new PlanAnalysisDto(savingsRate, riskAreas, suggestions, goalFeasibility, Math.Round(overallScore, 1));
    }
}
