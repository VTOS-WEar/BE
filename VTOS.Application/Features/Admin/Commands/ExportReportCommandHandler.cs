using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;

namespace VTOS.Application.Features.Admin.Commands;

public class ExportReportCommandHandler : IExportReportCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ExportReportCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<byte[]>> HandleAsync(
        ExportReportCommand command,
        CancellationToken cancellationToken)
    {
        var validFormats = new[] { "CSV", "EXCEL" };
        if (!validFormats.Contains(command.ExportFormat))
            return Result<byte[]>.Failure("Invalid export format", "INVALID_FORMAT");

        try
        {
            var data = command.ReportType.ToLower() switch
            {
                "order" => await ExportOrderReport(command, cancellationToken),
                "revenue" => await ExportRevenueReport(command, cancellationToken),
                "schoolperformance" => await ExportSchoolPerformanceReport(command, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown report type: {command.ReportType}")
            };

            return Result<byte[]>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure($"Export failed: {ex.Message}", "EXPORT_ERROR");
        }
    }

    private async Task<byte[]> ExportOrderReport(ExportReportCommand command, CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders.AsQueryable();

        if (command.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= command.DateFrom.Value);
        if (command.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= command.DateTo.Value);

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        return command.ExportFormat.ToUpper() switch
        {
            "CSV" => ExportOrdersToCsv(orders),
            "EXCEL" => ExportOrdersToExcel(orders),
            _ => throw new InvalidOperationException("Unknown format")
        };
    }

    private static byte[] ExportOrdersToCsv(List<Order> orders)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Order ID,Status,Amount,Created Date");

        foreach (var order in orders)
        {
            csv.AppendLine($"\"{order.Id}\",\"{order.OrderStatus}\",{order.TotalAmount:F2},{order.CreatedAt:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static byte[] ExportOrdersToExcel(List<Order> orders)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Orders");

        worksheet.Cell("A1").Value = "Order ID";
        worksheet.Cell("B1").Value = "Status";
        worksheet.Cell("C1").Value = "Amount";
        worksheet.Cell("D1").Value = "Created Date";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var order in orders)
        {
            worksheet.Cell($"A{row}").Value = order.Id.ToString();
            worksheet.Cell($"B{row}").Value = order.OrderStatus.ToString();
            worksheet.Cell($"C{row}").Value = order.TotalAmount;
            worksheet.Cell($"D{row}").Value = order.CreatedAt.Date;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> ExportRevenueReport(ExportReportCommand command, CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders
            .Where(o => o.OrderStatus == OrderStatus.Delivered)
            .AsQueryable();

        if (command.DateFrom.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= command.DateFrom.Value);
        if (command.DateTo.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= command.DateTo.Value);

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        return command.ExportFormat.ToUpper() switch
        {
            "CSV" => ExportRevenueToCsv(orders),
            "EXCEL" => ExportRevenueToExcel(orders),
            _ => throw new InvalidOperationException("Unknown format")
        };
    }

    private static byte[] ExportRevenueToCsv(List<Order> orders)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Order ID,Amount,Status,Date");

        foreach (var order in orders)
        {
            csv.AppendLine($"\"{order.Id}\",{order.TotalAmount:F2},\"{order.OrderStatus}\",{order.CreatedAt:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static byte[] ExportRevenueToExcel(List<Order> orders)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Revenue");

        worksheet.Cell("A1").Value = "Order ID";
        worksheet.Cell("B1").Value = "Amount";
        worksheet.Cell("C1").Value = "Status";
        worksheet.Cell("D1").Value = "Date";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        decimal totalRevenue = 0;

        foreach (var order in orders)
        {
            worksheet.Cell($"A{row}").Value = order.Id.ToString();
            worksheet.Cell($"B{row}").Value = order.TotalAmount;
            worksheet.Cell($"C{row}").Value = order.OrderStatus.ToString();
            worksheet.Cell($"D{row}").Value = order.CreatedAt.Date;
            totalRevenue += order.TotalAmount;
            row++;
        }

        worksheet.Cell($"A{row}").Value = "TOTAL";
        worksheet.Cell($"B{row}").Value = totalRevenue;
        var summaryRow = worksheet.Row(row);
        summaryRow.Style.Font.Bold = true;
        summaryRow.Style.Fill.BackgroundColor = XLColor.Yellow;

        worksheet.Column("B").Style.NumberFormat.Format = "$#,##0.00";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> ExportSchoolPerformanceReport(ExportReportCommand command, CancellationToken cancellationToken)
    {
        var schools = await _context.Schools
            .Include(s => s.ChildProfiles)
            .ThenInclude(cp => cp.Orders)
            .Include(s => s.SemesterPublications)
            .ToListAsync(cancellationToken);

        var performanceData = schools.Select(s => new
        {
            SchoolName = s.SchoolName,
            TotalOrders = s.ChildProfiles.SelectMany(cp => cp.Orders).Count(),
            CompletedOrders = s.ChildProfiles.SelectMany(cp => cp.Orders).Count(o => o.OrderStatus == OrderStatus.Delivered),
            TotalRevenue = s.ChildProfiles.SelectMany(cp => cp.Orders)
                .Where(o => o.OrderStatus == OrderStatus.Delivered)
                .Sum(o => o.TotalAmount),
            ActiveCampaigns = s.SemesterPublications.Count(sp => sp.Status == SemesterPublicationStatus.Active)
        }).Cast<dynamic>().ToList();

        return command.ExportFormat.ToUpper() switch
        {
            "CSV" => ExportSchoolPerformanceToCsv(performanceData),
            "EXCEL" => ExportSchoolPerformanceToExcel(performanceData),
            _ => throw new InvalidOperationException("Unknown format")
        };
    }

    private static byte[] ExportSchoolPerformanceToCsv(List<dynamic> performanceData)
    {
        var csv = new StringBuilder();
        csv.AppendLine("School Name,Total Orders,Completed Orders,Total Revenue,Active Campaigns");

        foreach (var item in performanceData)
        {
            csv.AppendLine($"\"{item.SchoolName}\",{item.TotalOrders},{item.CompletedOrders},{item.TotalRevenue:F2},{item.ActiveCampaigns}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private static byte[] ExportSchoolPerformanceToExcel(List<dynamic> performanceData)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("School Performance");

        worksheet.Cell("A1").Value = "School Name";
        worksheet.Cell("B1").Value = "Total Orders";
        worksheet.Cell("C1").Value = "Completed Orders";
        worksheet.Cell("D1").Value = "Total Revenue";
        worksheet.Cell("E1").Value = "Active Campaigns";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var item in performanceData)
        {
            worksheet.Cell($"A{row}").Value = item.SchoolName;
            worksheet.Cell($"B{row}").Value = item.TotalOrders;
            worksheet.Cell($"C{row}").Value = item.CompletedOrders;
            worksheet.Cell($"D{row}").Value = item.TotalRevenue;
            worksheet.Cell($"E{row}").Value = item.ActiveCampaigns;
            row++;
        }

        worksheet.Column("D").Style.NumberFormat.Format = "$#,##0.00";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
