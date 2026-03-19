using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Application.Features.Contracts.Commands;
using VTOS.Application.Features.Contracts.DTOs;

namespace VTOS.Application.Features.Contracts.Queries;

// ─── List Contracts Handler ───
public class GetContractsQueryHandler : IGetContractsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetContractsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<ContractDto>>> HandleAsync(
        GetContractsQuery query, CancellationToken ct = default)
    {
        // Resolve scope based on role
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null) return Result<List<ContractDto>>.Failure("User not found.", "NOT_FOUND");

        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var q = ContractMapper.IncludeAll(_context.Contracts.AsQueryable());

        if (query.Role == "School")
        {
            if (schoolMgr == null)
                return Result<List<ContractDto>>.Failure("User is not linked to a school.", "NOT_SCHOOL");
            q = q.Where(c => c.SchoolID == schoolMgr.SchoolID);
        }

        else if (query.Role == "Provider")
        {
            var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
            if (providerMgr == null)
                return Result<List<ContractDto>>.Failure("User is not linked to a provider.", "NOT_PROVIDER");
            q = q.Where(c => c.ProviderID == providerMgr.ProviderID);
        }

        if (!string.IsNullOrWhiteSpace(query.StatusFilter))
            q = q.Where(c => c.Status == query.StatusFilter);

        q = q.OrderByDescending(c => c.CreatedAt);

        var contracts = await q.ToListAsync(ct);

        return Result<List<ContractDto>>.Success(
            contracts.Select(ContractMapper.MapToDto).ToList()
        );
    }
}

// ─── Contract Detail Handler ───
public class GetContractDetailQueryHandler : IGetContractDetailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetContractDetailQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<ContractDto>> HandleAsync(
        GetContractDetailQuery query, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null) return Result<ContractDto>.Failure("User not found.", "NOT_FOUND");

        var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var q = ContractMapper.IncludeAll(_context.Contracts.AsQueryable())
            .Where(c => c.Id == query.ContractId);

        // Scope to role
        if (query.Role == "School" && schoolMgr?.SchoolID != null)
            q = q.Where(c => c.SchoolID == schoolMgr.SchoolID);
        else if (query.Role == "Provider" && providerMgr != null)
            q = q.Where(c => c.ProviderID == providerMgr.ProviderID);
        else
            return Result<ContractDto>.Failure("User role not linked to entity.", "NOT_LINKED");

        var contract = await q.FirstOrDefaultAsync(ct);
        if (contract == null)
            return Result<ContractDto>.Failure("Contract not found.", "NOT_FOUND");

        return Result<ContractDto>.Success(ContractMapper.MapToDto(contract));
    }
}
