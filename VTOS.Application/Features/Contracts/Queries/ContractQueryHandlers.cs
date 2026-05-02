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

    public async Task<Result<ContractListResponse>> HandleAsync(
        GetContractsQuery query, CancellationToken ct = default)
    {
        // Resolve scope based on role
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == query.UserId, ct);
        if (user == null) return Result<ContractListResponse>.Failure("User not found.", "NOT_FOUND");

        var schoolMgr = await _context.SchoolManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);

        var q = ContractMapper.IncludeAll(_context.Contracts.AsNoTracking());

        if (query.Role == "School")
        {
            if (schoolMgr == null)
                return Result<ContractListResponse>.Failure("User is not linked to a school.", "NOT_SCHOOL");
            q = q.Where(c => c.SchoolID == schoolMgr.SchoolID);
        }

        else if (query.Role == "Provider")
        {
            var providerMgr = await _context.ProviderManagers.AsNoTracking().FirstOrDefaultAsync(m => m.UserID == user.Id, ct);
            if (providerMgr == null)
                return Result<ContractListResponse>.Failure("User is not linked to a provider.", "NOT_PROVIDER");
            q = q.Where(c => c.ProviderID == providerMgr.ProviderID);
        }

        var now = DateTime.UtcNow;
        var expiringThreshold = now.AddDays(14);
        var summary = new ContractListSummaryDto
        {
            Total = await q.CountAsync(ct),
            Pending = await q.CountAsync(c => c.Status == "Pending", ct),
            WaitingSchool = await q.CountAsync(c => c.Status == "Pending" || c.Status == "PendingSchoolSign", ct),
            WaitingProvider = await q.CountAsync(c => c.Status == "PendingProviderSign", ct),
            Active = await q.CountAsync(c => c.Status == "Active" || c.Status == "InUse", ct),
            Fulfilled = await q.CountAsync(c => c.Status == "Fulfilled", ct),
            Rejected = await q.CountAsync(c => c.Status == "Rejected", ct),
            Issue = await q.CountAsync(c => c.Status == "Rejected" || c.Status == "Expired" || c.Status == "Cancelled", ct),
            ExpiringSoon = await q.CountAsync(c =>
                (c.Status == "Active" || c.Status == "InUse") &&
                c.ExpiresAt >= now &&
                c.ExpiresAt <= expiringThreshold, ct)
        };

        if (!string.IsNullOrWhiteSpace(query.StatusFilter))
        {
            var statuses = query.StatusFilter
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statuses.Length > 0)
                q = q.Where(c => statuses.Contains(c.Status));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(c =>
                c.ContractName.ToLower().Contains(search) ||
                c.ContractNumber.ToLower().Contains(search) ||
                (c.School != null && c.School.SchoolName.ToLower().Contains(search)) ||
                (c.Provider != null && c.Provider.ProviderName.ToLower().Contains(search)) ||
                c.ContractItems.Any(ci => ci.Outfit != null && ci.Outfit.OutfitName.ToLower().Contains(search)));
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var totalCount = await q.CountAsync(ct);

        var contracts = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Result<ContractListResponse>.Success(new ContractListResponse
        {
            Items = contracts.Select(c => ContractMapper.MapToDto(c)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Summary = summary
        });
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
