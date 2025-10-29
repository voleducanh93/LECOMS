﻿using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IProductImageRepository : IRepository<ProductImage>
    {
        Task<IEnumerable<ProductImage>> GetByProductIdAsync(string productId);
        Task DeleteAllByProductIdAsync(string productId);
    }

}
