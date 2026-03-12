using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Conversations;

namespace StudentSocialNetwork.Api.Application.Validators.Conversations;

public class AddConversationMemberRequestDtoValidator : AbstractValidator<AddConversationMemberRequestDto>
{
    public AddConversationMemberRequestDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(20);
    }
}
