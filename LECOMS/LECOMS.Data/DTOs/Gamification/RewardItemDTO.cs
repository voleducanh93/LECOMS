namespace LECOMS.Data.DTOs.Gamification
{
    public class RewardItemDTO
    {
        public string Id { get; set; } = null!;
        public string Type { get; set; } = null!; // Booster / Voucher

        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int CostPoints { get; set; }

        public string ExtraInfo { get; set; } = null!;
    }
}
