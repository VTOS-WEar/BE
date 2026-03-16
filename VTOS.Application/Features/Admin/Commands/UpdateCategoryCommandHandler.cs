using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public class UpdateCategoryCommandHandler : IUpdateCategoryCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> HandleAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Find category
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);

        if (category == null)
            return Result<string>.Failure("Category not found", "CATEGORY_NOT_FOUND");

        // Validation: New name must be unique (exclude current category)
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryName == command.CategoryName && c.Id != command.CategoryId, cancellationToken);

        if (existingCategory != null)
            return Result<string>.Failure("Category name already exists", "DUPLICATE_CATEGORY_NAME");

        // Validation: Category name max length 255
        if (string.IsNullOrWhiteSpace(command.CategoryName) || command.CategoryName.Length > 255)
            return Result<string>.Failure("Category name must be between 1 and 255 characters", "INVALID_CATEGORY_NAME");

        category.CategoryName = command.CategoryName;
        category.UpdatedAt = DateTime.UtcNow;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Category updated successfully");
    }
}
