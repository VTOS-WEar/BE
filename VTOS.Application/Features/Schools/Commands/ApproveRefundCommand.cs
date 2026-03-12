using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Common.Models;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Schools.Commands;

/// <summary>
/// Command for school to approve a refund request.
/// Flow: authorize School role → validate ownership → PayOS payout to parent → deduct school wallet → update statuses
/// </summary>
public record ApproveRefundCommand(Guid SchoolUserId, Guid RefundId);

public interface IApproveRefundCommandHandler
{
    Task<Result> HandleAsync(ApproveRefundCommand command, CancellationToken ct = default);
}
