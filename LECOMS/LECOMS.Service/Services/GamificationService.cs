using AutoMapper;
using LECOMS.Data.DTOs.Gamification;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.Service.Jobs;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public GamificationService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        #region Profile

        public async Task<GamificationProfileDTO> GetProfileAsync(string userId)
        {
            // Lấy hoặc tạo wallet
            var wallet = await _uow.PointWallets.GetByUserIdAsync(userId);
            if (wallet == null)
            {
                wallet = new PointWallet
                {
                    UserId = userId,
                    Balance = 0,
                    LifetimeEarned = 0,
                    LifetimeSpent = 0,
                    Level = 1,
                    CurrentXP = 0
                };
                await _uow.PointWallets.AddAsync(wallet);
                await _uow.CompleteAsync();
            }

            // Lấy quest hiện tại (đang active trong ngày/tuần/tháng)
            var now = DateTime.UtcNow;

            var progressesQuery = _uow.UserQuestProgresses.Query()
                .Include(q => q.Quest)
                .Where(q => q.UserId == userId && q.PeriodStart <= now && q.PeriodEnd >= now);

            var progresses = await progressesQuery.ToListAsync();
            var questDtos = _mapper.Map<List<QuestDTO>>(progresses);

            var profile = new GamificationProfileDTO
            {
                Level = wallet.Level,
                CurrentXP = wallet.CurrentXP,
                XpToNextLevel = CalcXpToNextLevel(wallet.Level),
                Coins = wallet.Balance,
                DailyStreak = 0,
                DailyQuests = questDtos.Where(q => q.Period == QuestPeriod.Daily.ToString()).ToList(),
                WeeklyQuests = questDtos.Where(q => q.Period == QuestPeriod.Weekly.ToString()).ToList(),
                MonthlyQuests = questDtos.Where(q => q.Period == QuestPeriod.Monthly.ToString()).ToList()
            };

            return profile;
        }

        private int CalcXpToNextLevel(int level)
        {
            // Rule đơn giản: next = 100 * level^2
            return 100 * level * level;
        }

        #endregion

        #region Quest

        public async Task<bool> ClaimQuestAsync(string userId, string userQuestId)
        {
            var progress = await _uow.UserQuestProgresses.GetAsync(
                q => q.Id == userQuestId && q.UserId == userId,
                includeProperties: "Quest");

            if (progress == null) throw new KeyNotFoundException("Quest progress not found.");
            if (!progress.IsCompleted) throw new InvalidOperationException("Quest not completed yet.");
            if (progress.IsClaimed) return false;

            var wallet = await _uow.PointWallets.GetByUserIdAsync(userId)
                         ?? throw new InvalidOperationException("Wallet not found.");

            // Cộng điểm + XP
            wallet.Balance += progress.Quest.RewardPoints;
            wallet.LifetimeEarned += progress.Quest.RewardPoints;
            wallet.CurrentXP += progress.Quest.RewardXP;

            // Cập nhật level nếu đủ XP
            await LevelUpIfNeeded(wallet);

            progress.IsClaimed = true;

            // Ghi ledger
            await _uow.PointLedgers.AddAsync(new PointLedger
            {
                Id = Guid.NewGuid().ToString(),
                PointWalletId = wallet.Id,
                Type = PointLedgerType.Earn,
                Points = progress.Quest.RewardPoints,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            await _uow.PointWallets.UpdateAsync(wallet);
            await _uow.UserQuestProgresses.UpdateAsync(progress);
            await _uow.CompleteAsync();

            return true;
        }

        private async Task LevelUpIfNeeded(PointWallet wallet)
        {
            var required = CalcXpToNextLevel(wallet.Level);
            while (wallet.CurrentXP >= required)
            {
                wallet.CurrentXP -= required;
                wallet.Level++;
                required = CalcXpToNextLevel(wallet.Level);
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Rewards Store

        public async Task<object> GetRewardsStoreAsync(string userId)
        {
            var wallet = await _uow.PointWallets.GetByUserIdAsync(userId);

            var boosters = await _uow.Boosters.GetAllAsync(b => b.Active);
            var now = DateTime.UtcNow;

            var vouchers = await _uow.Vouchers.GetAllAsync(v =>
                v.IsActive &&
                v.StartDate <= now &&
                (!v.EndDate.HasValue || v.EndDate.Value >= now) &&
                v.QuantityAvailable > 0
            );

            // ⭐ lấy danh sách RedeemRule
            var redeemRules = await _uow.RedeemRules.GetAllAsync(r => r.Active);

            var boosterDtos = boosters.Select(b => new RewardItemDTO
            {
                Id = b.Id.ToString(),
                Type = "Booster",
                Name = b.Name,
                Description = b.Description ?? "",
                CostPoints = b.CostPoints,
                ExtraInfo = b.Duration.HasValue
                    ? $"Hoạt động {b.Duration.Value.TotalHours} giờ"
                    : "One-time use"
            }).ToList();

            var voucherDtos = vouchers.Select(v =>
            {
                var rule = redeemRules.FirstOrDefault(r => r.Reward == v.Code);

                return new RewardItemDTO
                {
                    Id = v.Id,
                    Type = "Voucher",
                    Name = v.Code,
                    Description = BuildVoucherDescription(v),
                    CostPoints = rule?.CostPoints ?? 0, // ⭐ FIX 100%
                    ExtraInfo = v.EndDate.HasValue
                        ? $"HSD đến {v.EndDate.Value:dd/MM/yyyy}"
                        : "Không giới hạn"
                };
            }).ToList();

            return new
            {
                balance = wallet?.Balance ?? 0,
                boosters = boosterDtos,
                vouchers = voucherDtos
            };
        }


        private string BuildVoucherDescription(Voucher v)
        {
            var parts = new List<string>();

            if (v.DiscountType == DiscountType.Percentage)
                parts.Add($"{v.DiscountValue}% off");

            if (v.DiscountType == DiscountType.FixedAmount)
                parts.Add($"Giảm ₫{v.DiscountValue:N0}");

            if (v.MinOrderAmount.HasValue)
                parts.Add($"ĐH tối thiểu ₫{v.MinOrderAmount.Value:N0}");

            return string.Join(" • ", parts);
        }

        #endregion

        #region Event + Leaderboard

        public async Task HandleEventAsync(string userId, GamificationEventDTO dto)
        {
            // 1. Tìm EarnRule theo Action
            var rule = await _uow.EarnRules.GetAsync(r => r.Action == dto.Action && r.Active);
            if (rule == null) return; // không có rule => không làm gì

            // 2. Lấy wallet
            var wallet = await _uow.PointWallets.GetByUserIdAsync(userId);
            if (wallet == null)
            {
                wallet = new PointWallet
                {
                    UserId = userId,
                    Level = 1,
                    CurrentXP = 0,
                    Balance = 0
                };
                await _uow.PointWallets.AddAsync(wallet);
                await _uow.CompleteAsync();
            }

            // 3. Cộng điểm & XP
            wallet.Balance += rule.Points;
            wallet.LifetimeEarned += rule.Points;
            wallet.CurrentXP += rule.Points;

            await LevelUpIfNeeded(wallet);

            // 4. Ghi ledger
            await _uow.PointLedgers.AddAsync(new PointLedger
            {
                Id = Guid.NewGuid().ToString(),
                PointWalletId = wallet.Id,
                Type = PointLedgerType.Earn,
                Points = rule.Points,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            // 5. Update quests liên quan (QuestDefinition.Code = Action)
            await UpdateUserQuestsOnEvent(userId, dto.Action);

            // 6. Update leaderboard
            await UpdateLeaderboardsOnEvent(userId, rule.Points);

            await _uow.PointWallets.UpdateAsync(wallet);
            await _uow.CompleteAsync();
        }

        private async Task UpdateUserQuestsOnEvent(string userId, string action)
        {
            var now = DateTime.UtcNow;

            var progresses = await _uow.UserQuestProgresses.GetAllAsync(
                q => q.UserId == userId
                     && q.PeriodStart <= now && q.PeriodEnd >= now
                     && q.Quest.Code == action,
                includeProperties: "Quest");

            foreach (var p in progresses)
            {
                if (p.IsCompleted) continue;

                p.CurrentValue += 1;
                if (p.CurrentValue >= p.Quest.TargetValue)
                {
                    p.CurrentValue = p.Quest.TargetValue;
                    p.IsCompleted = true;
                }

                await _uow.UserQuestProgresses.UpdateAsync(p);
            }
        }

        private async Task UpdateLeaderboardsOnEvent(string userId, int deltaScore)
        {
            var now = DateTime.UtcNow;

            // WEEKLY
            var weeklyLb = await GetOrCreateLeaderboardAsync("GLOBAL_WEEKLY", "Weekly", now.StartOfWeek(), now.EndOfWeek());
            await AddScoreToLeaderboard(weeklyLb, userId, deltaScore);

            // MONTHLY
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);
            var monthlyLb = await GetOrCreateLeaderboardAsync("GLOBAL_MONTHLY", "Monthly", monthStart, monthEnd);
            await AddScoreToLeaderboard(monthlyLb, userId, deltaScore);
        }

        private async Task<Leaderboard> GetOrCreateLeaderboardAsync(string code, string period, DateTime start, DateTime end)
        {
            var lb = await _uow.Leaderboards.GetAsync(
                l => l.Code == code && l.StartAt <= DateTime.UtcNow && l.EndAt >= DateTime.UtcNow);
            if (lb != null) return lb;

            lb = new Leaderboard
            {
                Id = Guid.NewGuid().ToString(),
                Code = code,
                Period = period,
                StartAt = start,
                EndAt = end
            };
            await _uow.Leaderboards.AddAsync(lb);
            await _uow.CompleteAsync();
            return lb;
        }

        private async Task AddScoreToLeaderboard(Leaderboard lb, string userId, int delta)
        {
            var entry = await _uow.LeaderboardEntries.GetByLeaderboardAndUserAsync(lb.Id, userId);
            if (entry == null)
            {
                entry = new LeaderboardEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    LeaderboardId = lb.Id,
                    UserId = userId,
                    Score = delta,
                    Rank = 0
                };
                await _uow.LeaderboardEntries.AddAsync(entry);
            }
            else
            {
                entry.Score += delta;
                await _uow.LeaderboardEntries.UpdateAsync(entry);
            }

            await _uow.CompleteAsync();

            // Recalculate rank
            var entries = _uow.LeaderboardEntries.Query()
                .Where(e => e.LeaderboardId == lb.Id)
                .OrderByDescending(e => e.Score)
                .ToList();

            int rank = 1;
            foreach (var e in entries)
            {
                e.Rank = rank++;
                _uow.LeaderboardEntries.UpdateAsync(e);
            }
            await _uow.CompleteAsync();
        }

        public async Task<LeaderboardDTO> GetLeaderboardAsync(string userId, string period)
        {
            period = period?.ToLower() ?? "weekly";

            Leaderboard? lb = null;
            var now = DateTime.UtcNow;

            if (period == "weekly")
            {
                lb = await _uow.Leaderboards.GetAsync(
                    l => l.Code == "GLOBAL_WEEKLY" && l.StartAt <= now && l.EndAt >= now,
                    includeProperties: "Entries,Entries.User");
            }
            else if (period == "monthly")
            {
                lb = await _uow.Leaderboards.GetAsync(
                    l => l.Code == "GLOBAL_MONTHLY" && l.StartAt <= now && l.EndAt >= now,
                    includeProperties: "Entries,Entries.User");
            }
            else // all time
            {
                lb = await _uow.Leaderboards.GetAsync(
                    l => l.Code == "GLOBAL_ALL",
                    includeProperties: "Entries,Entries.User");
            }

            if (lb == null)
            {
                return new LeaderboardDTO
                {
                    Period = period,
                    Entries = new List<LeaderboardEntryDTO>()
                };
            }

            var ordered = lb.Entries.OrderBy(e => e.Rank).ToList();

            var entriesDto = ordered.Select(e => new LeaderboardEntryDTO
            {
                Rank = e.Rank,
                UserId = e.UserId,
                DisplayName = e.User.UserName,
                AvatarUrl = e.User.ImageUrl ?? "",
                Score = e.Score,
                Level = _uow.PointWallets.Query().FirstOrDefault(w => w.UserId == e.UserId)?.Level ?? 1
            }).ToList();

            var current = entriesDto.FirstOrDefault(e => e.UserId == userId);

            return new LeaderboardDTO
            {
                Period = period,
                Entries = entriesDto,
                CurrentUser = current
            };
        }

        #endregion

        #region Redeem (Booster + Voucher)

        public async Task<RedeemResponseDTO> RedeemAsync(string userId, RedeemRequestDTO dto)
        {
            var wallet = await _uow.PointWallets.GetByUserIdAsync(userId)
                         ?? throw new InvalidOperationException("Wallet not found.");

            // 1. Tìm RedeemRule (Reward = code)
            var rule = await _uow.RedeemRules.GetAsync(r => r.Reward == dto.RewardCode && r.Active);
            if (rule == null)
                throw new InvalidOperationException("Redeem rule not found.");

            if (wallet.Balance < rule.CostPoints)
                throw new InvalidOperationException("Not enough points.");

            // 2. Trừ điểm
            wallet.Balance -= rule.CostPoints;
            wallet.LifetimeSpent += rule.CostPoints;

            await _uow.PointLedgers.AddAsync(new PointLedger
            {
                Id = Guid.NewGuid().ToString(),
                PointWalletId = wallet.Id,
                Type = PointLedgerType.Redeem,
                Points = -rule.CostPoints,
                BalanceAfter = wallet.Balance,
                CreatedAt = DateTime.UtcNow
            });

            // 3. Xem Reward là Booster hay Voucher
            var booster = await _uow.Boosters.GetAsync(b => b.Code == dto.RewardCode);
            if (booster != null)
            {
                await _uow.UserBoosters.AddAsync(new UserBooster
                {
                    UserId = userId,
                    BoosterId = booster.Id,
                    AcquiredAt = DateTime.UtcNow
                });
            }
            else
            {
                var voucher = await _uow.Vouchers.GetByCodeAsync(dto.RewardCode)
                              ?? throw new InvalidOperationException("Reward code not found.");

                await _uow.UserVouchers.AddAsync(new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucher.Id,
                    AssignedAt = DateTime.UtcNow,
                    IsUsed = false
                });
            }

            await _uow.PointWallets.UpdateAsync(wallet);
            await _uow.CompleteAsync();

            return new RedeemResponseDTO
            {
                Success = true,
                Message = "Redeem success.",
                NewBalance = wallet.Balance
            };
        }

        #endregion

    }
}
