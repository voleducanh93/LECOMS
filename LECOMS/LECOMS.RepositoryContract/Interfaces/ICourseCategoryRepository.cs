using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ICourseCategoryRepository : IRepository<CourseCategory>
    {
        Task<CourseCategory?> GetByNameAsync(string name);
    }
}
