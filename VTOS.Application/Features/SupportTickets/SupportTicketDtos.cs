using VTOS.Domain.Enums;

namespace VTOS.Application.Features.SupportTickets;

public record CreateSupportTicketRequestDto(
    string Title,
    string Description,
    string? Category = null,
    Guid? OrderId = null,
    Guid? SemesterPublicationId = null
);

public record SupportTicketResponseDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Status,
    string RequesterRole,
    string RequesterName,
    string RequesterEmail,
    string? SchoolName,
    string? ProviderName,
    Guid? OrderId,
    Guid? SemesterPublicationId,
    string? SemesterLabel,
    string? Response,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    DateTime? ResolvedAt
);

public record SupportTicketListResult(
    IReadOnlyList<SupportTicketResponseDto> Items,
    int Total,
    int Page,
    int PageSize,
    int OpenCount,
    int InProgressCount,
    int ResolvedCount,
    int ClosedCount
);

internal static class SupportTicketStatusParser
{
    public static bool TryParse(string? value, out SupportTicketStatus status)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse(value.Trim(), true, out status))
        {
            return true;
        }

        status = default;
        return false;
    }
}
