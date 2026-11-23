using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        
        if(user.MainGroupId == groupId)
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
        catch(Exception e)
        {
            logger.LogError(e, "Error while selecting group");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}