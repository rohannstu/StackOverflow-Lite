using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackOverflowLite.Application.Features.Votes.Commands.CastVote;
using StackOverflowLite.Application.Features.Votes.DTOs;

namespace StackOverflowLite.Host.Controllers;

[ApiController]
[Route("api")]
public class VotesController : ControllerBase
{
    private readonly ISender _sender;

    public VotesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Cast a vote on a question. Toggling the same vote type removes it.
    /// </summary>
    [Authorize]
    [HttpPost("questions/{questionId:int}/vote")]
    public async Task<IActionResult> VoteOnQuestion(int questionId, [FromBody] VoteDto dto)
    {
        var command = new CastVoteCommand(questionId, null, dto.Type);
        await _sender.Send(command);
        return Ok(new { message = "Vote recorded successfully." });
    }

    /// <summary>
    /// Cast a vote on an answer. Toggling the same vote type removes it.
    /// </summary>
    [Authorize]
    [HttpPost("answers/{answerId:int}/vote")]
    public async Task<IActionResult> VoteOnAnswer(int answerId, [FromBody] VoteDto dto)
    {
        var command = new CastVoteCommand(null, answerId, dto.Type);
        await _sender.Send(command);
        return Ok(new { message = "Vote recorded successfully." });
    }
}
