namespace VTOS.Domain.Enums;

public enum TransactionType
{
    OrderPayment = 1,    // Parent pays for Order → auto-fund School Wallet
    ProviderPayment = 2, // School pays Provider from wallet
    Refund = 3,          // Refund to Parent → wallet balance decreases
    EscrowHold = 4,      // Funds are being held pending settlement
    EscrowRelease = 5,   // Funds are released from school/system ledger
    ProviderPayout = 6   // Funds are credited into provider wallet
}
