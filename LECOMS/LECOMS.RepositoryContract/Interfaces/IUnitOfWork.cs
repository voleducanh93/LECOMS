using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IShopRepository Shops { get; }

        ICourseRepository Courses { get; }          // thêm
        ICourseSectionRepository Sections { get; }  // thêm
        ILessonRepository Lessons { get; }          // thêm
        ICourseProductRepository CourseProducts { get; } // thêm

        ICourseCategoryRepository CourseCategories { get; } // thêm 

        IProductCategoryRepository ProductCategories { get; }
        IProductRepository Products { get; }    
        Task<int> CompleteAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
