using FluentValidation;
using VTOS.Application.Features.Users.Commands;

namespace VTOS.Application.Features.Users.Validators;

public class UpdateAvatarValidator : AbstractValidator<UpdateAvatarCommand>
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    public UpdateAvatarValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Avatar)
            .NotNull()
            .WithMessage("Avatar file is required");

        RuleFor(x => x.Avatar.Length)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024)}MB")
            .When(x => x.Avatar != null);

        RuleFor(x => x.Avatar.FileName)
            .Must(HasValidExtension)
            .WithMessage($"Only {string.Join(", ", AllowedExtensions)} files are allowed")
            .When(x => x.Avatar != null);

        RuleFor(x => x.Avatar.ContentType)
            .Must(contentType => AllowedContentTypes.Contains(contentType.ToLower()))
            .WithMessage($"Invalid file type. Allowed types: {string.Join(", ", AllowedContentTypes)}")
            .When(x => x.Avatar != null);
    }

    private static bool HasValidExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}