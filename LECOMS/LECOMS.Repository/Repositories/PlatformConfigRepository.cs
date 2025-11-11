using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    /// <summary>
    /// Repository implementation cho PlatformConfig
    /// Singleton pattern - chỉ có 1 record duy nhất trong DB
    /// </summary>
    public class PlatformConfigRepository : Repository<PlatformConfig>, IPlatformConfigRepository
    {
        private const string SINGLETON_ID = "PLATFORM_CONFIG_SINGLETON";
        protected readonly LecomDbContext _db;

        public PlatformConfigRepository(LecomDbContext db) : base(db) 
        {
            _db = db;
        }

        /// <summary>
        /// Lấy platform config (singleton)
        /// </summary>
        public async Task<PlatformConfig> GetConfigAsync()
        {
            var config = await dbSet.FirstOrDefaultAsync(c => c.Id == SINGLETON_ID);

            // Nếu chưa có config → tạo mới với giá trị mặc định
            if (config == null)
            {
                config = new PlatformConfig
                {
                    Id = SINGLETON_ID,
                    DefaultCommissionRate = 5.00m,
                    OrderHoldingDays = 7,
                    MinWithdrawalAmount = 100000m,
                    MaxWithdrawalAmount = 50000000m,
                    AutoApproveWithdrawal = false,
                    MaxRefundDays = 30,
                    AutoApproveRefund = false,
                    PayOSEnvironment = "sandbox",
                    EnableEmailNotification = true,
                    LastUpdated = DateTime.UtcNow
                };

                await dbSet.AddAsync(config);
                await _db.SaveChangesAsync();
            }

            return config;
        }

        /// <summary>
        /// Update platform config
        /// </summary>
        public async Task<PlatformConfig> UpdateConfigAsync(PlatformConfig config)
        {
            // Đảm bảo ID luôn là singleton ID
            config.Id = SINGLETON_ID;
            config.LastUpdated = DateTime.UtcNow;

            var existingConfig = await dbSet.FirstOrDefaultAsync(c => c.Id == SINGLETON_ID);

            if (existingConfig == null)
            {
                await dbSet.AddAsync(config);
            }
            else
            {
                // Update tất cả properties
                _db.Entry(existingConfig).CurrentValues.SetValues(config);
            }

            await _db.SaveChangesAsync();
            return config;
        }
    }
}