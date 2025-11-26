using LECOMS.Data.DTOs.Gamification;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IGamificationService
    {
        Task<GamificationProfileDTO> GetProfileAsync(string userId);
        Task<bool> ClaimQuestAsync(string userId, string userQuestId);
        Task<object> GetRewardsStoreAsync(string userId);
        // ⭐ mới thêm
        Task HandleEventAsync(string userId, GamificationEventDTO dto);
        Task<LeaderboardDTO> GetLeaderboardAsync(string userId, string period); // "weekly" | "monthly" | "all"
        Task<RedeemResponseDTO> RedeemAsync(string userId, RedeemRequestDTO dto);
        Task InitializeUserGamificationAsync(string userId);

    }
}
