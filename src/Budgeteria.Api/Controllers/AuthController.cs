using Budgeteria.Api.Dtos;
using Budgeteria.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Budgeteria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController(Auth0UserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var user = await userService.GetOrCreateUser(User);
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Avatar));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        UpdateProfileRequest request,
        [FromServices] Data.BudgeteriaDbContext db)
    {
        var user = await userService.GetOrCreateUser(User);

        if (request.Name is not null) user.Name = request.Name;
        if (request.Avatar is not null) user.Avatar = request.Avatar;

        // Sync name/avatar to all FamilyMember slots for this user so Family views stay current.
        var members = await db.Members
            .Where(m => m.UserId == user.Id)
            .ToListAsync();
        foreach (var m in members)
        {
            if (request.Name is not null) m.Name = request.Name;
            if (request.Avatar is not null) m.Avatar = request.Avatar;
        }

        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Avatar));
    }
}
