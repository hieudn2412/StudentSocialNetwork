using StudentSocialNetwork.Api.Application.DTOs.Auth;
using StudentSocialNetwork.Api.Domain.Entities;

namespace StudentSocialNetwork.Api.Application.Interfaces.Security;

public interface IJwtTokenGenerator
{
    TokenResultDto GenerateToken(User user);
}
