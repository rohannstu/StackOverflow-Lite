using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackOverflowLite.Application.Features.Questions.Commands.CreateQuestion;
using StackOverflowLite.Application.Features.Questions.Commands.DeleteQuestion;
using StackOverflowLite.Application.Features.Questions.Commands.UpdateQuestion;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Features.Questions.Queries.GetQuestionById;
using StackOverflowLite.Application.Features.Questions.Queries.GetQuestions;

namespace StackOverflowLite.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly ISender _sender;

    public QuestionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get all questions (paginated).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _sender.Send(new GetQuestionsQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// Get a single question by ID (increments view count).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> GetById(int id)
    {
        var result = await _sender.Send(new GetQuestionByIdQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Create a new question. Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] CreateQuestionDto dto)
    {
        var command = new CreateQuestionCommand(dto.Title, dto.Description);
        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update a question. Only the author can update.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuestionDto>> Update(int id, [FromBody] UpdateQuestionDto dto)
    {
        var command = new UpdateQuestionCommand(id, dto.Title, dto.Description);
        var result = await _sender.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a question. Only the author can delete.
    /// </summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _sender.Send(new DeleteQuestionCommand(id));
        return NoContent();
    }
}
