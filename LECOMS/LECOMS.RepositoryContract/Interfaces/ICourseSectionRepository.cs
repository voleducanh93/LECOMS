using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface ICourseSectionRepository : IRepository<CourseSection>
    {
        Task<IEnumerable<CourseSection>> GetSectionsByCourseAsync(string courseId);
    }
}
