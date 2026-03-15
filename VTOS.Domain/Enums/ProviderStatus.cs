namespace VTOS.Domain.Enums;

public enum ProviderStatus
{
    Pending = 1,    // Chờ xác minh
    Active = 2,     // Đang hoạt động
    Rejected = 3,   // Bị từ chối
    Inactive = 4    // Không hoạt động
}
