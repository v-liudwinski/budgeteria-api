using System.Security.Claims;
using Budgeteria.Api.Data;
using Budgeteria.Api.Dtos;
using Budgeteria.Api.Models;
using Budgeteria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Budgeteria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlansController(
    BudgeteriaDbContext db,
    Auth0UserService userService,
    IEmailService emailService,
    IConfiguration configuration) : ControllerBase
{
    private string FrontendUrl =>
        configuration["Email:FrontendUrl"] ?? "http://localhost:5173";

    // ── GET /api/plans ──────────────────────────────────────────────────────
    /// Returns ALL plans the authenticated user belongs to.
    [HttpGet]
    public async Task<ActionResult<List<FamilyPlanDto>>> GetPlans()
    {
        var userId = GetUserId();
        var plans = await db.Plans
            .Include(p => p.Categories)
            .Include(p => p.Goals)
            .Include(p => p.Members)
            .Where(p => p.Members.Any(m => m.UserId == userId))
            .ToListAsync();

        var planIds = plans.Select(p => p.Id).ToList();
        var expenses = await db.Expenses.Where(e => planIds.Contains(e.PlanId)).ToListAsync();

        return Ok(plans.Select(p => ToDto(p, expenses.Where(e => e.PlanId == p.Id).ToList())).ToList());
    }

    // ── POST /api/plans ─────────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<FamilyPlanDto>> CreatePlan(CreatePlanRequest request)
    {
        var user = await userService.GetOrCreateUser(User);

        var plan = new FamilyPlan
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CurrencyCode = request.Currency.Code,
            CurrencySymbol = request.Currency.Symbol,
            CurrencyLocale = request.Currency.Locale,
            MonthlyIncome = request.MonthlyIncome,
            CreatedBy = user.Id,
            Categories = request.Categories.Select(c => new PlanCategory
            {
                Id = Guid.NewGuid(),
                Name = c.Name,
                Emoji = c.Emoji,
                Color = c.Color,
                MonthlyLimit = c.MonthlyLimit,
                IsEssential = c.IsEssential
            }).ToList(),
            Goals = request.Goals.Select(g => new FinancialGoal
            {
                Id = Guid.NewGuid(),
                Name = g.Name,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                Deadline = g.Deadline,
                Priority = g.Priority,
                MonthlyContribution = g.MonthlyContribution
            }).ToList(),
            Members =
            [
                new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Avatar = user.Avatar,
                    Role = "admin"
                }
            ]
        };

        db.Plans.Add(plan);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlans), ToDto(plan, []));
    }

    // ── PUT /api/plans/{planId} ─────────────────────────────────────────────
    [HttpPut("{planId:guid}")]
    public async Task<ActionResult<FamilyPlanDto>> UpdatePlan(Guid planId, UpdatePlanRequest request)
    {
        var plan = await GetMemberPlan(planId);
        if (plan is null) return NotFound();

        if (request.Name is not null) plan.Name = request.Name;
        if (request.Currency is not null)
        {
            plan.CurrencyCode = request.Currency.Code;
            plan.CurrencySymbol = request.Currency.Symbol;
            plan.CurrencyLocale = request.Currency.Locale;
        }
        if (request.MonthlyIncome.HasValue) plan.MonthlyIncome = request.MonthlyIncome.Value;

        if (request.Categories is not null)
        {
            db.Categories.RemoveRange(plan.Categories);
            plan.Categories = request.Categories.Select(c => new PlanCategory
            {
                Id = c.Id ?? Guid.NewGuid(),
                PlanId = plan.Id,
                Name = c.Name,
                Emoji = c.Emoji,
                Color = c.Color,
                MonthlyLimit = c.MonthlyLimit,
                IsEssential = c.IsEssential
            }).ToList();
        }

        if (request.Goals is not null)
        {
            db.Goals.RemoveRange(plan.Goals);
            plan.Goals = request.Goals.Select(g => new FinancialGoal
            {
                Id = g.Id ?? Guid.NewGuid(),
                PlanId = plan.Id,
                Name = g.Name,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                Deadline = g.Deadline,
                Priority = g.Priority,
                MonthlyContribution = g.MonthlyContribution
            }).ToList();
        }

        await db.SaveChangesAsync();

        var expenses = await db.Expenses.Where(e => e.PlanId == plan.Id).ToListAsync();
        return Ok(ToDto(plan, expenses));
    }

    // ── POST /api/plans/{planId}/analyze ────────────────────────────────────
    [HttpPost("{planId:guid}/analyze")]
    public async Task<ActionResult<PlanAnalysisDto>> AnalyzePlan(Guid planId)
    {
        var plan = await GetMemberPlan(planId);
        if (plan is null) return NotFound();

        var expenses = await db.Expenses.Where(e => e.PlanId == plan.Id).ToListAsync();
        return Ok(PlanAnalysisService.Analyze(plan, expenses));
    }

    // ── POST /api/plans/{planId}/members ────────────────────────────────────
    /// <summary>
    /// Creates a pending invite slot and returns the invite link.
    /// If email is provided, also sends the invite email.
    /// Name is NOT taken from the caller — the invitee sets their own name after login.
    /// </summary>
    [HttpPost("{planId:guid}/members")]
    public async Task<ActionResult<InviteLinkResponse>> InviteMember(Guid planId, InviteMemberRequest request)
    {
        var plan = await GetMemberPlan(planId);
        if (plan is null) return NotFound();

        // Enforce 7-member limit (active members only, not pending)
        var activeCount = plan.Members.Count(m => !m.UserId.StartsWith("pending|"));
        if (activeCount >= 7)
            return BadRequest(new { error = "This plan has reached the maximum of 7 members." });

        var inviter = await userService.GetOrCreateUser(User);

        var inviteToken = Guid.NewGuid().ToString("N");
        var inviteUrl = $"{FrontendUrl}/invite/accept?token={inviteToken}";

        var member = new FamilyMember
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            UserId = $"pending|{Guid.NewGuid()}",
            Name = string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Role = "member",
            InviteToken = inviteToken,
            InviteTokenExpiry = DateTime.UtcNow.AddDays(7)
        };

        db.Members.Add(member);
        await db.SaveChangesAsync();

        // Only send email if an address was provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                var inviterName = inviter.Name.Length > 0 ? inviter.Name : inviter.Email;
                await emailService.SendInviteAsync(
                    request.Email.Trim(),
                    string.Empty, // recipient name unknown until they sign up
                    inviterName,
                    plan.Name,
                    inviteToken);
            }
            catch (Exception ex)
            {
                Response.Headers.Append("X-Email-Warning", ex.Message);
            }
        }

        return Ok(new InviteLinkResponse(member.Id, member.UserId, inviteToken, inviteUrl));
    }

    // ── POST /api/plans/members/accept ──────────────────────────────────────
    /// <summary>Accept an invitation. The authenticated user's Auth0 sub replaces the pending UserId.</summary>
    [HttpPost("members/accept")]
    public async Task<ActionResult<FamilyPlanDto>> AcceptInvitation([FromBody] AcceptInviteRequest request)
    {
        var member = await db.Members
            .Include(m => m.Plan)
            .FirstOrDefaultAsync(m => m.InviteToken == request.Token);

        if (member is null)
            return NotFound(new { error = "Invalid or expired invitation link." });

        if (member.InviteTokenExpiry.HasValue && member.InviteTokenExpiry.Value < DateTime.UtcNow)
            return BadRequest(new { error = "This invitation has expired. Ask the plan admin to send a new one." });

        var user = await userService.GetOrCreateUser(User);

        // Check if this user is already an active member of THIS specific plan
        // (exclude the current pending invite slot itself)
        var existingActive = await db.Members
            .FirstOrDefaultAsync(m => m.PlanId == member.PlanId && m.UserId == user.Id && m.Id != member.Id);

        if (existingActive is not null)
        {
            // Already an active member — clean up the pending invite slot and return success
            db.Members.Remove(member);
            await db.SaveChangesAsync();
        }
        else
        {
            // Promote the pending invite to an active membership
            member.UserId = user.Id;
            member.Name = user.Name.Length > 0 ? user.Name : string.Empty;
            member.Email = user.Email.Length > 0 ? user.Email : member.Email;
            member.Avatar = user.Avatar ?? member.Avatar;
            member.InviteToken = null;
            member.InviteTokenExpiry = null;
            member.JoinedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        var plan = await db.Plans
            .Include(p => p.Categories)
            .Include(p => p.Goals)
            .Include(p => p.Members)
            .FirstAsync(p => p.Id == member.PlanId);

        var expenses = await db.Expenses.Where(e => e.PlanId == plan.Id).ToListAsync();
        return Ok(ToDto(plan, expenses));
    }

    // ── DELETE /api/plans/{planId}/members/{memberId} ───────────────────────
    [HttpDelete("{planId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid planId, Guid memberId)
    {
        var plan = await GetMemberPlan(planId);
        if (plan is null) return NotFound();

        var member = plan.Members.FirstOrDefault(m => m.Id == memberId);
        if (member is null) return NotFound();

        plan.Members.Remove(member);
        await db.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// Returns the plan if the current user is a member, otherwise null.
    // ── DELETE /api/plans/{planId} ──────────────────────────────────────────
    /// <summary>Admin-only: permanently deletes a plan and all its data.</summary>
    [HttpDelete("{planId:guid}")]
    public async Task<IActionResult> DeletePlan(Guid planId)
    {
        var userId = GetUserId();
        var plan = await db.Plans
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == planId && p.Members.Any(m => m.UserId == userId && m.Role == "admin"));

        if (plan is null) return NotFound();

        // Expenses have no FK cascade — delete them explicitly
        var expenses = db.Expenses.Where(e => e.PlanId == planId);
        db.Expenses.RemoveRange(expenses);

        db.Plans.Remove(plan); // cascades: Categories, Goals, Members
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<FamilyPlan?> GetMemberPlan(Guid planId)
    {
        var userId = GetUserId();
        return await db.Plans
            .Include(p => p.Categories)
            .Include(p => p.Goals)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == planId && p.Members.Any(m => m.UserId == userId));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static FamilyPlanDto ToDto(FamilyPlan plan, List<Expense> expenses) => new(
        plan.Id,
        plan.Name,
        new CurrencyDto(plan.CurrencyCode, plan.CurrencySymbol, plan.CurrencyLocale),
        plan.MonthlyIncome,
        plan.Categories.Select(c => new PlanCategoryDto(
            c.Id, c.Name, c.Emoji, c.Color, c.MonthlyLimit,
            expenses.Where(e => e.CategoryId == c.Id).Sum(e => e.Amount),
            c.IsEssential)).ToList(),
        plan.Goals.Select(g => new FinancialGoalDto(
            g.Id, g.Name, g.TargetAmount, g.CurrentAmount,
            g.Deadline, g.Priority, g.MonthlyContribution)).ToList(),
        plan.CreatedBy,
        plan.Members.Select(m => new FamilyMemberDto(
            m.Id, m.UserId, m.Name, m.Email, m.Avatar, m.Role, m.JoinedAt)).ToList(),
        plan.CreatedAt);
}
