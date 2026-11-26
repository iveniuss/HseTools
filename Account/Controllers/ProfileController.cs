using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Account.DTOs;
using Account.Models;

namespace Account.Controllers;

[ApiController]
[Route("[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Authorize]
public class ProfileController(AppDbContext dbContext, ILogger<ProfileController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new ProfileResponse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.Identity?.Name));
    }

    [HttpPost("[action]")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SelectGroup(string groupId)
    {
        var user = await dbContext.Users.FindAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (user == null)
            return Unauthorized();

        if (user.MainGroupId == groupId)
            return BadRequest(ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                title: "Group already selected",
                detail: "The selected group is already the user's main group."));

        if (await dbContext.MainGroups.FindAsync(groupId) == null)
            return BadRequest();

        user.MainGroupId = groupId;

        try
        {
            var res = await dbContext.SaveChangesAsync();
            return res > 0 ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while selecting group");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("[action]")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AddOptGroup(string optGroupId)
    {
        var user = await dbContext.Users.FindAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (user == null)
            return Unauthorized();

        await dbContext.UserOpts.AddAsync(new UserOpt { UserId = user.Id, OptGroupId = optGroupId });

        try
        {
            var res = await dbContext.SaveChangesAsync();
            return res > 0 ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (DbUpdateException e)
        {
            logger.LogWarning(e, "Error while adding optional group {OptGroupId} for user {UserId}", optGroupId, user.Id);
            return Conflict(ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status409Conflict,
                title: "Optional group already selected",
                detail: "The user is already subscribed to this optional group."));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while adding optional group {OptGroupId} for user {UserId}", optGroupId, user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("[action]")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveOptGroup(string optGroupId)
    {
        var user = await dbContext.Users.FindAsync(
            User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (user == null)
            return Unauthorized();

        var userOpt = await dbContext.UserOpts.FindAsync(user.Id, optGroupId);
        if (userOpt == null)
            return NotFound(ProblemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status404NotFound,
                title: "Optional group not found",
                detail: "The user is not subscribed to this optional group."));

        dbContext.UserOpts.Remove(userOpt);

        try
        {
            var res = await dbContext.SaveChangesAsync();
            return res > 0 ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while removing optional group {OptGroupId} for user {UserId}", optGroupId, user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
