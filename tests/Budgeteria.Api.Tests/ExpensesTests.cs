using System.Net;
using System.Net.Http.Json;
using Budgeteria.Api.Dtos;
using FluentAssertions;

namespace Budgeteria.Api.Tests;

public class ExpensesTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public ExpensesTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetExpenses_Empty_ReturnsEmptyList()
    {
        var (client, _, _) = await SetupPlan();

        var response = await client.GetAsync("/api/expenses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseDto>>();
        expenses.Should().BeEmpty();
    }

    [Fact]
    public async Task AddExpense_ReturnsCreated()
    {
        var (client, categoryId, _) = await SetupPlan();

        var request = new CreateExpenseRequest(categoryId, 42.50m, "Lunch", "2026-03-31", Guid.NewGuid(), "Test");
        var response = await client.PostAsJsonAsync("/api/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseDto>();
        expense!.Amount.Should().Be(42.50m);
        expense.Note.Should().Be("Lunch");
    }

    [Fact]
    public async Task DeleteExpense_ReturnsNoContent()
    {
        var (client, categoryId, _) = await SetupPlan();

        var addResponse = await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseRequest(categoryId, 10, "Coffee", "2026-03-31", Guid.NewGuid(), "Test"));
        var expense = (await addResponse.Content.ReadFromJsonAsync<ExpenseDto>())!;

        var response = await client.DeleteAsync($"/api/expenses/{expense.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var allResponse = await client.GetAsync("/api/expenses");
        var all = await allResponse.Content.ReadFromJsonAsync<List<ExpenseDto>>();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByCategory_FiltersCorrectly()
    {
        var (client, categoryId, _) = await SetupPlan();

        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseRequest(categoryId, 20, "A", "2026-03-31", Guid.NewGuid(), "Test"));
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseRequest(Guid.NewGuid(), 30, "B", "2026-03-31", Guid.NewGuid(), "Test"));

        var response = await client.GetAsync($"/api/expenses/by-category/{categoryId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseDto>>();
        expenses.Should().HaveCount(1);
        expenses![0].Note.Should().Be("A");
    }

    [Fact]
    public async Task GetCategories_ReturnsWithSpent()
    {
        var (client, categoryId, _) = await SetupPlan();
        await client.PostAsJsonAsync("/api/expenses",
            new CreateExpenseRequest(categoryId, 75, "Grocery", "2026-03-31", Guid.NewGuid(), "Test"));

        var response = await client.GetAsync("/api/expenses/categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<PlanCategoryDto>>();
        categories.Should().HaveCount(1);
        categories![0].Spent.Should().Be(75);
    }

    private async Task<(HttpClient Client, Guid CategoryId, FamilyPlanDto Plan)> SetupPlan()
    {
        var sub = $"auth0|{Guid.NewGuid()}";
        var client = _factory.CreateAuthenticatedClient(sub);
        await client.GetAsync("/api/auth/me"); // auto-create user

        var createRequest = new CreatePlanRequest(
            "Budget", new CurrencyDto("USD", "$", "en-US"), 5000,
            [new CreateCategoryRequest("Food", "\U0001f355", "#ff0000", 500, true)],
            []);
        var planResponse = await client.PostAsJsonAsync("/api/plans", createRequest);
        var plan = (await planResponse.Content.ReadFromJsonAsync<FamilyPlanDto>())!;
        return (client, plan.Categories[0].Id, plan);
    }
}
