using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LECOMS.Common.Helper;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly Cloudinary _cloudinary;

        // Inject IOptions<CloudinarySettings> để đọc cấu hình
        public PhotoService(IOptions<CloudinarySettings> config)
        {
            // Kiểm tra null cho config và các giá trị bên trong
            if (config?.Value == null ||
                string.IsNullOrEmpty(config.Value.CloudName) ||
                string.IsNullOrEmpty(config.Value.ApiKey) ||
                string.IsNullOrEmpty(config.Value.ApiSecret))
            {
                throw new ArgumentNullException(nameof(config), "Cloudinary settings are missing or incomplete in configuration.");
            }

            // Tạo tài khoản Cloudinary từ cấu hình
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        // Phương thức upload hình ảnh
        public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // Optional: Thêm transformation nếu muốn thay đổi kích thước, cắt ảnh,...
                    // Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
                    // Optional: Folder = "lecoms/images" // Upload vào thư mục cụ thể trên Cloudinary
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                // Ném lỗi hoặc trả về kết quả lỗi nếu file không hợp lệ
                throw new ArgumentException("Invalid file provided for photo upload.");
            }
            // Kiểm tra lỗi upload từ Cloudinary
            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary photo upload failed: {uploadResult.Error.Message}");
            }
            return uploadResult;
        }

        // Phương thức upload video
        public async Task<VideoUploadResult> AddVideoAsync(IFormFile file)
        {
            var uploadResult = new VideoUploadResult();
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // Optional: Folder = "lecoms/videos"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                throw new ArgumentException("Invalid file provided for video upload.");
            }
            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary video upload failed: {uploadResult.Error.Message}");
            }
            return uploadResult;
        }

        // Phương thức upload file raw (tài liệu, pdf,...)
        public async Task<RawUploadResult> AddFileAsync(IFormFile file)
        {
            var uploadResult = new RawUploadResult();
            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    // Optional: Folder = "lecoms/documents"
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                throw new ArgumentException("Invalid file provided for raw file upload.");
            }
            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary file upload failed: {uploadResult.Error.Message}");
            }
            return uploadResult;
        }

        // Phương thức xóa file
        public async Task<DeletionResult> DeleteFileAsync(string publicId, ResourceType resourceType = ResourceType.Image)
        {
            if (string.IsNullOrEmpty(publicId))
            {
                throw new ArgumentException("Public ID cannot be null or empty for deletion.");
            }
            // Xác định loại tài nguyên cần xóa
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType
            };
            var result = await _cloudinary.DestroyAsync(deleteParams);
            // Bạn có thể kiểm tra result.Result == "ok" hoặc "không tìm thấy"
            return result;
        }
    }
}
