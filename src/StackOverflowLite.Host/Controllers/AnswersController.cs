using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackOverflowLite.Application.Features.Answers.Commands.CreateAnswer;
using StackOverflowLite.Application.Features.Answers.Commands.DeleteAnswer;
using StackOverflowLite.Application.Features.Answers.Commands.UpdateAnswer;
using StackOverflowLite.Application.Features.Answers.DTOs;
using StackOverflowLite.Application.Features.Answers.Queries.GetAnswersByQuestion;

namespace StackOverflowLite.Host.Controllers;

[ApiController]
public class AnswersController : ControllerBase
{
    private readonly ISender _sender;

    public AnswersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get all answers for a question.
    /// </summary>
    [HttpGet("api/questions/{questionId:int}/answers")]
    public async Task<ActionResult<IReadOnlyList<AnswerDto>>> GetByQuestion(int questionId)
    {
        var result = await _sender.Send(new GetAnswersByQuestionQuery(questionId));
        return Ok(result);
    }

    /// <summary>
    /// Create an answer for a question. Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost("api/questions/{questionId:int}/answers")]
    public async Task<ActionResult<AnswerDto>> Create(int questionId, [FromBody] CreateAnswerDto dto)
    {
        var command = new CreateAnswerCommand(questionId, dto.Content);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Update an answer. Only the author can update.
    /// </summary>
    [Authorize]
    [HttpPut("api/answers/{id:int}")]
    public async Task<ActionResult<AnswerDto>> Update(int id, [FromBody] UpdateAnswerDto dto)
    {
        var command = new UpdateAnswerCommand(id, dto.Content);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete an answer. Only the author can delete and only if it is not accepted.
    /// </summary>
    [Authorize]
    [HttpDelete("api/answers/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _sender.Send(new DeleteAnswerCommand(id));
        return NoContent();
    }
}
