using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IEarnRuleRepository : IRepository<EarnRule> { }

    public interface IRedeemRuleRepository : IRepository<RedeemRule> { }

    public interface ILeaderboardRepository : IRepository<Leaderboard>
    {
        Task<Leaderboard?> GetByCodeAsync(string code);
    }

    public interface ILeaderboardEntryRepository : IRepository<LeaderboardEntry>
    {
        Task<LeaderboardEntry?> GetByLeaderboardAndUserAsync(string leaderboardId, string userId);
    }

    public interface IVoucherRepository : IRepository<Voucher>
    {
        Task<Voucher?> GetByCodeAsync(string code);
    }

    public interface IUserVoucherRepository : IRepository<UserVoucher> { }

    public interface IRankTierRepository : IRepository<RankTier> { }
}
