namespace StackOverflowLite.Application.Features.Auth.DTOs;

public record AuthResponseDto(string Token, string Username, string Email, int Reputation);
