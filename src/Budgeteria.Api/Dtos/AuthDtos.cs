namespace Budgeteria.Api.Dtos;

public record UpdateProfileRequest(string? Name, string? Avatar);
public record UserDto(string Id, string Name, string Email, string? Avatar);
