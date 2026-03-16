using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;

namespace VTOS.Application.Features.Admin.Commands;

public class AddCategoryCommandHandler : IAddCategoryCommandHandler
{
    private readonly IApplicationDbContext _context;

    public AddCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> HandleAsync(
        AddCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Validation: Category name must be unique
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryName == command.CategoryName, cancellationToken);

        if (existingCategory != null)
            return Result<Guid>.Failure("Category name already exists", "DUPLICATE_CATEGORY_NAME");

        // Validation: Category name max length 255
        if (string.IsNullOrWhiteSpace(command.CategoryName) || command.CategoryName.Length > 255)
            return Result<Guid>.Failure("Category name must be between 1 and 255 characters", "INVALID_CATEGORY_NAME");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            CategoryName = command.CategoryName,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
