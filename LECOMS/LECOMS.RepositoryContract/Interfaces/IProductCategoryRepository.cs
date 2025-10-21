using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IProductCategoryRepository : IRepository<ProductCategory>
    {
        Task<ProductCategory?> GetByNameAsync(string name);
        Task<bool> ExistsSlugAsync(string slug);
    }
}
