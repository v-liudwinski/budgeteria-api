namespace Budgeteria.Api.Models;

public class Expense
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
}
