using System.Text;
using ClosedXML.Excel;
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
    /// UC-43: Import student data from a .csv or .xlsx file.
    /// </summary>
    /// <param name="file">.csv or .xlsx file (max 5MB). Row 1 = header, Row 2+ = data. Columns: Student Name, DOB (dd/MM/yyyy), Grade, Gender, Parent Phone Number.</param>
    [HttpPost("me/students/import")]
    [ProducesResponseType(typeof(ImportStudentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB
    public async Task<IActionResult> ImportStudents(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded.", code = "FILE_REQUIRED" });

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (extension != ".csv" && extension != ".xlsx")
            return BadRequest(new { error = "Only .csv and .xlsx files are supported.", code = "INVALID_FILE_TYPE" });

        // Parse rows in the controller (where ClosedXML is available)
        IReadOnlyList<string[]> rows;
        try
        {
            using var stream = file.OpenReadStream();
            rows = extension == ".xlsx"
                ? ParseXlsxRows(stream)
                : await ParseCsvRowsAsync(stream, ct);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Could not read file: {ex.Message}", code = "FILE_READ_ERROR" });
        }

        var command = new ImportStudentDataCommand(_currentUser.UserId, rows);
        var result = await _importStudentHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>Parse all data rows (skip header) from an XLSX stream using ClosedXML.</summary>
    private static IReadOnlyList<string[]> ParseXlsxRows(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var rows = new List<string[]>();
        bool isHeader = true;
        foreach (var row in ws.RowsUsed())
        {
            if (isHeader) { isHeader = false; continue; } // skip header row
            if (row.Cells(1, 5).All(c => c.IsEmpty())) continue; // skip fully empty rows

            var cols = new string[5];
            for (int i = 0; i < 5; i++)
                cols[i] = row.Cell(i + 1).IsEmpty() ? string.Empty : row.Cell(i + 1).GetString().Trim();
            rows.Add(cols);
        }
        return rows;
    }

    /// <summary>Parse all data rows (skip header) from a CSV stream.</summary>
    private static async Task<IReadOnlyList<string[]>> ParseCsvRowsAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = new List<string[]>();
        await reader.ReadLineAsync(ct); // skip header
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (!string.IsNullOrWhiteSpace(line))
                rows.Add(ParseCsvLine(line));
        }
        return rows;
    }

    /// <summary>Simple CSV line parser supporting quoted fields.</summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = false;
                }
                else current.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
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
