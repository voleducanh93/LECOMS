using LECOMS.Data.DTOs.Course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface ICourseCategoryService
    {
        Task<IEnumerable<CourseCategoryDTO>> GetAllAsync();
        Task<CourseCategoryDTO> CreateAsync(CourseCategoryCreateDTO dto);
    }
}
