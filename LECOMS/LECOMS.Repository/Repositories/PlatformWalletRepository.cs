using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class PlatformWalletRepository : Repository<PlatformWallet>, IPlatformWalletRepository
    {
        public PlatformWalletRepository(LecomDbContext db) : base(db)
        {
        }
    }

}
