using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class PointWalletRepository : Repository<PointWallet>, IPointWalletRepository
    {
        private readonly LecomDbContext _db;

        public PointWalletRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<PointWallet?> GetByUserIdAsync(string userId)
        {
            return await _db.PointWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        }
    }

    public class QuestDefinitionRepository : Repository<QuestDefinition>, IQuestDefinitionRepository
    {
        public QuestDefinitionRepository(LecomDbContext db) : base(db) { }
    }

    public class UserQuestProgressRepository : Repository<UserQuestProgress>, IUserQuestProgressRepository
    {
        public UserQuestProgressRepository(LecomDbContext db) : base(db) { }
    }

    public class PointLedgerRepository : Repository<PointLedger>, IPointLedgerRepository
    {
        public PointLedgerRepository(LecomDbContext db) : base(db) { }
    }

    public class BoosterRepository : Repository<Booster>, IBoosterRepository
    {
        public BoosterRepository(LecomDbContext db) : base(db) { }
    }

    public class UserBoosterRepository : Repository<UserBooster>, IUserBoosterRepository
    {
        public UserBoosterRepository(LecomDbContext db) : base(db) { }
    }
}
