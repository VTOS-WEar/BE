using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Providers.Commands;

public record AcceptDirectOrderCommand(Guid UserId, Guid OrderId);
public interface IAcceptDirectOrderCommandHandler
{
    Task<Result> HandleAsync(AcceptDirectOrderCommand command, CancellationToken cancellationToken = default);
}

public record UpdateDirectOrderInProductionCommand(Guid UserId, Guid OrderId);
public interface IUpdateDirectOrderInProductionCommandHandler
{
    Task<Result> HandleAsync(UpdateDirectOrderInProductionCommand command, CancellationToken cancellationToken = default);
}

public record MarkDirectOrderReadyToShipCommand(Guid UserId, Guid OrderId);
public interface IMarkDirectOrderReadyToShipCommandHandler
{
    Task<Result> HandleAsync(MarkDirectOrderReadyToShipCommand command, CancellationToken cancellationToken = default);
}

public record ShipDirectOrderCommand(Guid UserId, Guid OrderId, string TrackingCode, string ShippingCompany);
public interface IShipDirectOrderCommandHandler
{
    Task<Result> HandleAsync(ShipDirectOrderCommand command, CancellationToken cancellationToken = default);
}

public class AcceptDirectOrderCommandHandler : IAcceptDirectOrderCommandHandler
{
    protected readonly IApplicationDbContext Context;

    public AcceptDirectOrderCommandHandler(IApplicationDbContext context)
    {
        Context = context;
    }

    public Task<Result> HandleAsync(AcceptDirectOrderCommand command, CancellationToken cancellationToken = default)
        => UpdateStatusAsync(command.UserId, command.OrderId, OrderStatus.Paid, OrderStatus.Accepted, cancellationToken);

    protected async Task<Result> UpdateStatusAsync(Guid userId, Guid orderId, OrderStatus expectedStatus, OrderStatus newStatus, CancellationToken ct)
    {
        var order = await FindProviderOrderAsync(userId, orderId, ct);
        if (order == null)
            return Result.Failure("Direct order not found.", "ORDER_NOT_FOUND");
        if (order.OrderStatus != expectedStatus)
            return Result.Failure($"Order must be in status {expectedStatus} before transitioning to {newStatus}.", "INVALID_ORDER_STATUS");

        order.OrderStatus = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync(ct);
        return Result.Success();
    }

    protected async Task<Order?> FindProviderOrderAsync(Guid userId, Guid orderId, CancellationToken ct)
    {
        var providerId = await Context.ProviderManagers
            .Where(pm => pm.UserID == userId)
            .Select(pm => (Guid?)pm.ProviderID)
            .FirstOrDefaultAsync(ct);

        if (!providerId.HasValue)
            return null;

        return await Context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.ProviderID == providerId.Value && o.SemesterPublicationID != null, ct);
    }
}

public class UpdateDirectOrderInProductionCommandHandler : AcceptDirectOrderCommandHandler, IUpdateDirectOrderInProductionCommandHandler
{
    public UpdateDirectOrderInProductionCommandHandler(IApplicationDbContext context) : base(context)
    {
    }

    public Task<Result> HandleAsync(UpdateDirectOrderInProductionCommand command, CancellationToken cancellationToken = default)
        => UpdateStatusAsync(command.UserId, command.OrderId, OrderStatus.Accepted, OrderStatus.InProduction, cancellationToken);
}

public class MarkDirectOrderReadyToShipCommandHandler : AcceptDirectOrderCommandHandler, IMarkDirectOrderReadyToShipCommandHandler
{
    public MarkDirectOrderReadyToShipCommandHandler(IApplicationDbContext context) : base(context)
    {
    }

    public Task<Result> HandleAsync(MarkDirectOrderReadyToShipCommand command, CancellationToken cancellationToken = default)
        => UpdateStatusAsync(command.UserId, command.OrderId, OrderStatus.InProduction, OrderStatus.ReadyToShip, cancellationToken);
}

public class ShipDirectOrderCommandHandler : IShipDirectOrderCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ShipDirectOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> HandleAsync(ShipDirectOrderCommand command, CancellationToken cancellationToken = default)
    {
        var providerId = await _context.ProviderManagers
            .Where(pm => pm.UserID == command.UserId)
            .Select(pm => (Guid?)pm.ProviderID)
            .FirstOrDefaultAsync(cancellationToken);

        if (!providerId.HasValue)
            return Result.Failure("Provider not found for current user.", "PROVIDER_NOT_FOUND");

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == command.OrderId && o.ProviderID == providerId.Value && o.SemesterPublicationID != null, cancellationToken);

        if (order == null)
            return Result.Failure("Direct order not found.", "ORDER_NOT_FOUND");

        if (order.OrderStatus != OrderStatus.ReadyToShip)
            return Result.Failure("Order must be ReadyToShip before shipping.", "INVALID_ORDER_STATUS");

        if (string.IsNullOrWhiteSpace(command.TrackingCode) || string.IsNullOrWhiteSpace(command.ShippingCompany))
            return Result.Failure("TrackingCode and ShippingCompany are required.", "MISSING_SHIPPING_INFO");

        order.TrackingCode = command.TrackingCode.Trim();
        order.ShippingCompany = command.ShippingCompany.Trim();
        order.OrderStatus = OrderStatus.Shipped;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
