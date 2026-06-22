using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackOverflowLite.Application.Features.Auth.Commands.Login;
using StackOverflowLite.Application.Features.Auth.Commands.Register;
using StackOverflowLite.Application.Features.Auth.DTOs;
using StackOverflowLite.Application.Features.Auth.Queries.GetProfile;

namespace StackOverflowLite.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var command = new RegisterCommand(dto.Username, dto.Email, dto.Password);
        var result = await _sender.Send(command);
        return Ok(new { message = result });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var command = new LoginCommand(dto.Email, dto.Password);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var query = new GetProfileQuery();
        var result = await _sender.Send(query);
        return Ok(result);
    }
}
