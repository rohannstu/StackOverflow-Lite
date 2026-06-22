using MediatR;
using Microsoft.AspNetCore.Identity;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Username, string Email, string Password) : IRequest<string>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, string>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        var existingUserByUsername = await _userManager.FindByNameAsync(request.Username);
        if (existingUserByUsername != null)
        {
            throw new ConflictException("Username is already taken.");
        }

        // Check if email already exists
        var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingUserByEmail != null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            Reputation = 0
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"User creation failed: {errors}");
        }

        return "User registered successfully.";
    }
}
