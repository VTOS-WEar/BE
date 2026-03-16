namespace VTOS.Application.Features.Admin.DTOs;

public record ParentListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    int ChildrenCount,
    int TotalOrders,
    decimal TotalSpending,
    string Status, // "Active" or "Banned"
    DateTime CreatedAt
);
