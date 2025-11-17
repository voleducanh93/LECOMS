using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LECOMS.Data.Entities
{
    public class PointWallet
    {
        [Key] public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required] public string UserId { get; set; } = null!;
        [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;

        /// <summary>Current coin/points balance (dùng mua booster, voucher)</summary>
        public int Balance { get; set; }

        /// <summary>Tổng điểm đã kiếm được từ trước tới nay</summary>
        public int LifetimeEarned { get; set; }

        /// <summary>Tổng điểm đã sử dụng</summary>
        public int LifetimeSpent { get; set; }

        /// <summary>Level hiện tại</summary>
        public int Level { get; set; } = 1;

        /// <summary>XP trong level hiện tại</summary>
        public int CurrentXP { get; set; }
    }
}
