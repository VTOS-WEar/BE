namespace VTOS.Application.Features.Schools.Services;

public static class SchoolGradePolicy
{
    public static bool IsClassAllowedForLevel(string? className, string? schoolLevel, out string message)
    {
        message = string.Empty;

        if (!TryGetAllowedRange(schoolLevel, out var minGrade, out var maxGrade))
        {
            message = "Cấp học của trường chưa được cấu hình hoặc không hợp lệ.";
            return false;
        }

        if (!TryExtractGradeNumber(className, out var gradeNumber))
        {
            message = "Tên lớp phải có số khối.";
            return false;
        }

        if (gradeNumber < minGrade || gradeNumber > maxGrade)
        {
            message = $"Lớp {gradeNumber} không hợp lệ với cấp {schoolLevel}. Chỉ được nhập lớp {minGrade}-{maxGrade}.";
            return false;
        }

        return true;
    }

    public static bool TryExtractGradeNumber(string? className, out int gradeNumber)
    {
        gradeNumber = 0;

        if (string.IsNullOrWhiteSpace(className))
            return false;

        var digits = new List<char>();
        foreach (var character in className.Trim())
        {
            if (char.IsDigit(character))
            {
                digits.Add(character);
                continue;
            }

            if (digits.Count > 0)
                break;
        }

        return digits.Count > 0 && int.TryParse(new string(digits.ToArray()), out gradeNumber);
    }

    public static bool TryGetAllowedRange(string? schoolLevel, out int minGrade, out int maxGrade)
    {
        minGrade = 0;
        maxGrade = 0;

        var normalized = NormalizeLevel(schoolLevel);
        switch (normalized)
        {
            case "tieuhoc":
            case "primary":
            case "elementary":
                minGrade = 1;
                maxGrade = 5;
                return true;
            case "thcs":
            case "trunghoccoso":
            case "middle":
            case "middleschool":
                minGrade = 6;
                maxGrade = 9;
                return true;
            case "thpt":
            case "trunghocphothong":
            case "high":
            case "highschool":
                minGrade = 10;
                maxGrade = 12;
                return true;
            default:
                return false;
        }
    }

    private static string NormalizeLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return string.Empty;

        return level.Trim().ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace("tiểu", "tieu")
            .Replace("học", "hoc")
            .Replace("cơ", "co")
            .Replace("sở", "so")
            .Replace("phổ", "pho")
            .Replace("thông", "thong");
    }
}
