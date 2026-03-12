using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Conversations;

namespace StudentSocialNetwork.Api.Application.Validators.Conversations;

public class CreatePrivateConversationRequestDtoValidator : AbstractValidator<CreatePrivateConversationRequestDto>
{
    public CreatePrivateConversationRequestDtoValidator()
    {
        RuleFor(x => x.OtherUserId).GreaterThan(0);
    }
}
