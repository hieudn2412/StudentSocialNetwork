using FluentValidation;
using StudentSocialNetwork.Api.Application.DTOs.Messages;

namespace StudentSocialNetwork.Api.Application.Validators.Messages;

public class MessageAttachmentInputDtoValidator : AbstractValidator<MessageAttachmentInputDto>
{
    public MessageAttachmentInputDtoValidator()
    {
        RuleFor(x => x.FileUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FileSize).GreaterThan(0);
    }
}
