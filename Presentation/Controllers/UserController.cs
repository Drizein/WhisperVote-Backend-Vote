using Application.Clients;
using Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace WhisperVote_Backend_Result.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly UcUser _ucUser;
    private readonly IAuthServerClient _authServerClient;

    public UserController(ILogger<UserController> logger, UcUser ucUser, IAuthServerClient authServerClient)
    {
        _logger = logger;
        _ucUser = ucUser;
        _authServerClient = authServerClient;
    }

    [HttpPost]
    public async Task<ActionResult<string>> RequestRoleChange([FromQuery] string jwt, [FromQuery] string role,
        [FromQuery] string reason)
    {
        _logger.LogDebug("UserController - RequestRoleChange");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) = await _ucUser.RequestRoleChange(jwt, role, reason);
        if (success) return Ok(message); // HTTP 200

        return BadRequest(message);
    }

    [HttpGet]
    public async Task<ActionResult<string>> GetAllOpenRequests([FromQuery] string jwt)
    {
        _logger.LogDebug("UserController - GetAllOpenRequests");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, openRequestChangeRoleDto) = _ucUser.GetAllOpenRequests(_authServerClient.GetRoleForUser(jwt));
        if (success) return Ok(openRequestChangeRoleDto); // HTTP 200

        return BadRequest(openRequestChangeRoleDto);
    }

    [HttpPost]
    public async Task<ActionResult<string>> CloseRequest([FromQuery] string jwt, [FromQuery] string requestUserId,
        [FromQuery] bool isAccepted)
    {
        _logger.LogDebug("UserController - AcceptRequest");
        if (!await _authServerClient.IsAuthenticated(jwt)) return Unauthorized();

        var (success, message) =
            await _ucUser.CloseRequest(jwt, _authServerClient.GetRoleForUser(jwt), requestUserId, isAccepted);
        if (success) return Ok(message); // HTTP 200

        return BadRequest(message);
    }
}