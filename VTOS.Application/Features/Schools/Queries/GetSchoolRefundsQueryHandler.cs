using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Queries;

public class GetSchoolRefundsQueryHandler : IGetSchoolRefundsQueryHandler
{
    private readonly IApplicationDbContext _db;

    public GetSchoolRefundsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<SchoolRefundListResponse>> HandleAsync(GetSchoolRefundsQuery query, CancellationToken ct = default)
    {
        // Step 1: Validate user is School role
        var schoolUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == query.SchoolUserId, ct);

        if (schoolUser == null)
            return Result<SchoolRefundListResponse>.Failure("User not found.", "USER_NOT_FOUND");

        if (schoolUser.Role?.RoleName != "School")
            return Result<SchoolRefundListResponse>.Failure("Only school managers can view refund requests.", "FORBIDDEN");

        if (schoolUser.SchoolID == null)
            return Result<SchoolRefundListResponse>.Failure("User is not assigned to any school.", "SCHOOL_NOT_FOUND");

        var schoolId = schoolUser.SchoolID.Value;

        // Step 2: Query refunds belonging to this school
        var refundsQuery = _db.Refunds
            .AsNoTracking()
            .Include(r => r.PaymentTransaction)
                .ThenInclude(pt => pt.Order)
                    .ThenInclude(o => o.ChildProfile)
                        .ThenInclude(cp => cp.ParentUser)
            .Where(r => r.PaymentTransaction.Order.ChildProfile.SchoolID == schoolId);

        // Apply status filter
        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<RefundStatus>(query.Status, true, out var status))
        {
            refundsQuery = refundsQuery.Where(r => r.RefundStatus == status);
        }

        var totalCount = await refundsQuery.CountAsync(ct);

        var refunds = await refundsQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new SchoolRefundDto
            {
                RefundId = r.Id,
                OrderId = r.PaymentTransaction.OrderID,
                PaymentTransactionId = r.PaymentID,
                RefundAmount = r.RefundAmount,
                RefundStatus = r.RefundStatus.ToString(),
                DisputeReason = r.DisputeReason,
                ParentName = r.PaymentTransaction.Order.ChildProfile.ParentUser.FullName,
                ChildName = r.PaymentTransaction.Order.ChildProfile.FullName,
                OrderTotalAmount = r.PaymentTransaction.Order.TotalAmount,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync(ct);

        return Result<SchoolRefundListResponse>.Success(new SchoolRefundListResponse
        {
            Items = refunds,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}
