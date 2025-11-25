namespace LECOMS.Data.DTOs.Gamification
{
    public class RewardItemDTO
    {
        public string Id { get; set; } = null!;
        public string RewardCode { get; set; } = null!;   // ⭐ ADD
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int CoinCost { get; set; }
        public string Type { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string DurationDescription { get; set; } = null!;
        public bool Redeemable { get; set; }
    }
}
