using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LecomDbContext _context;
        public IUserRepository Users { get; }

        public UnitOfWork(LecomDbContext context, IUserRepository userRepository)
        {
            _context = context;
            Users = userRepository;


        }
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
