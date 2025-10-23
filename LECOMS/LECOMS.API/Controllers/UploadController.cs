using LECOMS.Common.Helper; // Using cho APIResponse
using LECOMS.ServiceContract.Interfaces; // Using cho IPhotoService
using Microsoft.AspNetCore.Authorization; // Using cho Authorize
using Microsoft.AspNetCore.Http; // Using cho IFormFile
using Microsoft.AspNetCore.Mvc; // Using cho ControllerBase, IActionResult,...
using System; // Using cho Exception
using System.Net; // Using cho HttpStatusCode
using System.Threading.Tasks; // Using cho Task
using CloudinaryDotNet.Actions; // Using cho ResourceType

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Yêu cầu người dùng phải đăng nhập để upload
    public class UploadController : ControllerBase
    {
        private readonly IPhotoService _photoService;
        private readonly APIResponse _response; // Sử dụng APIResponse đã có

        public UploadController(IPhotoService photoService, APIResponse response) // Inject APIResponse
        {
            _photoService = photoService;
            _response = response; // Gán giá trị
        }

        /// <summary>
        /// Upload một file hình ảnh.
        /// </summary>
        /// <param name="file">File hình ảnh cần upload.</param>
        /// <returns>URL và Public ID của ảnh đã upload.</returns>
        [HttpPost("image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            // Reset response trước mỗi request
            _response.IsSuccess = true;
            _response.ErrorMessages = new List<string>();
            _response.Result = null;
            _response.StatusCode = HttpStatusCode.OK;


            if (file == null || file.Length == 0)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Không có file nào được tải lên.");
                return BadRequest(_response);
            }

            // Optional: Kiểm tra kích thước file
            // if (file.Length > 10 * 1024 * 1024) // Ví dụ: Giới hạn 10MB
            // {
            //      _response.IsSuccess = false;
            //      _response.StatusCode = HttpStatusCode.BadRequest;
            //      _response.ErrorMessages.Add("Kích thước file không được vượt quá 10MB.");
            //      return BadRequest(_response);
            // }

            // Optional: Kiểm tra loại file (chỉ cho phép ảnh)
            // var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            // var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            // if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            // {
            //      _response.IsSuccess = false;
            //      _response.StatusCode = HttpStatusCode.BadRequest;
            //      _response.ErrorMessages.Add("Định dạng file không hợp lệ. Chỉ chấp nhận JPG, JPEG, PNG, GIF.");
            //      return BadRequest(_response);
            // }


            try
            {
                var result = await _photoService.AddPhotoAsync(file);

                _response.StatusCode = HttpStatusCode.OK;
                // Trả về cả URL bảo mật (https) và Public ID
                _response.Result = new { url = result.SecureUrl.AbsoluteUri, publicId = result.PublicId };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Lỗi khi tải ảnh lên: {ex.Message}");
            }
            // Trả về kết quả dựa trên StatusCode đã set
            return StatusCode((int)_response.StatusCode, _response);
        }

        /// <summary>
        /// Upload một file tài liệu (PDF, DOCX,...).
        /// </summary>
        /// <param name="file">File tài liệu cần upload.</param>
        /// <returns>URL và Public ID của tài liệu đã upload.</returns>
        [HttpPost("document")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            _response.IsSuccess = true;
            _response.ErrorMessages = new List<string>();
            _response.Result = null;
            _response.StatusCode = HttpStatusCode.OK;

            if (file == null || file.Length == 0)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Không có file nào được tải lên.");
                return BadRequest(_response);
            }

            // Optional: Thêm kiểm tra loại file cho document nếu cần
            // var allowedDocExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            // ...

            try
            {
                // Sử dụng AddFileAsync cho tài liệu
                var result = await _photoService.AddFileAsync(file);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = new { url = result.SecureUrl.AbsoluteUri, publicId = result.PublicId };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Lỗi khi tải tài liệu lên: {ex.Message}");
            }
            return StatusCode((int)_response.StatusCode, _response);
        }

        /// <summary>
        /// Upload một file video.
        /// </summary>
        /// <param name="file">File video cần upload.</param>
        /// <returns>URL và Public ID của video đã upload.</returns>
        [HttpPost("video")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            _response.IsSuccess = true;
            _response.ErrorMessages = new List<string>();
            _response.Result = null;
            _response.StatusCode = HttpStatusCode.OK;

            if (file == null || file.Length == 0)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Không có file nào được tải lên.");
                return BadRequest(_response);
            }

            // Optional: Thêm kiểm tra loại file video nếu cần
            // var allowedVideoExtensions = new[] { ".mp4", ".mov", ".avi", ".wmv" };
            // ...

            try
            {
                var result = await _photoService.AddVideoAsync(file);

                _response.StatusCode = HttpStatusCode.OK;
                _response.Result = new { url = result.SecureUrl.AbsoluteUri, publicId = result.PublicId };
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Lỗi khi tải video lên: {ex.Message}");
            }
            return StatusCode((int)_response.StatusCode, _response);
        }

        /// <summary>
        /// Xóa file trên Cloudinary bằng Public ID.
        /// </summary>
        /// <param name="publicId">Public ID của file cần xóa.</param>
        /// <param name="resourceType">Loại tài nguyên ('image', 'video', 'raw'). Mặc định là 'image'.</param>
        /// <returns>Kết quả xóa từ Cloudinary.</returns>
        [HttpDelete("{publicId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteFile(string publicId, [FromQuery] string resourceType = "image") // Nhận loại từ query string
        {
            _response.IsSuccess = true;
            _response.ErrorMessages = new List<string>();
            _response.Result = null;
            _response.StatusCode = HttpStatusCode.OK;

            if (string.IsNullOrWhiteSpace(publicId))
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages.Add("Public ID không được để trống.");
                return BadRequest(_response);
            }

            ResourceType type;
            switch (resourceType.ToLowerInvariant())
            {
                case "video":
                    type = ResourceType.Video;
                    break;
                case "raw":
                    type = ResourceType.Raw;
                    break;
                case "image":
                default:
                    type = ResourceType.Image;
                    break;
            }

            try
            {
                var result = await _photoService.DeleteFileAsync(publicId, type);

                if (result.Result.ToLower() == "not found")
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages.Add($"Không tìm thấy file với Public ID: {publicId}");
                }
                else if (result.Result.ToLower() != "ok")
                {
                    // Ném lỗi nếu kết quả không phải 'ok' hoặc 'not found'
                    throw new Exception($"Lỗi xóa file từ Cloudinary: {result.Result}");
                }
                else
                {
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.Result = new { message = "File đã được xóa thành công." };
                }

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages.Add($"Lỗi khi xóa file: {ex.Message}");
            }
            return StatusCode((int)_response.StatusCode, _response);
        }
    }
}