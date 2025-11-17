using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IPointWalletRepository : IRepository<PointWallet>
    {
        Task<PointWallet?> GetByUserIdAsync(string userId);
    }

    public interface IQuestDefinitionRepository : IRepository<QuestDefinition> { }

    public interface IUserQuestProgressRepository : IRepository<UserQuestProgress> { }

    public interface IPointLedgerRepository : IRepository<PointLedger> { }

    public interface IBoosterRepository : IRepository<Booster> { }

    public interface IUserBoosterRepository : IRepository<UserBooster> { }
}
