namespace VTOS.Domain.Enums;

public enum OrderStatus
{
    Pending = 1, //-> Chờ thanh toán
    Paid = 2, //-> Chờ xác nhận
    Confirmed = 3, //-> Chờ xử lý
    Processed = 4, //-> Chờ giao hàng
    Shipped = 5, //-> Chờ phân phối
    Delivered = 6, //-> Đã phân phối
    Cancelled = 7, //-> Đã hủy
    Refunded = 8 //-> Đã hoàn tiền
}

