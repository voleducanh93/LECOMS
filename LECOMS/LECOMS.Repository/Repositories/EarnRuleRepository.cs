using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class EarnRuleRepository : Repository<EarnRule>, IEarnRuleRepository
    {
        public EarnRuleRepository(LecomDbContext db) : base(db) { }
    }

    public class RedeemRuleRepository : Repository<RedeemRule>, IRedeemRuleRepository
    {
        public RedeemRuleRepository(LecomDbContext db) : base(db) { }
    }

    public class LeaderboardRepository : Repository<Leaderboard>, ILeaderboardRepository
    {
        private readonly LecomDbContext _db;
        public LeaderboardRepository(LecomDbContext db) : base(db) { _db = db; }

        public async Task<Leaderboard?> GetByCodeAsync(string code)
        {
            return await _db.Leaderboards
                .Include(l => l.Entries)
                .FirstOrDefaultAsync(l => l.Code == code);
        }
    }

    public class LeaderboardEntryRepository : Repository<LeaderboardEntry>, ILeaderboardEntryRepository
    {
        private readonly LecomDbContext _db;
        public LeaderboardEntryRepository(LecomDbContext db) : base(db) { _db = db; }

        public async Task<LeaderboardEntry?> GetByLeaderboardAndUserAsync(string leaderboardId, string userId)
        {
            return await _db.LeaderboardEntries
                .FirstOrDefaultAsync(e => e.LeaderboardId == leaderboardId && e.UserId == userId);
        }
    }

    public class VoucherRepository : Repository<Voucher>, IVoucherRepository
    {
        private readonly LecomDbContext _db;
        public VoucherRepository(LecomDbContext db) : base(db) { _db = db; }

        public async Task<Voucher?> GetByCodeAsync(string code)
        {
            return await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        }
    }

    public class UserVoucherRepository : Repository<UserVoucher>, IUserVoucherRepository
    {
        public UserVoucherRepository(LecomDbContext db) : base(db) { }
    }

    public class RankTierRepository : Repository<RankTier>, IRankTierRepository
    {
        public RankTierRepository(LecomDbContext db) : base(db) { }
    }
}
