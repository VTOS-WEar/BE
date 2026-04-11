namespace VTOS.Application.Common.Models.BodygramDTOs;

public class BodygramScanHistoryItemResponse
{
    public Guid ScanRecordId { get; set; }
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int HeightCm { get; set; }
    public float WeightKg { get; set; }
    public double? BustCm { get; set; }
    public double? WaistGirthCm { get; set; }
    public double? HipGirthCm { get; set; }
    public double? WaistToHipRatio { get; set; }
    public string? AvatarThumbnailUrl { get; set; }
}

public class BodygramMeasurementDetailItem
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Value { get; set; }
    public double? ValueCm { get; set; }
}

public class BodygramScanDetailResponse
{
    public Guid ScanRecordId { get; set; }
    public Guid ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public string BodygramScanId { get; set; } = string.Empty;
    public string CustomScanId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public int HeightCm { get; set; }
    public float WeightKg { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarFormat { get; set; }
    public string? AvatarType { get; set; }
    public double? WaistToHipRatio { get; set; }
    public string? RiskLevel { get; set; }
    public double? BustCm { get; set; }
    public double? WaistCm { get; set; }
    public double? HipCm { get; set; }
    public double? UpperArmCm { get; set; }
    public double? ThighCm { get; set; }
    public double? CalfCm { get; set; }
    public string? Gender { get; set; }
    public List<BodygramMeasurementDetailItem> Measurements { get; set; } = new();
}
