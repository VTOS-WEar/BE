namespace VTOS.Domain.Enums;

public enum TransactionType
{
    OrderPayment = 1,    // Parent pays for Order → auto-fund SchoolWallet
    ProviderPayment = 2, // School pays Provider from wallet
    Refund = 3           // Refund to Parent → wallet balance decreases
}
