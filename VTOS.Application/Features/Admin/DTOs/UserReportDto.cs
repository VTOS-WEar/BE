namespace VTOS.Application.Features.Admin.DTOs;

public record UserReportDto(
    int TotalUsers,
    int TotalParents,
    int TotalSchools,
    int TotalProviders,
    int TotalAdmins,
    int ActiveUsers,
    int BannedUsers,
    decimal TotalSpending,
    int TotalOrders,
    List<UserByRoleDto> UsersByRole,
    List<UserActivityDto> RecentActivity
);

public record UserByRoleDto(
    string Role,
    int Count,
    int Active,
    int Banned
);

public record UserActivityDto(
    Guid UserId,
    string FullName,
    string Role,
    int OrderCount,
    decimal TotalSpending,
    DateTime? LastLogin
);
