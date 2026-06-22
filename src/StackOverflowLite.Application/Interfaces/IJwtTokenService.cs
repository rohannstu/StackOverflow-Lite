using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(ApplicationUser user);
}
