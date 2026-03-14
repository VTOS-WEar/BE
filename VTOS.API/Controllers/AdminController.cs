using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Features.Admin.Commands;
using VTOS.Application.Features.Admin.Queries;

namespace VTOS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    // Existing handlers
    private readonly IGetAllUsersQueryHandler _usersHandler;
    private readonly IGetAllFeedbacksQueryHandler _feedbacksHandler;
    private readonly IApproveUserCommandHandler _approveHandler;
    private readonly ISuspendUserCommandHandler _suspendHandler;
    private readonly IRemoveFeedbackCommandHandler _removeFeedbackHandler;
    private readonly IApproveWithdrawalCommandHandler _approveWithdrawalHandler;
    private readonly IGetWithdrawalRequestsQueryHandler _getWithdrawalRequestsHandler;

    // New handlers for user management (3.2.8, 3.2.11)
    private readonly IGetUserDetailQueryHandler _getUserDetailHandler;
    private readonly IGetUserReportQueryHandler _getUserReportHandler;
    
    // School/Provider approval
    private readonly IApproveSchoolRequestCommandHandler _approveSchoolHandler;
    private readonly IApproveProviderRequestCommandHandler _approveProviderHandler;
    
    // Parent management (3.5.1, 3.5.2)
    private readonly IGetParentListQueryHandler _getParentListHandler;
    private readonly IGetParentDetailQueryHandler _getParentDetailHandler;
    
    // Dashboard & Analytics (3.13.x)
    private readonly IGetDashboardAnalyticsQueryHandler _getDashboardAnalyticsHandler;
    private readonly IGetTotalOrdersQueryHandler _getTotalOrdersHandler;
    private readonly IGetTotalQuantityPerItemQueryHandler _getTotalQuantityPerItemHandler;
    private readonly IGetTotalRevenueQueryHandler _getTotalRevenueHandler;
    private readonly IGetPaymentCompletionRateQueryHandler _getPaymentCompletionRateHandler;

    public AdminController(
        IGetAllUsersQueryHandler usersHandler,
        IGetAllFeedbacksQueryHandler feedbacksHandler,
        IApproveUserCommandHandler approveHandler,
        ISuspendUserCommandHandler suspendHandler,
        IRemoveFeedbackCommandHandler removeFeedbackHandler,
        IApproveWithdrawalCommandHandler approveWithdrawalHandler,
        IGetWithdrawalRequestsQueryHandler getWithdrawalRequestsHandler,
        IGetUserDetailQueryHandler getUserDetailHandler,
        IGetUserReportQueryHandler getUserReportHandler,
        IApproveSchoolRequestCommandHandler approveSchoolHandler,
        IApproveProviderRequestCommandHandler approveProviderHandler,
        IGetParentListQueryHandler getParentListHandler,
        IGetParentDetailQueryHandler getParentDetailHandler,
        IGetDashboardAnalyticsQueryHandler getDashboardAnalyticsHandler,
        IGetTotalOrdersQueryHandler getTotalOrdersHandler,
        IGetTotalQuantityPerItemQueryHandler getTotalQuantityPerItemHandler,
        IGetTotalRevenueQueryHandler getTotalRevenueHandler,
        IGetPaymentCompletionRateQueryHandler getPaymentCompletionRateHandler)
    {
        _usersHandler = usersHandler;
        _feedbacksHandler = feedbacksHandler;
        _approveHandler = approveHandler;
        _suspendHandler = suspendHandler;
        _removeFeedbackHandler = removeFeedbackHandler;
        _approveWithdrawalHandler = approveWithdrawalHandler;
        _getWithdrawalRequestsHandler = getWithdrawalRequestsHandler;
        
        _getUserDetailHandler = getUserDetailHandler;
        _getUserReportHandler = getUserReportHandler;
        _approveSchoolHandler = approveSchoolHandler;
        _approveProviderHandler = approveProviderHandler;
        _getParentListHandler = getParentListHandler;
        _getParentDetailHandler = getParentDetailHandler;
        _getDashboardAnalyticsHandler = getDashboardAnalyticsHandler;
        _getTotalOrdersHandler = getTotalOrdersHandler;
        _getTotalQuantityPerItemHandler = getTotalQuantityPerItemHandler;
        _getTotalRevenueHandler = getTotalRevenueHandler;
        _getPaymentCompletionRateHandler = getPaymentCompletionRateHandler;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var result = await _usersHandler.HandleAsync(new GetAllUsersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("feedbacks")]
    public async Task<IActionResult> GetFeedbacks(CancellationToken ct)
    {
        var result = await _feedbacksHandler.HandleAsync(new GetAllFeedbacksQuery(), ct);
        return Ok(result);
    }

    // ✅ Approve User
    [HttpPost("users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(Guid id, CancellationToken ct)
    {
        var success = await _approveHandler.HandleAsync(
            new ApproveUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    // ✅ Suspend User
    [HttpPost("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        var success = await _suspendHandler.HandleAsync(
            new SuspendUserCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    // ✅ Remove Feedback
    [HttpDelete("feedback/{id}")]
    public async Task<IActionResult> RemoveFeedback(Guid id, CancellationToken ct)
    {
        var success = await _removeFeedbackHandler.HandleAsync(
            new RemoveFeedbackCommand(id), ct);

        if (!success) return NotFound();

        return Ok();
    }

    // ✅ Get Withdrawal Requests
    [HttpGet("withdrawals")]
    public async Task<IActionResult> GetWithdrawalRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getWithdrawalRequestsHandler.HandleAsync(
            new GetWithdrawalRequestsQuery(page, pageSize, status), ct);

        return Ok(result);
    }

    // ✅ Approve Withdrawal Request
    [HttpPost("withdrawals/{id}/approve")]
    public async Task<IActionResult> ApproveWithdrawal(
        Guid id,
        [FromBody] ApproveWithdrawalRequest request,
        CancellationToken ct)
    {
        var result = await _approveWithdrawalHandler.HandleAsync(
            new ApproveWithdrawalCommand(id, request.AdminNote), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode is "WITHDRAWAL_NOT_FOUND"
                ? NotFound(new { error = result.Error, code = result.ErrorCode })
                : BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    // ✅ 3.2.8 View User Detail
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserDetail(Guid id, CancellationToken ct)
    {
        var result = await _getUserDetailHandler.HandleAsync(new GetUserDetailQuery(id), ct);
        
        if (result == null)
            return NotFound(new { error = "User not found", code = "USER_NOT_FOUND" });
        
        return Ok(result);
    }

    // ✅ 3.2.11 View User Report
    [HttpGet("reports/users")]
    public async Task<IActionResult> GetUserReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? role,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var result = await _getUserReportHandler.HandleAsync(
            new GetUserReportQuery(dateFrom, dateTo, role, status), ct);
        return Ok(result);
    }

    // ✅ 3.2.12 Approve/Reject School Request
    [HttpPost("school-requests/{schoolId:guid}")]
    public async Task<IActionResult> ApproveSchoolRequest(
        Guid schoolId,
        [FromBody] ApproveOrRejectRequest request,
        CancellationToken ct)
    {
        var result = await _approveSchoolHandler.HandleAsync(
            new ApproveSchoolRequestCommand(schoolId, request.Action, request.AdminNote), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.2.13 Approve/Reject Provider Request
    [HttpPost("provider-requests/{providerId:guid}")]
    public async Task<IActionResult> ApproveProviderRequest(
        Guid providerId,
        [FromBody] ApproveOrRejectRequest request,
        CancellationToken ct)
    {
        var result = await _approveProviderHandler.HandleAsync(
            new ApproveProviderRequestCommand(providerId, request.Action, request.AdminNote), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.5.1 View Parent List
    [HttpGet("parents")]
    public async Task<IActionResult> GetParentList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getParentListHandler.HandleAsync(
            new GetParentListQuery(page, pageSize, search, status), ct);
        return Ok(result);
    }

    // ✅ 3.5.2 View Parent Detail
    [HttpGet("parents/{id:guid}")]
    public async Task<IActionResult> GetParentDetail(Guid id, CancellationToken ct)
    {
        var result = await _getParentDetailHandler.HandleAsync(new GetParentDetailQuery(id), ct);
        
        if (result == null)
            return NotFound(new { error = "Parent not found", code = "PARENT_NOT_FOUND" });
        
        return Ok(result);
    }

    // ✅ 3.13.1 View Dashboard Analytics
    [HttpGet("analytics/dashboard")]
    public async Task<IActionResult> GetDashboardAnalytics(
        [FromQuery] string timeRange = "Month",
        CancellationToken ct = default)
    {
        var result = await _getDashboardAnalyticsHandler.HandleAsync(
            new GetDashboardAnalyticsQuery(timeRange), ct);
        return Ok(result);
    }

    // ✅ 3.13.2 View Total Order
    [HttpGet("analytics/orders")]
    public async Task<IActionResult> GetTotalOrders(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct = default)
    {
        var result = await _getTotalOrdersHandler.HandleAsync(
            new GetTotalOrdersQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }

    // ✅ 3.13.3 View Total Quantity Per Item
    [HttpGet("analytics/quantity-per-item")]
    public async Task<IActionResult> GetTotalQuantityPerItem(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct = default)
    {
        var result = await _getTotalQuantityPerItemHandler.HandleAsync(
            new GetTotalQuantityPerItemQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }

    // ✅ 3.13.4 View Total Revenue
    [HttpGet("analytics/revenue")]
    public async Task<IActionResult> GetTotalRevenue(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct = default)
    {
        var result = await _getTotalRevenueHandler.HandleAsync(
            new GetTotalRevenueQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }

    // ✅ 3.13.5 View Payment Completion Rate
    [HttpGet("analytics/payment-completion-rate")]
    public async Task<IActionResult> GetPaymentCompletionRate(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct = default)
    {
        var result = await _getPaymentCompletionRateHandler.HandleAsync(
            new GetPaymentCompletionRateQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }
}

public record ApproveWithdrawalRequest(string? AdminNote);

public record ApproveOrRejectRequest(
    string Action,
    string? AdminNote = null
);
