using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PointLedger
{
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required] public string PointWalletId { get; set; }
    [ForeignKey(nameof(PointWalletId))] public PointWallet Wallet { get; set; } = null!;

    public PointLedgerType Type { get; set; }

    // ⭐ SỐ ĐIỂM THAY ĐỔI (+50, -10,…)
    public int Amount { get; set; }

    // ⭐ LÝ DO (hiển thị FE)
    public int Points { get; set; }

    public string? Description { get; set; }

    public int BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
