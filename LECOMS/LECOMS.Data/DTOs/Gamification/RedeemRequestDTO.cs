namespace LECOMS.Data.DTOs.Gamification
{
    public class RedeemRequestDTO
    {
        /// <summary>Reward code = Booster.Code hoặc Voucher.Code</summary>
        public string RewardCode { get; set; } = null!;
    }

    public class RedeemResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public int NewBalance { get; set; }
    }
}
