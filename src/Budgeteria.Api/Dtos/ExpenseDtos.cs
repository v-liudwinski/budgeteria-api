namespace Budgeteria.Api.Dtos;

public record ExpenseDto(
    Guid Id, Guid CategoryId, decimal Amount,
    string Note, string Date, Guid MemberId, string MemberName);

public record CreateExpenseRequest(
    Guid CategoryId, decimal Amount, string Note,
    string Date, Guid MemberId, string MemberName);
