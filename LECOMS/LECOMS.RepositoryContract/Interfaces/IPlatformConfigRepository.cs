using LECOMS.Data.Entities;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    /// <summary>
    /// Repository cho PlatformConfig
    /// Singleton pattern - chỉ có 1 record duy nhất
    /// </summary>
    public interface IPlatformConfigRepository : IRepository<PlatformConfig>
    {
        /// <summary>
        /// Lấy platform config (singleton)
        /// </summary>
        Task<PlatformConfig> GetConfigAsync();

        /// <summary>
        /// Update platform config
        /// </summary>
        Task<PlatformConfig> UpdateConfigAsync(PlatformConfig config);
    }
}