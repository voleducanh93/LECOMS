using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ICourseProductRepository : IRepository<CourseProduct>
    {
        Task<bool> ExistsAsync(string courseId, string productId);
    }
}
