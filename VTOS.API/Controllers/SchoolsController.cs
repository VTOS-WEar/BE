using System.Text;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Contracts.Commands;
using VTOS.Application.Features.Contracts.DTOs;
using VTOS.Application.Features.Contracts.Queries;
using VTOS.Application.Features.Schools.Commands;
using VTOS.Application.Features.Schools.DTOs;
using VTOS.Application.Features.Schools.Queries;
using VTOS.Domain.Enums;

namespace VTOS.API.Controllers;

/// <summary>
/// School management APIs (requires School role).
/// UC-42: Maintain School Profile
/// UC-43: Import Student Data
/// UC-44: Publish Pre-order Campaign
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
    private readonly IGetSchoolStudentsQueryHandler _getStudentsHandler;
    private readonly IGetStudentByIdQueryHandler _getStudentByIdHandler;
    private readonly ICreateStudentCommandHandler _createStudentHandler;
    private readonly IUpdateStudentCommandHandler _updateStudentHandler;
    private readonly IDeleteStudentCommandHandler _deleteStudentHandler;
    private readonly IPublishCampaignCommandHandler _publishCampaignHandler;
    private readonly IValidator<UpdateSchoolProfileCommand> _updateProfileValidator;
    private readonly IValidator<PublishCampaignCommand> _publishCampaignValidator;
    // UC 3.9.x handlers
    private readonly IGetCampaignListQueryHandler _getCampaignListHandler;
    private readonly IGetCampaignDetailQueryHandler _getCampaignDetailHandler;
    private readonly IGetCampaignOrderedItemsQueryHandler _getCampaignOrderedItemsHandler;
    private readonly IGetCampaignSelectedSizesQueryHandler _getCampaignSelectedSizesHandler;
    private readonly ILockCampaignCommandHandler _lockCampaignHandler;
    private readonly IGetCampaignSummaryQueryHandler _getCampaignSummaryHandler;
    private readonly IGetCampaignTotalQuantityQueryHandler _getCampaignTotalQuantityHandler;
    private readonly IGenerateProductionOrderCommandHandler _generateProductionOrderHandler;
    private readonly ISendProductionRequestCommandHandler _sendProductionRequestHandler;
    private readonly IConfirmProductionOrderCommandHandler _confirmProductionOrderHandler;
    private readonly IGetProductionComplaintsQueryHandler _getProductionComplaintsHandler;
    private readonly IGetProductionOrderListQueryHandler _getProductionOrderListHandler;
    private readonly IGetProductionOrderDetailQueryHandler _getProductionOrderDetailHandler;
    private readonly IGetProductionOrderItemsQueryHandler _getProductionOrderItemsHandler;
    private readonly IGetProductionOrderQuantityQueryHandler _getProductionOrderQuantityHandler;
    private readonly IGetDeliveryDeadlineQueryHandler _getDeliveryDeadlineHandler;
    private readonly IProcessProductionOrderCommandHandler _processProductionOrderHandler;
    private readonly IRejectProductionOrderCommandHandler _rejectProductionOrderHandler;
    private readonly IImageUploadService _imageUploadService;
    private readonly IGetSchoolGradesQueryHandler _getGradesHandler;
    private readonly IGetImportHistoryQueryHandler _getImportHistoryHandler;
    private readonly IGetSchoolOutfitsQueryHandler _getOutfitsHandler;
    private readonly ICreateOutfitCommandHandler _createOutfitHandler;
    private readonly IUpdateOutfitCommandHandler _updateOutfitHandler;
    private readonly IDeleteOutfitCommandHandler _deleteOutfitHandler;
    private readonly IGetProvidersQueryHandler _getProvidersHandler;
    private readonly IApproveRefundCommandHandler _approveRefundHandler;
    private readonly IGetOutfitVariantsQueryHandler _getVariantsHandler;
    private readonly ICreateVariantCommandHandler _createVariantHandler;
    private readonly IUpdateVariantCommandHandler _updateVariantHandler;
    private readonly IDeleteVariantCommandHandler _deleteVariantHandler;
    private readonly ICreateWithdrawalRequestCommandHandler _createWithdrawalHandler;
    private readonly IUpdateSchoolBankAccountCommandHandler _updateBankAccountHandler;
    private readonly IGetSchoolRefundsQueryHandler _getSchoolRefundsHandler;
    private readonly ICreateContractCommandHandler _createContractHandler;
    private readonly IGetContractsQueryHandler _getContractsHandler;
    private readonly IGetContractDetailQueryHandler _getContractDetailHandler;
    // Phase 4 — Delivery & Distribution
    private readonly IConfirmDeliveryCommandHandler _confirmDeliveryHandler;
    private readonly IGetVerifyQuantityQueryHandler _getVerifyQuantityHandler;
    private readonly IReportDefectCommandHandler _reportDefectHandler;
    private readonly IDistributeOrdersCommandHandler _distributeOrdersHandler;
    private readonly IGetDistributionStatusQueryHandler _getDistributionStatusHandler;
    private readonly IGetSchoolDeliveryStatusQueryHandler _getSchoolDeliveryStatusHandler;
    // Phase 5 — Complaints
    private readonly IGetComplaintDetailQueryHandler _getComplaintDetailHandler;
    private readonly ICloseComplaintCommandHandler _closeComplaintHandler;
    // Phase 5 — Distribution Scheduling
    private readonly Application.Features.Distribution.ICreateDistributionScheduleHandler _createScheduleHandler;
    private readonly Application.Features.Distribution.IGetDistributionSchedulesHandler _getSchedulesHandler;
    private readonly Application.Features.Distribution.IUpdateDistributionScheduleHandler _updateScheduleHandler;
    private readonly IGetContractedProvidersForOutfitsQueryHandler _getContractedProvidersHandler;

    public SchoolsController(
        ICurrentUserService currentUser,
        IGetSchoolProfileQueryHandler getProfileHandler,
        IUpdateSchoolProfileCommandHandler updateProfileHandler,
        IGetSchoolOrdersQueryHandler getOrdersHandler,
        IGetCampaignProgressQueryHandler getCampaignProgressHandler,
        IGetSalesReportQueryHandler getSalesReportHandler,
        IGetFeedbackReportQueryHandler getFeedbackReportHandler,
        IImportStudentDataCommandHandler importStudentHandler,
        IGetSchoolStudentsQueryHandler getStudentsHandler,
        IGetStudentByIdQueryHandler getStudentByIdHandler,
        ICreateStudentCommandHandler createStudentHandler,
        IUpdateStudentCommandHandler updateStudentHandler,
        IDeleteStudentCommandHandler deleteStudentHandler,
        IPublishCampaignCommandHandler publishCampaignHandler,
        IValidator<UpdateSchoolProfileCommand> updateProfileValidator,
        IValidator<PublishCampaignCommand> publishCampaignValidator,
        // UC 3.9.x
        IGetCampaignListQueryHandler getCampaignListHandler,
        IGetCampaignDetailQueryHandler getCampaignDetailHandler,
        IGetCampaignOrderedItemsQueryHandler getCampaignOrderedItemsHandler,
        IGetCampaignSelectedSizesQueryHandler getCampaignSelectedSizesHandler,
        ILockCampaignCommandHandler lockCampaignHandler,
        IGetCampaignSummaryQueryHandler getCampaignSummaryHandler,
        IGetCampaignTotalQuantityQueryHandler getCampaignTotalQuantityHandler,
        IGenerateProductionOrderCommandHandler generateProductionOrderHandler,
        ISendProductionRequestCommandHandler sendProductionRequestHandler,
        IConfirmProductionOrderCommandHandler confirmProductionOrderHandler,
        IGetProductionComplaintsQueryHandler getProductionComplaintsHandler,
        IGetProductionOrderListQueryHandler getProductionOrderListHandler,
        IGetProductionOrderDetailQueryHandler getProductionOrderDetailHandler,
        IGetProductionOrderItemsQueryHandler getProductionOrderItemsHandler,
        IGetProductionOrderQuantityQueryHandler getProductionOrderQuantityHandler,
        IGetDeliveryDeadlineQueryHandler getDeliveryDeadlineHandler,
        IProcessProductionOrderCommandHandler processProductionOrderHandler,
        IRejectProductionOrderCommandHandler rejectProductionOrderHandler,
        IImageUploadService imageUploadService,
        IGetSchoolGradesQueryHandler getGradesHandler,
        IGetImportHistoryQueryHandler getImportHistoryHandler,
        IGetSchoolOutfitsQueryHandler getOutfitsHandler,
        ICreateOutfitCommandHandler createOutfitHandler,
        IUpdateOutfitCommandHandler updateOutfitHandler,
        IDeleteOutfitCommandHandler deleteOutfitHandler,
        IGetProvidersQueryHandler getProvidersHandler,
        IApproveRefundCommandHandler approveRefundHandler,
        IGetOutfitVariantsQueryHandler getVariantsHandler,
        ICreateVariantCommandHandler createVariantHandler,
        IUpdateVariantCommandHandler updateVariantHandler,
        IDeleteVariantCommandHandler deleteVariantHandler,
        ICreateWithdrawalRequestCommandHandler createWithdrawalHandler,
        IUpdateSchoolBankAccountCommandHandler updateBankAccountHandler,
        IGetSchoolRefundsQueryHandler getSchoolRefundsHandler,
        ICreateContractCommandHandler createContractHandler,
        IGetContractsQueryHandler getContractsHandler,
        IGetContractDetailQueryHandler getContractDetailHandler,
        // Phase 4
        IConfirmDeliveryCommandHandler confirmDeliveryHandler,
        IGetVerifyQuantityQueryHandler getVerifyQuantityHandler,
        IReportDefectCommandHandler reportDefectHandler,
        IDistributeOrdersCommandHandler distributeOrdersHandler,
        IGetDistributionStatusQueryHandler getDistributionStatusHandler,
        IGetSchoolDeliveryStatusQueryHandler getSchoolDeliveryStatusHandler,
        // Phase 5
        IGetComplaintDetailQueryHandler getComplaintDetailHandler,
        ICloseComplaintCommandHandler closeComplaintHandler,
        Application.Features.Distribution.ICreateDistributionScheduleHandler createScheduleHandler,
        Application.Features.Distribution.IGetDistributionSchedulesHandler getSchedulesHandler,
        Application.Features.Distribution.IUpdateDistributionScheduleHandler updateScheduleHandler,
        IGetContractedProvidersForOutfitsQueryHandler getContractedProvidersHandler)
    {
        _currentUser = currentUser;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
        _getOrdersHandler = getOrdersHandler;
        _getCampaignProgressHandler = getCampaignProgressHandler;
        _getSalesReportHandler = getSalesReportHandler;
        _getFeedbackReportHandler = getFeedbackReportHandler;
        _importStudentHandler = importStudentHandler;
        _getStudentsHandler = getStudentsHandler;
        _getStudentByIdHandler = getStudentByIdHandler;
        _createStudentHandler = createStudentHandler;
        _updateStudentHandler = updateStudentHandler;
        _deleteStudentHandler = deleteStudentHandler;
        _publishCampaignHandler = publishCampaignHandler;
        _updateProfileValidator = updateProfileValidator;
        _publishCampaignValidator = publishCampaignValidator;
        _getCampaignListHandler = getCampaignListHandler;
        _getCampaignDetailHandler = getCampaignDetailHandler;
        _getCampaignOrderedItemsHandler = getCampaignOrderedItemsHandler;
        _getCampaignSelectedSizesHandler = getCampaignSelectedSizesHandler;
        _lockCampaignHandler = lockCampaignHandler;
        _getCampaignSummaryHandler = getCampaignSummaryHandler;
        _getCampaignTotalQuantityHandler = getCampaignTotalQuantityHandler;
        _generateProductionOrderHandler = generateProductionOrderHandler;
        _sendProductionRequestHandler = sendProductionRequestHandler;
        _confirmProductionOrderHandler = confirmProductionOrderHandler;
        _getProductionComplaintsHandler = getProductionComplaintsHandler;
        _getProductionOrderListHandler = getProductionOrderListHandler;
        _getProductionOrderDetailHandler = getProductionOrderDetailHandler;
        _getProductionOrderItemsHandler = getProductionOrderItemsHandler;
        _getProductionOrderQuantityHandler = getProductionOrderQuantityHandler;
        _getDeliveryDeadlineHandler = getDeliveryDeadlineHandler;
        _processProductionOrderHandler = processProductionOrderHandler;
        _rejectProductionOrderHandler = rejectProductionOrderHandler;
        _imageUploadService = imageUploadService;
        _getGradesHandler = getGradesHandler;
        _getImportHistoryHandler = getImportHistoryHandler;
        _getOutfitsHandler = getOutfitsHandler;
        _createOutfitHandler = createOutfitHandler;
        _updateOutfitHandler = updateOutfitHandler;
        _deleteOutfitHandler = deleteOutfitHandler;
        _getProvidersHandler = getProvidersHandler;
        _approveRefundHandler = approveRefundHandler;
        _getVariantsHandler = getVariantsHandler;
        _createVariantHandler = createVariantHandler;
        _updateVariantHandler = updateVariantHandler;
        _deleteVariantHandler = deleteVariantHandler;
        _createWithdrawalHandler = createWithdrawalHandler;
        _updateBankAccountHandler = updateBankAccountHandler;
        _getSchoolRefundsHandler = getSchoolRefundsHandler;
        _createContractHandler = createContractHandler;
        _getContractsHandler = getContractsHandler;
        _getContractDetailHandler = getContractDetailHandler;
        _confirmDeliveryHandler = confirmDeliveryHandler;
        _getVerifyQuantityHandler = getVerifyQuantityHandler;
        _reportDefectHandler = reportDefectHandler;
        _distributeOrdersHandler = distributeOrdersHandler;
        _getDistributionStatusHandler = getDistributionStatusHandler;
        _getSchoolDeliveryStatusHandler = getSchoolDeliveryStatusHandler;
        _getComplaintDetailHandler = getComplaintDetailHandler;
        _closeComplaintHandler = closeComplaintHandler;
        _createScheduleHandler = createScheduleHandler;
        _getSchedulesHandler = getSchedulesHandler;
        _updateScheduleHandler = updateScheduleHandler;
        _getContractedProvidersHandler = getContractedProvidersHandler;
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
            request.ContactInfo,
            request.Level
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
    /// UC-42: Upload school logo image.
    /// Uploads to MinIO and saves the returned URL to LogoURL.
    /// </summary>
    [HttpPost("me/logo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded.", code = "FILE_REQUIRED" });

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { error = "File too large. Max 2 MB.", code = "FILE_TOO_LARGE" });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { error = "Invalid image type. Use JPEG, PNG, WebP, or GIF.", code = "INVALID_TYPE" });

        // Upload to MinIO
        using var stream = file.OpenReadStream();
        var imageUrl = await _imageUploadService.UploadAsync(stream, file.FileName, "schools", ct);

        // Save URL to school profile
        var command = new UpdateSchoolProfileCommand(
            _currentUser.UserId,
            null, // schoolName
            imageUrl, // logoURL
            null, // contactInfo
            null  // level
        );
        var result = await _updateProfileHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(new { logoURL = imageUrl });
    }

    /// <summary>
    /// Get students for the current school with optional filters.
    /// </summary>
    [HttpGet("me/students")]
    [ProducesResponseType(typeof(StudentListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? grade = null,
        [FromQuery] string? measurementStatus = null,
        [FromQuery] string? parentLinkStatus = null,
        CancellationToken ct = default)
    {
        var query = new GetSchoolStudentsQuery(_currentUser.UserId, page, pageSize, search, grade, measurementStatus, parentLinkStatus);
        var result = await _getStudentsHandler.HandleAsync(query, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new student under the current school.
    /// </summary>
    [HttpPost("me/students")]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStudent([FromBody] CreateOrUpdateStudentRequest request, CancellationToken ct)
    {
        var command = new CreateStudentCommand(
            _currentUser.UserId, request.FullName, request.DateOfBirth,
            request.Grade, request.Gender, request.ParentPhone, request.HeightCm, request.WeightKg);
        var result = await _createStudentHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Get a single student detail by ID.
    /// </summary>
    [HttpGet("me/students/{id:guid}")]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById(Guid id, CancellationToken ct)
    {
        var query = new GetStudentByIdQuery(_currentUser.UserId, id);
        var result = await _getStudentByIdHandler.HandleAsync(query, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Update a student by ID.
    /// </summary>
    [HttpPut("me/students/{id:guid}")]
    [ProducesResponseType(typeof(StudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] CreateOrUpdateStudentRequest request, CancellationToken ct)
    {
        var command = new UpdateStudentCommand(
            _currentUser.UserId, id, request.FullName, request.DateOfBirth,
            request.Grade, request.Gender, request.HeightCm, request.WeightKg,
            request.ParentPhone);
        var result = await _updateStudentHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete (soft) a student by ID.
    /// </summary>
    [HttpDelete("me/students/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudent(Guid id, CancellationToken ct)
    {
        var command = new DeleteStudentCommand(_currentUser.UserId, id);
        var result = await _deleteStudentHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
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

        var command = new ImportStudentDataCommand(_currentUser.UserId, rows, file.FileName);
        var result = await _importStudentHandler.HandleAsync(command, ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get distinct grades for the current school's students.
    /// Used to populate a grade combobox on the frontend.
    /// </summary>
    [HttpGet("me/students/grades")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentGrades(CancellationToken ct)
    {
        var result = await _getGradesHandler.HandleAsync(new GetSchoolGradesQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Get import history (recent batches) for the current school.
    /// </summary>
    [HttpGet("me/students/import/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ImportBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImportHistory([FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _getImportHistoryHandler.HandleAsync(new GetImportHistoryQuery(_currentUser.UserId, limit), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ── Outfit CRUD endpoints ──

    /// <summary>
    /// Get all outfits for the current school.
    /// </summary>
    [HttpGet("me/outfits")]
    [ProducesResponseType(typeof(OutfitListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutfits([FromQuery] bool? isAvailable, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var result = await _getOutfitsHandler.HandleAsync(
            new GetSchoolOutfitsQuery(userId, isAvailable), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new outfit for the current school.
    /// </summary>
    [HttpPost("me/outfits")]
    [ProducesResponseType(typeof(OutfitDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOutfit([FromBody] CreateOutfitRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var result = await _createOutfitHandler.HandleAsync(
            new CreateOutfitCommand(
                userId,
                request.OutfitName,
                request.Description,
                request.Price,
                request.OutfitType,
                request.MainImageURL,
                request.SizeChartID,
                request.IsCustomizable
            ), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Delete an outfit by ID.
    /// </summary>
    [HttpDelete("me/outfits/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOutfit(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var result = await _deleteOutfitHandler.HandleAsync(
            new DeleteOutfitCommand(userId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return NoContent();
    }

    /// <summary>
    /// Update an existing outfit (partial update).
    /// </summary>
    [HttpPut("me/outfits/{id:guid}")]
    [ProducesResponseType(typeof(OutfitDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOutfit(Guid id, [FromBody] UpdateOutfitRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var result = await _updateOutfitHandler.HandleAsync(
            new UpdateOutfitCommand(
                userId, id,
                request.OutfitName,
                request.Description,
                request.Price,
                request.OutfitType,
                request.MainImageURL,
                request.SizeChartID,
                request.IsAvailable,
                request.IsCustomizable
            ), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Upload an image for an outfit (returns the hosted URL).
    /// </summary>
    [HttpPost("me/outfits/upload-image")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadOutfitImage(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded.", code = "FILE_REQUIRED" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "File too large. Max 5 MB.", code = "FILE_TOO_LARGE" });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { error = "Invalid image type. Use JPEG, PNG, or WebP.", code = "INVALID_TYPE" });

        using var stream = file.OpenReadStream();
        var imageUrl = await _imageUploadService.UploadAsync(stream, file.FileName, "outfits", ct);

        return Ok(new { imageUrl });
    }

    // ── Product Variant (Size) CRUD endpoints ──

    /// <summary>
    /// Get all product variants (sizes) for an outfit.
    /// </summary>
    [HttpGet("me/outfits/{outfitId:guid}/variants")]
    [ProducesResponseType(typeof(List<ProductVariantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutfitVariants(Guid outfitId, CancellationToken ct)
    {
        var result = await _getVariantsHandler.HandleAsync(
            new GetOutfitVariantsQuery(_currentUser.UserId, outfitId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new product variant (size) for an outfit.
    /// </summary>
    [HttpPost("me/outfits/{outfitId:guid}/variants")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVariant(Guid outfitId, [FromBody] CreateVariantRequest request, CancellationToken ct)
    {
        var result = await _createVariantHandler.HandleAsync(
            new CreateVariantCommand(
                _currentUser.UserId,
                outfitId,
                request.Size,
                request.ColorVariant,
                request.MaterialType,
                request.SKUCode
            ), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Update a product variant (size) for an outfit (partial update).
    /// </summary>
    [HttpPut("me/outfits/{outfitId:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVariant(Guid outfitId, Guid variantId, [FromBody] UpdateVariantRequest request, CancellationToken ct)
    {
        var result = await _updateVariantHandler.HandleAsync(
            new UpdateVariantCommand(
                _currentUser.UserId,
                outfitId,
                variantId,
                request.Size,
                request.ColorVariant,
                request.MaterialType,
                request.SKUCode
            ), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a product variant (size) for an outfit.
    /// </summary>
    [HttpDelete("me/outfits/{outfitId:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVariant(Guid outfitId, Guid variantId, CancellationToken ct)
    {
        var result = await _deleteVariantHandler.HandleAsync(
            new DeleteVariantCommand(_currentUser.UserId, outfitId, variantId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return NoContent();
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
    // ── Provider listing endpoint ──

    /// <summary>
    /// Get list of available providers (for campaign outfit assignment).
    /// </summary>
    [HttpGet("me/providers")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        var result = await _getProvidersHandler.HandleAsync(new GetProvidersQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// Get contracted providers grouped by outfit for campaign creation.
    /// Only returns providers with Approved contracts covering each outfit.
    /// </summary>
    [HttpGet("me/contracts/providers-for-outfits")]
    [ProducesResponseType(typeof(GetContractedProvidersForOutfitsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetContractedProvidersForOutfits(CancellationToken ct)
    {
        var result = await _getContractedProvidersHandler.HandleAsync(
            new GetContractedProvidersForOutfitsQuery(_currentUser.UserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>
    /// UC-44: Publish (or save as draft) a pre-order campaign.
    /// </summary>
    [HttpPost("me/campaigns")]
    [ProducesResponseType(typeof(PublishCampaignResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishCampaign([FromBody] PublishCampaignRequest request, CancellationToken ct)
    {
        var command = new PublishCampaignCommand(
            _currentUser.UserId,
            request.CampaignName,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Outfits.Select(o => new CampaignOutfitInput(
                o.OutfitId,
                o.ProviderId,
                o.CampaignPrice,
                o.MaxQuantity
            )).ToList(),
            request.SaveAsDraft
        );

        var validationResult = await _publishCampaignValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var result = await _publishCampaignHandler.HandleAsync(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return StatusCode(StatusCodes.Status201Created, result.Value);
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
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        // First get school ID from current user
        var profileResult = await _getProfileHandler.HandleAsync(new GetSchoolProfileQuery(_currentUser.UserId), ct);
        if (!profileResult.IsSuccess)
            return BadRequest(new { error = profileResult.Error, code = profileResult.ErrorCode });

        var query = new GetSchoolOrdersQuery(profileResult.Value!.Id, page, pageSize, status, search);
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

    // ──────────────────────────────────────────────────────────────────────────
    // UC 3.9.x — Pre-Order & Production Management
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>UC 3.9.2 — View campaign list for the school.</summary>
    [HttpGet("me/campaigns")]
    [ProducesResponseType(typeof(GetCampaignListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaignList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getCampaignListHandler.HandleAsync(
            new GetCampaignListQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.3 — View campaign detail.</summary>
    [HttpGet("me/campaigns/{id:guid}")]
    [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaignDetail(Guid id, CancellationToken ct)
    {
        var result = await _getCampaignDetailHandler.HandleAsync(new GetCampaignDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.4 — View ordered items for a campaign.</summary>
    [HttpGet("me/campaigns/{id:guid}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignOrderedItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaignOrderedItems(Guid id, CancellationToken ct)
    {
        var result = await _getCampaignOrderedItemsHandler.HandleAsync(new GetCampaignOrderedItemsQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.5a — View selected sizes for a campaign.</summary>
    [HttpGet("me/campaigns/{id:guid}/sizes")]
    [ProducesResponseType(typeof(IReadOnlyList<CampaignOutfitSizesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaignSelectedSizes(Guid id, CancellationToken ct)
    {
        var result = await _getCampaignSelectedSizesHandler.HandleAsync(new GetCampaignSelectedSizesQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.5b — Lock a pre-order campaign (no more orders accepted).</summary>
    [HttpPost("me/campaigns/{id:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LockCampaign(Guid id, CancellationToken ct)
    {
        var result = await _lockCampaignHandler.HandleAsync(new LockCampaignCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>UC 3.9.6 — View pre-order summary for a campaign.</summary>
    [HttpGet("me/campaigns/{id:guid}/summary")]
    [ProducesResponseType(typeof(CampaignSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaignSummary(Guid id, CancellationToken ct)
    {
        var result = await _getCampaignSummaryHandler.HandleAsync(new GetCampaignSummaryQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.7 — Calculate total quantity for a campaign.</summary>
    [HttpGet("me/campaigns/{id:guid}/quantity")]
    [ProducesResponseType(typeof(CampaignTotalQuantityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCampaignTotalQuantity(Guid id, CancellationToken ct)
    {
        var result = await _getCampaignTotalQuantityHandler.HandleAsync(new GetCampaignTotalQuantityQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.8 — Generate a production order from a locked campaign.</summary>
    [HttpPost("me/campaigns/{id:guid}/production-order")]
    [ProducesResponseType(typeof(GenerateProductionOrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateProductionOrder(Guid id, [FromBody] GenerateProductionOrderRequest request, CancellationToken ct)
    {
        var result = await _generateProductionOrderHandler.HandleAsync(
            new GenerateProductionOrderCommand(_currentUser.UserId, id, request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>UC 3.9.9 — Send production request to provider.</summary>
    [HttpPost("me/batches/{id:guid}/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendProductionRequest(Guid id, CancellationToken ct)
    {
        var result = await _sendProductionRequestHandler.HandleAsync(new SendProductionRequestCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>UC 3.9.10 — Confirm a production order.</summary>
    [HttpPost("me/campaigns/{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmProductionOrder(Guid id, CancellationToken ct)
    {
        var result = await _confirmProductionOrderHandler.HandleAsync(new ConfirmProductionOrderCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>UC 3.9.11 — View production complaints for the school.</summary>
    [HttpGet("me/complaints")]
    [ProducesResponseType(typeof(GetProductionComplaintsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductionComplaints(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getProductionComplaintsHandler.HandleAsync(
            new GetProductionComplaintsQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.10.5 — View complaint detail.</summary>
    [HttpGet("me/complaints/{id:guid}")]
    [ProducesResponseType(typeof(ComplaintDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComplaintDetail(Guid id, CancellationToken ct)
    {
        var result = await _getComplaintDetailHandler.HandleAsync(
            new GetComplaintDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.10.7 — Close a resolved complaint.</summary>
    [HttpPut("me/complaints/{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseComplaint(Guid id, CancellationToken ct)
    {
        var result = await _closeComplaintHandler.HandleAsync(
            new CloseComplaintCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>UC 3.9.12 — View production order list.</summary>
    [HttpGet("me/production-orders")]
    [ProducesResponseType(typeof(GetProductionOrderListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductionOrderList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getProductionOrderListHandler.HandleAsync(
            new GetProductionOrderListQuery(_currentUser.UserId, page, pageSize, status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.13 — View production order detail.</summary>
    [HttpGet("me/production-orders/{id:guid}")]
    [ProducesResponseType(typeof(ProductionOrderDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductionOrderDetail(Guid id, CancellationToken ct)
    {
        var result = await _getProductionOrderDetailHandler.HandleAsync(new GetProductionOrderDetailQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.14 — View items in a production order.</summary>
    [HttpGet("me/production-orders/{id:guid}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductionBatchItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductionOrderItems(Guid id, CancellationToken ct)
    {
        var result = await _getProductionOrderItemsHandler.HandleAsync(new GetProductionOrderItemsQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.15 — View required quantity for a production order.</summary>
    [HttpGet("me/production-orders/{id:guid}/quantity")]
    [ProducesResponseType(typeof(ProductionOrderQuantityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductionOrderQuantity(Guid id, CancellationToken ct)
    {
        var result = await _getProductionOrderQuantityHandler.HandleAsync(new GetProductionOrderQuantityQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.16 — View delivery deadline for a production order.</summary>
    [HttpGet("me/production-orders/{id:guid}/deadline")]
    [ProducesResponseType(typeof(DeliveryDeadlineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryDeadline(Guid id, CancellationToken ct)
    {
        var result = await _getDeliveryDeadlineHandler.HandleAsync(new GetDeliveryDeadlineQuery(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>UC 3.9.17 — Process (mark InProduction) a confirmed production order.</summary>
    [HttpPost("me/production-orders/{id:guid}/process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessProductionOrder(Guid id, CancellationToken ct)
    {
        var result = await _processProductionOrderHandler.HandleAsync(new ProcessProductionOrderCommand(_currentUser.UserId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>UC 3.9.18 — Reject a production order with a reason.</summary>
    [HttpPost("me/production-orders/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectProductionOrder(
        Guid id,
        [FromBody] RejectProductionOrderRequest request,
        CancellationToken ct)
    {
        var result = await _rejectProductionOrderHandler.HandleAsync(
            new RejectProductionOrderCommand(_currentUser.UserId, id, request.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>
    /// Approve a refund request: deduct school wallet, payout to parent bank account, update statuses.
    /// </summary>
    [HttpPost("me/refunds/{refundId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRefund(
        Guid refundId,
        CancellationToken ct)
    {
        var result = await _approveRefundHandler.HandleAsync(
            new ApproveRefundCommand(_currentUser.UserId, refundId), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode is "REFUND_NOT_FOUND" or "SCHOOL_NOT_FOUND" or "USER_NOT_FOUND"
                ? NotFound(new { error = result.Error, code = result.ErrorCode })
                : BadRequest(new { error = result.Error, code = result.ErrorCode });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get refund requests for the current school.
    /// </summary>
    [HttpGet("me/refunds")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRefunds(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _getSchoolRefundsHandler.HandleAsync(
            new GetSchoolRefundsQuery(_currentUser.UserId, page, pageSize, status), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a withdrawal request from school wallet.
    /// </summary>
    [HttpPost("me/wallet/withdrawals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWithdrawalRequest(
        [FromBody] CreateWithdrawalRequest request,
        CancellationToken ct)
    {
        var result = await _createWithdrawalHandler.HandleAsync(
            new CreateWithdrawalRequestCommand(_currentUser.UserId, request.Amount), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    /// <summary>
    /// Update school bank account information on the wallet.
    /// </summary>
    [HttpPut("me/wallet/bank-account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateBankAccount(
        [FromBody] UpdateSchoolBankAccountRequest request,
        CancellationToken ct)
    {
        var result = await _updateBankAccountHandler.HandleAsync(
            new UpdateSchoolBankAccountCommand(
                _currentUser.UserId,
                request.BankCode,
                request.BankName,
                request.BankAccountNumber,
                request.BankAccountName), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });

        return Ok(result.Value);
    }

    // ──────── Contract Management (Phase 2) ────────

    /// <summary>Create a new contract with a provider (items: outfit, price/unit, qty range).</summary>
    [HttpPost("me/contracts")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request, CancellationToken ct)
    {
        var result = await _createContractHandler.HandleAsync(
            new CreateContractCommand(_currentUser.UserId, request), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return CreatedAtAction(nameof(GetContractDetail), new { id = result.Value!.ContractId }, result.Value);
    }

    /// <summary>List contracts for the current school, optionally filtered by status.</summary>
    [HttpGet("me/contracts")]
    [ProducesResponseType(typeof(List<ContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts([FromQuery] string? status, CancellationToken ct)
    {
        var result = await _getContractsHandler.HandleAsync(
            new GetContractsQuery(_currentUser.UserId, "School", status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Get contract detail by ID (school-scoped).</summary>
    [HttpGet("me/contracts/{id}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractDetail(Guid id, CancellationToken ct)
    {
        var result = await _getContractDetailHandler.HandleAsync(
            new GetContractDetailQuery(_currentUser.UserId, "School", id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ──────── Delivery & Distribution (Phase 4) ────────

    /// <summary>View delivery status for a production order (school side).</summary>
    [HttpGet("me/production-orders/{batchId:guid}/delivery-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDeliveryStatus(Guid batchId, CancellationToken ct)
    {
        var result = await _getSchoolDeliveryStatusHandler.HandleAsync(
            new GetSchoolDeliveryStatusQuery(_currentUser.UserId, batchId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Confirm a specific delivery record with accepted/defective quantities.</summary>
    [HttpPut("me/production-orders/{batchId:guid}/confirm-delivery/{deliveryId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmDelivery(Guid batchId, Guid deliveryId, [FromBody] ConfirmDeliveryRequest request, CancellationToken ct)
    {
        var result = await _confirmDeliveryHandler.HandleAsync(
            new ConfirmDeliveryCommand(_currentUser.UserId, batchId, deliveryId, request.AcceptedQuantity, request.DefectiveQuantity, request.DefectNote), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }

    /// <summary>Verify delivered quantity vs expected per outfit/size.</summary>
    [HttpGet("me/production-orders/{batchId:guid}/verify-quantity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyQuantity(Guid batchId, CancellationToken ct)
    {
        var result = await _getVerifyQuantityHandler.HandleAsync(
            new GetVerifyQuantityQuery(_currentUser.UserId, batchId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Report defective uniforms (creates a Complaint).</summary>
    [HttpPost("me/production-orders/{batchId:guid}/defect-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReportDefect(Guid batchId, [FromBody] ReportDefectRequest request, CancellationToken ct)
    {
        var result = await _reportDefectHandler.HandleAsync(
            new ReportDefectCommand(_currentUser.UserId, batchId, request.Title, request.Description, request.ProofImageUrls), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { complaintId = result.Value });
    }

    /// <summary>Distribute uniforms to parent orders (AtSchool or AtHome).</summary>
    [HttpPost("me/production-orders/{batchId:guid}/distribute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DistributeOrders(Guid batchId, [FromBody] DistributeOrdersRequest request, CancellationToken ct)
    {
        var result = await _distributeOrdersHandler.HandleAsync(
            new DistributeOrdersCommand(_currentUser.UserId, batchId, request.OrderIds, request.ShippingCompany, request.TrackingCode, request.ProofImageUrl, request.Note), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>View distribution status for a production order's campaign orders.</summary>
    [HttpGet("me/production-orders/{batchId:guid}/distribution")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDistributionStatus(Guid batchId, CancellationToken ct)
    {
        var result = await _getDistributionStatusHandler.HandleAsync(
            new GetDistributionStatusQuery(_currentUser.UserId, batchId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    // ──────── Distribution Scheduling (Phase 5) ────────

    /// <summary>Create a distribution schedule for a batch.</summary>
    [HttpPost("me/production-orders/{batchId:guid}/schedules")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDistributionSchedule(Guid batchId, [FromBody] CreateScheduleRequest req, CancellationToken ct)
    {
        var result = await _createScheduleHandler.HandleAsync(
            new Application.Features.Distribution.CreateDistributionScheduleCommand(
                _currentUser.UserId, batchId, req.ScheduledDate, req.Method, req.TimeSlot, req.Note), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return StatusCode(StatusCodes.Status201Created, new { scheduleId = result.Value });
    }

    /// <summary>Get distribution schedules for a batch.</summary>
    [HttpGet("me/production-orders/{batchId:guid}/schedules")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistributionSchedules(Guid batchId, CancellationToken ct)
    {
        var result = await _getSchedulesHandler.HandleAsync(_currentUser.UserId, batchId, ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(result.Value);
    }

    /// <summary>Update a distribution schedule.</summary>
    [HttpPut("me/production-orders/schedules/{scheduleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDistributionSchedule(Guid scheduleId, [FromBody] UpdateScheduleRequest req, CancellationToken ct)
    {
        var result = await _updateScheduleHandler.HandleAsync(_currentUser.UserId,
            new Application.Features.Distribution.UpdateDistributionScheduleCommand(
                scheduleId, req.ScheduledDate, req.Method, req.TimeSlot, req.Note, req.Status), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error, code = result.ErrorCode });
        return Ok(new { message = result.Value });
    }
}

/// <summary>Request body for creating a withdrawal request.</summary>
public record CreateWithdrawalRequest(decimal Amount);

/// <summary>Request body for updating school bank account (partial update).</summary>
public record UpdateSchoolBankAccountRequest(
    string? BankCode = null,
    string? BankName = null,
    string? BankAccountNumber = null,
    string? BankAccountName = null);

/// <summary>Request body for UC 3.9.18 Reject Production Order.</summary>
public record RejectProductionOrderRequest(string Reason);

/// <summary>Request body for confirming delivery.</summary>
public record ConfirmDeliveryRequest(int AcceptedQuantity, int? DefectiveQuantity, string? DefectNote);

/// <summary>Request body for reporting defective uniforms.</summary>
public record ReportDefectRequest(string Title, string Description, List<string>? ProofImageUrls);

/// <summary>Request body for creating a distribution schedule.</summary>
public record CreateScheduleRequest(DateTime ScheduledDate, string Method, string TimeSlot, string? Note);

/// <summary>Request body for updating a distribution schedule.</summary>
public record UpdateScheduleRequest(DateTime? ScheduledDate, string? Method, string? TimeSlot, string? Note, string? Status);

/// <summary>Request body for distributing orders.</summary>
public record DistributeOrdersRequest(
    List<Guid> OrderIds,
    string? ShippingCompany,
    string? TrackingCode,
    string? ProofImageUrl,
    string? Note);

/// <summary>Request body for creating an outfit.</summary>
public record CreateOutfitRequest(
    string OutfitName,
    string? Description,
    decimal Price,
    OutfitType OutfitType,
    string? MainImageURL,
    Guid? SizeChartID,
    bool IsCustomizable
);

/// <summary>Request body for updating an outfit (all fields optional).</summary>
public record UpdateOutfitRequest(
    string? OutfitName = null,
    string? Description = null,
    decimal? Price = null,
    OutfitType? OutfitType = null,
    string? MainImageURL = null,
    Guid? SizeChartID = null,
    bool? IsAvailable = null,
    bool? IsCustomizable = null
);

