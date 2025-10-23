using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IPhotoService
    {
        // Upload hình ảnh (png, jpg,...)
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);

        // Upload video (mp4, mov,...)
        Task<VideoUploadResult> AddVideoAsync(IFormFile file);

        // Upload các loại file khác (pdf, docx,...)
        Task<RawUploadResult> AddFileAsync(IFormFile file);

        // Xóa file trên Cloudinary bằng Public ID
        Task<DeletionResult> DeleteFileAsync(string publicId, ResourceType resourceType = ResourceType.Image); // Thêm tham số loại tài nguyên
    }
}
