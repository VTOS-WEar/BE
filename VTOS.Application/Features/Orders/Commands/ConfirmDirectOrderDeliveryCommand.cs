using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Orders.Commands;

public record ConfirmDirectOrderDeliveryCommand(Guid ParentId, Guid OrderId);

public interface IConfirmDirectOrderDeliveryCommandHandler
{
    Task<Result> HandleAsync(ConfirmDirectOrderDeliveryCommand command, CancellationToken cancellationToken = default);
}

public class ConfirmDirectOrderDeliveryCommandHandler : IConfirmDirectOrderDeliveryCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ConfirmDirectOrderDeliveryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> HandleAsync(ConfirmDirectOrderDeliveryCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.ChildProfile)
            .FirstOrDefaultAsync(
                o => o.Id == command.OrderId &&
                     o.ProviderID != null &&
                     o.SemesterPublicationID != null &&
                     o.ChildProfile.ParentUserID == command.ParentId,
                cancellationToken);

        if (order == null)
            return Result.Failure("Direct order not found.", "ORDER_NOT_FOUND");

        if (order.OrderStatus != OrderStatus.Shipped)
            return Result.Failure("Only shipped orders can be confirmed as delivered.", "INVALID_ORDER_STATUS");

        order.OrderStatus = OrderStatus.Delivered;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
