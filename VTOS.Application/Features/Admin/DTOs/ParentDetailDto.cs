namespace VTOS.Application.Features.Admin.DTOs;

public record ParentDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string Avatar,
    DateTime? DOB,
    string Gender,
    string Status,
    DateTime CreatedAt,
    DateTime? LastLogin,
    List<ParentChildDto> Children,
    List<ParentOrderDto> Orders,
    decimal TotalSpending
);

public record ParentChildDto(
    Guid Id,
    string FullName,
    string? SchoolName,
    string? Grade
);

public record ParentOrderDto(
    Guid Id,
    int OrderNumber,
    string Status,
    decimal TotalPrice,
    DateTime OrderDate
);
