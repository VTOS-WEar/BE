namespace VTOS.Domain.Common;

public static class PlatformFeeConstants
{
    public const decimal OrderFeeRate = 0.01m;       // 1% on settled provider orders
    public const decimal WithdrawalFeeRate = 0.02m;  // 2% on approved withdrawal requests
}
