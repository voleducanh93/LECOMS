using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class ShopAddressRepository : Repository<ShopAddress>, IShopAddressRepository
    {
        private readonly LecomDbContext _context;
        public ShopAddressRepository(LecomDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ShopAddress?> GetDefaultByShopIdAsync(int shopId)
        {
            return await _context.Set<ShopAddress>()
                .Include(sa => sa.Shop)
                .FirstOrDefaultAsync(sa => sa.ShopId == shopId && sa.IsDefault);
        }

        public async Task<bool> HasAddressAsync(int shopId)
        {
            return await _context.Set<ShopAddress>()
                .AnyAsync(sa => sa.ShopId == shopId);
        }
    }
}
