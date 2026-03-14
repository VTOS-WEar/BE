using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public class DeleteCategoryCommandHandler : IDeleteCategoryCommandHandler
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<string>> HandleAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Find category
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);

        if (category == null)
            return Result<string>.Failure("Category not found", "CATEGORY_NOT_FOUND");

        // Validation: Category must not be used by any uniform
        var isUsedByOutfit = await _context.OutfitCategories
            .AnyAsync(oc => oc.CategoryID == command.CategoryId, cancellationToken);

        if (isUsedByOutfit)
            return Result<string>.Failure(
                "Category is being used by one or more uniforms and cannot be deleted",
                "CATEGORY_IN_USE");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<string>.Success("Category deleted successfully");
    }
}
