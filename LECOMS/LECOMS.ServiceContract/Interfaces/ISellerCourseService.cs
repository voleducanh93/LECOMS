using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ISellerCourseService
    {
        Task<Course> CreateCourseAsync(CreateCourseDto dto);
        Task<CourseSection> CreateSectionAsync(CreateSectionDto dto);
        Task<Lesson> CreateLessonAsync(CreateLessonDto dto);
        Task<CourseProduct> LinkCourseProductAsync(LinkCourseProductDto dto);
    }
}
