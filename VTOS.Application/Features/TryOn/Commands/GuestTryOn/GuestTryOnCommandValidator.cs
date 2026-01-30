using FluentValidation;

namespace VTOS.Application.Features.TryOn.Commands.GuestTryOn;

/// <summary>
/// Validator for GuestTryOnCommand
/// </summary>
public class GuestTryOnCommandValidator : AbstractValidator<GuestTryOnCommand>
{
    private static readonly string[] AllowedContentTypes = 
    {
        "image/jpeg",
        "image/jpg", 
        "image/png",
        "image/webp"
    };

    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public GuestTryOnCommandValidator()
    {
        RuleFor(x => x.OutfitId)
            .NotEmpty()
            .WithMessage("Outfit ID is required.");

        RuleFor(x => x.Photo)
            .NotNull()
            .WithMessage("Photo is required.")
            .Must(photo => photo.Length > 0)
            .WithMessage("Photo cannot be empty.")
            .Must(photo => photo.Length <= MaxFileSizeBytes)
            .WithMessage($"Photo size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.")
            .Must(photo => AllowedContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
            .WithMessage($"Photo must be a valid image format: {string.Join(", ", AllowedContentTypes)}.");
    }
}
