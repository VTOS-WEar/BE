using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Payments.Commands;

// ── GenerateInvoiceCommand ──────────────────────────────────────────
public record GenerateInvoiceCommand(Guid UserId, Guid OrderId);

public record GenerateInvoiceResponse(Guid InvoiceId, DateTime IssueDate);

public interface IGenerateInvoiceCommandHandler
{
    Task<Result<GenerateInvoiceResponse>> HandleAsync(GenerateInvoiceCommand command, CancellationToken ct = default);
}

public class GenerateInvoiceCommandHandler : IGenerateInvoiceCommandHandler
{
    private readonly IApplicationDbContext _db;

    public GenerateInvoiceCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<GenerateInvoiceResponse>> HandleAsync(GenerateInvoiceCommand command, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == command.UserId, ct);
        if (user == null)
            return Result<GenerateInvoiceResponse>.Failure("Access denied.", "ACCESS_DENIED");

        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order == null)
            return Result<GenerateInvoiceResponse>.Failure("Order not found.", "ORDER_NOT_FOUND");

        // Check if invoice already exists
        var existing = await _db.Invoices.AsNoTracking()
            .AnyAsync(i => i.OrderID == command.OrderId, ct);
        if (existing)
            return Result<GenerateInvoiceResponse>.Failure("Invoice already exists for this order.", "ALREADY_EXISTS");

        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            OrderID = command.OrderId,
            IssueDate = DateTime.UtcNow,
            InvoiceDataURL = null, // Can be populated later with PDF URL
            CreatedAt = DateTime.UtcNow
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return Result<GenerateInvoiceResponse>.Success(
            new GenerateInvoiceResponse(invoice.Id, invoice.IssueDate));
    }
}
