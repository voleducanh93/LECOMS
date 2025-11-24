using LECOMS.Data.DTOs.Gamification.LECOMS.Data.DTOs.Gamification;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class GamificationAdminService : IGamificationAdminService
    {
        private readonly IUnitOfWork _uow;

        public GamificationAdminService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<RedeemRuleDTO>> GetRedeemRulesAsync()
        {
            var rules = await _uow.RedeemRules.GetAllAsync();
            return rules.Select(r => new RedeemRuleDTO
            {
                Id = r.Id.ToString(),
                Reward = r.Reward,
                CostPoints = r.CostPoints,
                Active = r.Active
            });
        }

        public async Task<RedeemRuleDTO> CreateRedeemRuleAsync(RedeemRuleCreateDTO dto)
        {
            var rule = new RedeemRule
            {
                // Remove: Id = Guid.NewGuid().ToString(),
                Reward = dto.Reward,
                CostPoints = dto.CostPoints,
                Active = dto.Active
            };

            await _uow.RedeemRules.AddAsync(rule);
            await _uow.CompleteAsync();

            return new RedeemRuleDTO
            {
                Id = rule.Id.ToString(),
                Reward = rule.Reward,
                CostPoints = rule.CostPoints,
                Active = rule.Active
            };
        }

        public async Task<RedeemRuleDTO> UpdateRedeemRuleAsync(string id, RedeemRuleUpdateDTO dto)
        {
            var rule = await _uow.RedeemRules.GetAsync(r => r.Id.ToString() == id)
                       ?? throw new InvalidOperationException("Redeem rule not found");

            if (dto.CostPoints.HasValue)
                rule.CostPoints = dto.CostPoints.Value;

            if (dto.Active.HasValue)
                rule.Active = dto.Active.Value;

            await _uow.RedeemRules.UpdateAsync(rule);
            await _uow.CompleteAsync();

            return new RedeemRuleDTO
            {
                Id = rule.Id.ToString(),
                Reward = rule.Reward,
                CostPoints = rule.CostPoints,
                Active = rule.Active
            };
        }

        public async Task<bool> DeleteRedeemRuleAsync(string id)
        {
            var rule = await _uow.RedeemRules.GetAsync(r => r.Id.ToString() == id);
            if (rule == null) return false;

            await _uow.RedeemRules.DeleteAsync(rule);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<RedeemRuleDTO?> GetRedeemRuleByIdAsync(string id)
        {
            var rule = await _uow.RedeemRules.GetAsync(r => r.Id.ToString() == id);
            if (rule == null) return null;

            return new RedeemRuleDTO
            {
                Id = rule.Id.ToString(),
                Reward = rule.Reward,
                CostPoints = rule.CostPoints,
                Active = rule.Active
            };
        }
    }
}
