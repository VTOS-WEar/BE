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

    // Reports & Export (3.13.8-11)
    private readonly IViewReportQueryHandler _viewReportHandler;
    private readonly IExportReportCommandHandler _exportReportHandler;
    private readonly IGenerateSystemReportCommandHandler _generateSystemReportHandler;
    private readonly IExportSchoolActivityLogsCommandHandler _exportSchoolActivityLogsHandler;

    // Uniform Categories (3.14.1-4)
    private readonly GetCategoriesQueryHandler _getCategoriesHandler;
    private readonly AddCategoryCommandHandler _addCategoryHandler;
    private readonly UpdateCategoryCommandHandler _updateCategoryHandler;
    private readonly DeleteCategoryCommandHandler _deleteCategoryHandler;

    // Settings Configuration (3.14.5-8)
    private readonly ConfigureSizeTemplateCommandHandler _configureSizeTemplateHandler;
    private readonly ConfigureDefaultSizeChartCommandHandler _configureDefaultSizeChartHandler;
    private readonly ConfigurePaymentMethodCommandHandler _configurePaymentMethodHandler;
    private readonly ConfigureAITryOnSettingsCommandHandler _configureAITryOnSettingsHandler;

    // Payment Monitoring (3.15.1)
    private readonly MonitorPaymentTransactionsQueryHandler _monitorPaymentTransactionsHandler;

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
        IGetPaymentCompletionRateQueryHandler getPaymentCompletionRateHandler,
        IViewReportQueryHandler viewReportHandler,
        IExportReportCommandHandler exportReportHandler,
        IGenerateSystemReportCommandHandler generateSystemReportHandler,
        IExportSchoolActivityLogsCommandHandler exportSchoolActivityLogsHandler,
        GetCategoriesQueryHandler getCategoriesHandler,
        AddCategoryCommandHandler addCategoryHandler,
        UpdateCategoryCommandHandler updateCategoryHandler,
        DeleteCategoryCommandHandler deleteCategoryHandler,
        ConfigureSizeTemplateCommandHandler configureSizeTemplateHandler,
        ConfigureDefaultSizeChartCommandHandler configureDefaultSizeChartHandler,
        ConfigurePaymentMethodCommandHandler configurePaymentMethodHandler,
        ConfigureAITryOnSettingsCommandHandler configureAITryOnSettingsHandler,
        MonitorPaymentTransactionsQueryHandler monitorPaymentTransactionsHandler)
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

        _viewReportHandler = viewReportHandler;
        _exportReportHandler = exportReportHandler;
        _generateSystemReportHandler = generateSystemReportHandler;
        _exportSchoolActivityLogsHandler = exportSchoolActivityLogsHandler;

        _getCategoriesHandler = getCategoriesHandler;
        _addCategoryHandler = addCategoryHandler;
        _updateCategoryHandler = updateCategoryHandler;
        _deleteCategoryHandler = deleteCategoryHandler;

        _configureSizeTemplateHandler = configureSizeTemplateHandler;
        _configureDefaultSizeChartHandler = configureDefaultSizeChartHandler;
        _configurePaymentMethodHandler = configurePaymentMethodHandler;
        _configureAITryOnSettingsHandler = configureAITryOnSettingsHandler;

        _monitorPaymentTransactionsHandler = monitorPaymentTransactionsHandler;
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
            new ApproveSchoolRequestCommand(schoolId, request.Action, request.RejectionReason, request.AdminNote), ct);

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
            new ApproveProviderRequestCommand(providerId, request.Action, request.RejectionReason, request.AdminNote), ct);

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

    // ✅ 3.13.8 View Report
    [HttpGet("reports")]
    public async Task<IActionResult> ViewReport(
        [FromQuery] string reportType,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] Guid? schoolId,
        CancellationToken ct = default)
    {
        var from = !string.IsNullOrEmpty(dateFrom) ? DateTime.Parse(dateFrom) : (DateTime?)null;
        var to = !string.IsNullOrEmpty(dateTo) ? DateTime.Parse(dateTo) : (DateTime?)null;

        var result = await _viewReportHandler.HandleAsync(
            new ViewReportQuery(reportType, from, to, schoolId), ct);
        return Ok(result);
    }

    // ✅ 3.13.9 Export Report
    [HttpGet("reports/export")]
    public async Task<IActionResult> ExportReport(
        [FromQuery] string reportType,
        [FromQuery] string exportFormat,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] Guid? schoolId,
        CancellationToken ct = default)
    {
        var from = !string.IsNullOrEmpty(dateFrom) ? DateTime.Parse(dateFrom) : (DateTime?)null;
        var to = !string.IsNullOrEmpty(dateTo) ? DateTime.Parse(dateTo) : (DateTime?)null;

        var result = await _exportReportHandler.HandleAsync(
            new ExportReportCommand(reportType, exportFormat, from, to, schoolId), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        // Set proper Content-Type and file extension based on format
        var (contentType, extension) = exportFormat.ToUpper() switch
        {
            "CSV" => ("text/csv", ".csv"),
            "EXCEL" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
            "PDF" => ("application/pdf", ".pdf"),
            _ => ("application/octet-stream", ".bin")
        };

        var filename = $"{reportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
        return File(result.Value, contentType, filename);
    }

    // ✅ 3.13.10 Generate System Report
    [HttpPost("reports/generate")]
    public async Task<IActionResult> GenerateSystemReport(
        [FromBody] GenerateSystemReportRequest request,
        CancellationToken ct = default)
    {
        var result = await _generateSystemReportHandler.HandleAsync(
            new GenerateSystemReportCommand(request.ReportFrequency), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.13.11 Export School Activity Logs
    [HttpGet("activities/export/{schoolId:guid}")]
    public async Task<IActionResult> ExportSchoolActivityLogs(
        Guid schoolId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct = default)
    {
        var from = !string.IsNullOrEmpty(dateFrom) ? DateTime.Parse(dateFrom) : (DateTime?)null;
        var to = !string.IsNullOrEmpty(dateTo) ? DateTime.Parse(dateTo) : (DateTime?)null;

        var result = await _exportSchoolActivityLogsHandler.HandleAsync(
            new ExportSchoolActivityLogsCommand(schoolId, from, to), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return File(result.Value, "text/csv", $"school_activity_logs_{schoolId}__{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    // ✅ 3.14.1 View Uniform Categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct = default)
    {
        var result = await _getCategoriesHandler.HandleAsync(new GetCategoriesQuery(), ct);
        return Ok(result);
    }

    // ✅ 3.14.2 Add Uniform Category
    [HttpPost("categories")]
    public async Task<IActionResult> AddCategory(
        [FromBody] AddCategoryRequest request,
        CancellationToken ct = default)
    {
        var result = await _addCategoryHandler.HandleAsync(
            new AddCategoryCommand(request.CategoryName), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetCategories), new { id = result.Value }, result.Value);
    }

    // ✅ 3.14.3 Update Uniform Category
    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        var result = await _updateCategoryHandler.HandleAsync(
            new UpdateCategoryCommand(id, request.CategoryName), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.14.4 Delete Uniform Category
    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        CancellationToken ct = default)
    {
        var result = await _deleteCategoryHandler.HandleAsync(
            new DeleteCategoryCommand(id), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.14.5 Configure Uniform Size Template
    [HttpPost("settings/size-template")]
    public async Task<IActionResult> ConfigureSizeTemplate(
        [FromBody] ConfigureSizeTemplateRequest request,
        CancellationToken ct = default)
    {
        var result = await _configureSizeTemplateHandler.HandleAsync(
            new ConfigureSizeTemplateCommand(request.ChartName, request.Description, request.Unit ?? "cm"), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetCategories), new { id = result.Value }, result.Value);
    }

    // ✅ 3.14.6 Configure Default Size Chart
    [HttpPost("settings/default-size-chart")]
    public async Task<IActionResult> ConfigureDefaultSizeChart(
        [FromBody] ConfigureDefaultSizeChartRequest request,
        CancellationToken ct = default)
    {
        var result = await _configureDefaultSizeChartHandler.HandleAsync(
            new ConfigureDefaultSizeChartCommand(request.SizeChartId), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.14.7 Configure Payment Method
    [HttpPost("settings/payment-method")]
    public async Task<IActionResult> ConfigurePaymentMethod(
        [FromBody] ConfigurePaymentMethodRequest request,
        CancellationToken ct = default)
    {
        var result = await _configurePaymentMethodHandler.HandleAsync(
            new ConfigurePaymentMethodCommand(request.PaymentGateway, request.IsEnabled, request.ApiKey, request.SecretKey), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.14.8 Configure AI Try-On Settings
    [HttpPost("settings/ai-tryon")]
    public async Task<IActionResult> ConfigureAITryOnSettings(
        [FromBody] ConfigureAITryOnSettingsRequest request,
        CancellationToken ct = default)
    {
        var result = await _configureAITryOnSettingsHandler.HandleAsync(
            new ConfigureAITryOnSettingsCommand(request.ModelVersion, request.ImageResolution, request.MaxUploadFileSizeMB), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ✅ 3.15.1 Monitor Payment Transactions
    [HttpGet("payments")]
    public async Task<IActionResult> MonitorPaymentTransactions(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? status,
        [FromQuery] string? paymentGateway,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _monitorPaymentTransactionsHandler.HandleAsync(
            new MonitorPaymentTransactionsQuery(dateFrom, dateTo, status, paymentGateway, page, pageSize), ct);
        return Ok(result);
    }
}


public record ApproveWithdrawalRequest(string? AdminNote);

public record ApproveOrRejectRequest(
    string Action,
    string? RejectionReason = null,
    string? AdminNote = null
);

// Report/Analytics DTOs
public record GenerateSystemReportRequest(string ReportFrequency);

// Category DTOs
public record AddCategoryRequest(string CategoryName);
public record UpdateCategoryRequest(string CategoryName);

// Settings Configuration DTOs
public record ConfigureSizeTemplateRequest(
    string ChartName,
    string? Description = null,
    string? Unit = null
);

public record ConfigureDefaultSizeChartRequest(Guid SizeChartId);

public record ConfigurePaymentMethodRequest(
    string PaymentGateway,
    bool IsEnabled,
    string? ApiKey = null,
    string? SecretKey = null
);

public record ConfigureAITryOnSettingsRequest(
    string? ModelVersion = null,
    string? ImageResolution = null,
    int? MaxUploadFileSizeMB = null
);
