using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;

namespace VTOS.API.Controllers;

/// <summary>
/// School management APIs (requires School role).
/// UC-42: Maintain School Profile
/// UC-43: Import Student Data
/// UC-45: View Parent Orders
/// UC-46: Track Pre-order Progress
/// UC-49: View Sales Reports
/// UC-50: View Feedback Reports
/// </summary>
[ApiController]
[Route("api/schools")]
[Authorize(Roles = "School")]
public class SchoolsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IGetSchoolProfileQueryHandler _getProfileHandler;
    private readonly IUpdateSchoolProfileCommandHandler _updateProfileHandler;
    private readonly IGetSchoolOrdersQueryHandler _getOrdersHandler;
    private readonly IGetCampaignProgressQueryHandler _getCampaignProgressHandler;
    private readonly IGetSalesReportQueryHandler _getSalesReportHandler;
    private readonly IGetFeedbackReportQueryHandler _getFeedbackReportHandler;
    private readonly IImportStudentDataCommandHandler _importStudentHandler;
    private readonly IValidator<UpdateSchoolProfileCommand> _updateProfileValidator;

    public SchoolsController(
        ICurrentUserService currentUser,
        IGetSchoolProfileQueryHandler getProfileHandler,
        IUpdateSchoolProfileCommandHandler updateProfileHandler,
        IGetSchoolOrdersQueryHandler getOrdersHandler,
        IGetCampaignProgressQueryHandler getCampaignProgressHandler,
        IGetSalesReportQueryHandler getSalesReportHandler,
        IGetFeedbackReportQueryHandler getFeedbackReportHandler,
        IImportStudentDataCommandHandler importStudentHandler,
        IValidator<UpdateSchoolProfileCommand> updateProfileValidator)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _getOrdersHandler = getOrdersHandler;
        _getCampaignProgressHandler = getCampaignProgressHandler;
        _getSalesReportHandler = getSalesReportHandler;
        _getFeedbackReportHandler = getFeedbackReportHandler;
        _importStudentHandler = importStudentHandler;
        _updateProfileValidator = updateProfileValidator;
    }

    /// <summary>
    /// UC-42: Get current school's profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(SchoolProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _getProfileHandler.HandleAsync(new GetSchoolProfileQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// UC-42: Update current school's profile.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(SchoolProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSchoolProfileRequest request, CancellationToken ct)
    {
        var command = new UpdateSchoolProfileCommand(
            _currentUser.UserId,
            request.SchoolName,
            request.LogoURL,
            request.ContactInfo
        );

        var validationResult = await _updateProfileValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var result = await _updateProfileHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// UC-43: Download student import template as .xlsx (proper Excel format).
    /// Uses ClosedXML — locale-independent, preserves leading zeros, full Vietnamese support.
    /// </summary>
    [HttpGet("me/students/import/template")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public IActionResult DownloadImportTemplate()
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Import Template");

        // --- Header row ---
        var headers = new[] { "Student Name", "DOB", "Grade", "Gender", "Parent Phone Number" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#4472C4");
        }

        // --- Sample data rows ---
        // Phone column (E) must be Text type to preserve leading zero
        var phoneCol = ws.Column(5);
        phoneCol.Style.NumberFormat.NumberFormatId = 49; // @ = Text format

        ws.Cell(2, 1).Value = "Nguyễn Văn A";
        ws.Cell(2, 2).Value = "15/03/2015";
        ws.Cell(2, 3).Value = "3A";
        ws.Cell(2, 4).Value = "Nam";
        ws.Cell(2, 5).SetValue("0901234567");

        ws.Cell(3, 1).Value = "Trần Thị B";
        ws.Cell(3, 2).Value = "22/07/2014";
        ws.Cell(3, 3).Value = "4B";
        ws.Cell(3, 4).Value = "Nữ";
        ws.Cell(3, 5).SetValue("0912345678");

        // Auto-fit columns for readability
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileBytes = stream.ToArray();

        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "student_import_template.xlsx");
    }

    /// <summary>
    /// UC-43: Import student data from CSV file.
    /// </summary>
    /// <param name="file">CSV file (UTF-8, max 5MB). Row 1 = header, Row 2+ = data.</param>
    [HttpPost("me/students/import")]
    [ProducesResponseType(typeof(ImportStudentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
    public async Task<IActionResult> ImportStudents(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded.", code = "FILE_REQUIRED" });

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (extension != ".csv")
            return BadRequest(new { error = "Only .csv files are supported.", code = "INVALID_FILE_TYPE" });

        using var stream = file.OpenReadStream();
        var command = new ImportStudentDataCommand(_currentUser.UserId, stream);
        var result = await _importStudentHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }


    /// <summary>
    /// UC-45: View parent orders for the school.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="status">Optional order status filter</param>
    [HttpGet("me/orders")]
    [ProducesResponseType(typeof(SchoolOrderListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        // First get school ID from current user
        var profileResult = await _getProfileHandler.HandleAsync(new GetSchoolProfileQuery(_currentUser.UserId), ct);
        if (!profileResult.IsSuccess)
            return BadRequest(new { error = profileResult.Error, code = profileResult.ErrorCode });

        var query = new GetSchoolOrdersQuery(profileResult.Value!.Id, page, pageSize, status);
        var result = await _getOrdersHandler.HandleAsync(query, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// UC-46: Track pre-order progress for a campaign.
    /// </summary>
    /// <param name="id">Campaign ID</param>
    [HttpGet("me/campaigns/{id:guid}/progress")]
    [ProducesResponseType(typeof(CampaignProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaignProgress(Guid id, CancellationToken ct)
    {
        var profileResult = await _getProfileHandler.HandleAsync(new GetSchoolProfileQuery(_currentUser.UserId), ct);
        if (!profileResult.IsSuccess)
            return BadRequest(new { error = profileResult.Error, code = profileResult.ErrorCode });

        var query = new GetCampaignProgressQuery(profileResult.Value!.Id, id);
        var result = await _getCampaignProgressHandler.HandleAsync(query, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// UC-49: View sales reports.
    /// </summary>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    [HttpGet("me/reports/sales")]
    [ProducesResponseType(typeof(SalesReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var profileResult = await _getProfileHandler.HandleAsync(new GetSchoolProfileQuery(_currentUser.UserId), ct);
        if (!profileResult.IsSuccess)
            return BadRequest(new { error = profileResult.Error, code = profileResult.ErrorCode });

        var query = new GetSalesReportQuery(profileResult.Value!.Id, fromDate, toDate);
        var result = await _getSalesReportHandler.HandleAsync(query, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// UC-50: View feedback reports.
    /// </summary>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    [HttpGet("me/reports/feedback")]
    [ProducesResponseType(typeof(FeedbackReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeedbackReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var profileResult = await _getProfileHandler.HandleAsync(new GetSchoolProfileQuery(_currentUser.UserId), ct);
        if (!profileResult.IsSuccess)
            return BadRequest(new { error = profileResult.Error, code = profileResult.ErrorCode });

        var query = new GetFeedbackReportQuery(profileResult.Value!.Id, fromDate, toDate);
        var result = await _getFeedbackReportHandler.HandleAsync(query, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }
}
