using System.Text;
using Microsoft.EntityFrameworkCore;
using VTOS.Application.Abstractions;
using VTOS.Application.Common;

namespace VTOS.Application.Features.Admin.Commands;

public class ExportSchoolActivityLogsCommandHandler : IExportSchoolActivityLogsCommandHandler
{
    private readonly IApplicationDbContext _context;

    public ExportSchoolActivityLogsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<byte[]>> HandleAsync(
        ExportSchoolActivityLogsCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var school = await _context.Schools
                .FirstOrDefaultAsync(s => s.Id == command.SchoolId && !s.IsDeleted, cancellationToken);

            if (school == null)
                return Result<byte[]>.Failure("School not found", "SCHOOL_NOT_FOUND");

            var csv = new StringBuilder();
            csv.AppendLine("Activity Type,Activity Details,Created Date");

            var publications = await _context.SemesterPublications
                .Where(sp => sp.SchoolID == command.SchoolId)
                .ToListAsync(cancellationToken);

            if (command.DateFrom.HasValue)
                publications = publications.Where(sp => sp.CreatedAt >= command.DateFrom.Value).ToList();
            if (command.DateTo.HasValue)
                publications = publications.Where(sp => sp.CreatedAt <= command.DateTo.Value).ToList();

            foreach (var publication in publications)
            {
                var label = publication.Semester + " " + publication.AcademicYear;
                csv.AppendLine($"Semester Publication,Created publication '{label}' (Status: {publication.Status}),{publication.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var outfits = await _context.Outfits
                .Where(o => o.SchoolID == command.SchoolId && !o.IsDeleted)
                .ToListAsync(cancellationToken);

            if (command.DateFrom.HasValue)
                outfits = outfits.Where(o => o.CreatedAt >= command.DateFrom.Value).ToList();
            if (command.DateTo.HasValue)
                outfits = outfits.Where(o => o.CreatedAt <= command.DateTo.Value).ToList();

            foreach (var outfit in outfits)
            {
                if (outfit.UpdatedAt.HasValue)
                    csv.AppendLine($"Uniform Update,Updated uniform '{outfit.OutfitName}' (Price: ${outfit.Price}),{outfit.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                else
                    csv.AppendLine($"Uniform Creation,Created uniform '{outfit.OutfitName}' (Price: ${outfit.Price}),{outfit.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var uploads = await _context.StudentDataImports
                .Where(s => s.SchoolID == command.SchoolId)
                .ToListAsync(cancellationToken);

            if (command.DateFrom.HasValue)
                uploads = uploads.Where(s => s.CreatedAt >= command.DateFrom.Value).ToList();
            if (command.DateTo.HasValue)
                uploads = uploads.Where(s => s.CreatedAt <= command.DateTo.Value).ToList();

            foreach (var upload in uploads)
            {
                csv.AppendLine($"Student List Upload,Uploaded student list (Total: {uploads.Count} records),{upload.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return Result<byte[]>.Success(Encoding.UTF8.GetBytes(csv.ToString()));
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure($"Export failed: {ex.Message}", "EXPORT_ERROR");
        }
    }
}
