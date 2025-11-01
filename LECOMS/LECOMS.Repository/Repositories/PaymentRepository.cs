using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        private readonly LecomDbContext _db;

        public PaymentRepository(LecomDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Payment?> GetByIdWithAttemptsAsync(string id)
        {
            return await _db.Set<Payment>()
                .Include(p => p.Attempts)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}