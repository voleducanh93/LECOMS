using LECOMS.Data.DTOs.Gamification.LECOMS.Data.DTOs.Gamification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IGamificationAdminService
    {
        Task<IEnumerable<RedeemRuleDTO>> GetRedeemRulesAsync();
        Task<RedeemRuleDTO> CreateRedeemRuleAsync(RedeemRuleCreateDTO dto);
        Task<RedeemRuleDTO> UpdateRedeemRuleAsync(string id, RedeemRuleUpdateDTO dto);
        Task<bool> DeleteRedeemRuleAsync(string id);
        Task<RedeemRuleDTO?> GetRedeemRuleByIdAsync(string id);
    }
}
