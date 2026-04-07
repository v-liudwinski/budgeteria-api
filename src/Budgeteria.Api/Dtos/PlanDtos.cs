namespace Budgeteria.Api.Dtos;

public record CurrencyDto(string Code, string Symbol, string Locale);

public record PlanCategoryDto(
    Guid Id, string Name, string Emoji, string Color,
    decimal MonthlyLimit, decimal Spent, bool IsEssential);

public record FinancialGoalDto(
    Guid Id, string Name, decimal TargetAmount, decimal CurrentAmount,
    string Deadline, string Priority, decimal MonthlyContribution);

public record FamilyMemberDto(
    Guid Id, string UserId, string Name, string Email,
    string? Avatar, string Role, DateTime JoinedAt);

public record FamilyPlanDto(
    Guid Id, string Name, CurrencyDto Currency, decimal MonthlyIncome,
    List<PlanCategoryDto> Categories, List<FinancialGoalDto> Goals,
    string CreatedBy, List<FamilyMemberDto> Members, DateTime CreatedAt);

public record CreatePlanRequest(
    string Name, CurrencyDto Currency, decimal MonthlyIncome,
    List<CreateCategoryRequest> Categories, List<CreateGoalRequest> Goals);

public record CreateCategoryRequest(
    string Name, string Emoji, string Color, decimal MonthlyLimit, bool IsEssential);

public record CreateGoalRequest(
    string Name, decimal TargetAmount, decimal CurrentAmount,
    string Deadline, string Priority, decimal MonthlyContribution);

public record UpdatePlanRequest(
    string? Name, CurrencyDto? Currency, decimal? MonthlyIncome,
    List<UpdateCategoryRequest>? Categories, List<UpdateGoalRequest>? Goals);

public record UpdateCategoryRequest(
    Guid? Id, string Name, string Emoji, string Color, decimal MonthlyLimit, bool IsEssential);

public record UpdateGoalRequest(
    Guid? Id, string Name, decimal TargetAmount, decimal CurrentAmount,
    string Deadline, string Priority, decimal MonthlyContribution);

/// <summary>Email is optional — omit to generate a link without sending an email.</summary>
public record InviteMemberRequest(string? Email);

public record AcceptInviteRequest(string Token);

public record InviteLinkResponse(Guid MemberId, string UserId, string InviteToken, string InviteUrl);

public record PlanAnalysisDto(
    decimal SavingsRate, List<string> RiskAreas, List<string> Suggestions,
    List<GoalFeasibilityDto> GoalFeasibility, decimal OverallScore);

public record GoalFeasibilityDto(Guid GoalId, bool Feasible, string? AdjustedTimeline);
