using System.Security.Claims;
using Budgeteria.Api.Data;
using Budgeteria.Api.Dtos;
using Budgeteria.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budgeteria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController(BudgeteriaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ExpenseDto>>> GetExpenses([FromQuery] Guid planId)
    {
        if (!await IsMember(planId)) return Forbid();

        var expenses = await db.Expenses
            .Where(e => e.PlanId == planId)
            .OrderByDescending(e => e.Date)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(expenses);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> AddExpense([FromQuery] Guid planId, CreateExpenseRequest request)
    {
        if (!await IsMember(planId)) return Forbid();

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Note = request.Note,
            Date = request.Date,
            MemberId = request.MemberId,
            MemberName = request.MemberName
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExpenses), ToDto(expense));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExpense([FromQuery] Guid planId, Guid id)
    {
        if (!await IsMember(planId)) return Forbid();

        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.PlanId == planId);
        if (expense is null) return NotFound();

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("by-category/{categoryId:guid}")]
    public async Task<ActionResult<List<ExpenseDto>>> GetByCategory([FromQuery] Guid planId, Guid categoryId)
    {
        if (!await IsMember(planId)) return Forbid();

        var expenses = await db.Expenses
            .Where(e => e.PlanId == planId && e.CategoryId == categoryId)
            .OrderByDescending(e => e.Date)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(expenses);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<PlanCategoryDto>>> GetCategories([FromQuery] Guid planId)
    {
        var userId = GetUserId();
        var plan = await db.Plans
            .Include(p => p.Categories)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == planId && p.Members.Any(m => m.UserId == userId));

        if (plan is null) return NotFound("No plan found");

        var expenses = await db.Expenses.Where(e => e.PlanId == plan.Id).ToListAsync();

        var categories = plan.Categories.Select(c => new PlanCategoryDto(
            c.Id, c.Name, c.Emoji, c.Color, c.MonthlyLimit,
            expenses.Where(e => e.CategoryId == c.Id).Sum(e => e.Amount),
            c.IsEssential)).ToList();

        return Ok(categories);
    }

    private async Task<bool> IsMember(Guid planId)
    {
        var userId = GetUserId();
        return await db.Plans.AnyAsync(p => p.Id == planId && p.Members.Any(m => m.UserId == userId));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static ExpenseDto ToDto(Expense e) =>
        new(e.Id, e.CategoryId, e.Amount, e.Note, e.Date, e.MemberId, e.MemberName);
}
