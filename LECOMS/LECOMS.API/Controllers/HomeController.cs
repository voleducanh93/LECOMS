using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.Service.Services;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/home")]
    public class HomeController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ISellerCourseService _courseService;
        private readonly IUnitOfWork _uow;
        private readonly IShopService _shopService;
        private readonly UserManager<User> _userManager;
        public HomeController(
            IProductService productService,
            ISellerCourseService courseService,
            IUnitOfWork uow,
            IShopService shopService, UserManager<User> userManager)
        {
            _productService = productService;
            _courseService = courseService;
            _uow = uow;
            _shopService = shopService;
            _userManager = userManager;

        }

        /// <summary>
        /// 🔹 Lấy danh sách sản phẩm public (homepage)
        /// </summary>
        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? sort = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null
        )
        {
            var response = new APIResponse();

            try
            {
                var data = await _productService.GetPublicProductsAsync(
                    search, category, sort, page, pageSize, minPrice, maxPrice
                );
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// 🔹 Lấy danh sách khóa học public (homepage)
        /// </summary>
        [HttpGet("courses")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourses(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? sort = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var response = new APIResponse();

            try
            {
                var data = await _courseService.GetPublicCoursesAsync(
                    search, category, sort, page, pageSize
                );
                response.StatusCode = HttpStatusCode.OK;
                response.Result = data;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// 🔹 Lấy thông tin sản phẩm public theo slug (trang chi tiết sản phẩm)
        /// </summary>
        [HttpGet("products/by-slug/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductBySlug(string slug)
        {
            var response = new APIResponse();

            try
            {
                var product = await _productService.GetBySlugAsync(slug);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = product;
            }
            catch (KeyNotFoundException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Product không tìm thấy.");
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
        /// <summary>
        /// Lấy thông tin shop (public) kèm danh sách sản phẩm và khóa học
        /// </summary>
        [HttpGet("{shopId}")]
        public async Task<IActionResult> GetShopDetail(int shopId)
        {
            var response = new APIResponse();
            try
            {
                var result = await _shopService.GetPublicShopDetailAsync(shopId);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (KeyNotFoundException)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Không tìm thấy cửa hàng.");
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }
        /// <summary>
        /// Lấy thông tin course (public) kèm sessions, lessons và sản phẩm linked theo slug (trang chi tiết khóa học)
        /// </summary>
        [HttpGet("courses/{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCourseDetailBySlug(string slug)
        {
            var response = new APIResponse();
            try
            {
                // 1️⃣ Lấy course theo slug (active) + include đầy đủ navigation
                var course = await _uow.Courses.GetAsync(
                    c => c.Slug == slug && c.Active == 1,
                    includeProperties:
                        "Category,Shop," +
                        "Sections.Lessons.LessonProducts.Product.Images," +
                        "Sections.Lessons.LessonProducts.Product.Shop"
                );

                if (course == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Course không tìm thấy.");
                    return StatusCode((int)response.StatusCode, response);
                }

                // 2️⃣ CHECK ENROLLMENT (để nằm TRƯỚC phần result)
                bool isEnrolled = false;

                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);

                    if (!string.IsNullOrEmpty(userId))
                    {
                        isEnrolled = await _uow.Enrollments.AnyAsync(
                            e => e.UserId == userId && e.CourseId == course.Id
                        );
                    }
                }

                // 3️⃣ Map dữ liệu trả về cho FE
                var result = new
                {
                    id = course.Id,
                    title = course.Title,
                    slug = course.Slug,
                    summary = course.Summary,
                    categoryName = course.Category?.Name,
                    courseThumbnail = course.CourseThumbnail,

                    // 👇👇 Add isEnrolled vào đây (do khai báo phía trên)
                    isEnrolled = isEnrolled,

                    shop = new
                    {
                        id = course.Shop.Id,
                        name = course.Shop.Name,
                        avatar = course.Shop.ShopAvatar,
                        description = course.Shop.Description
                    },

                    sections = course.Sections
                        .OrderBy(s => s.OrderIndex)
                        .Select(s => new
                        {
                            id = s.Id,
                            title = s.Title,
                            orderIndex = s.OrderIndex,
                            lessons = s.Lessons
                                .OrderBy(l => l.OrderIndex)
                                .Select(l =>
                                {
                                    // 🔹 Lấy danh sách sản phẩm liên kết với lesson
                                    var linkedProducts = l.LessonProducts?
                                        .Where(lp => lp.Product != null)
                                        .Select(lp => new LessonLinkedProductDTO
                                        {
                                            Id = lp.Product.Id,
                                            Name = lp.Product.Name,
                                            Price = lp.Product.Price,
                                            ThumbnailUrl = lp.Product.Images
                                                .OrderBy(i => i.OrderIndex)
                                                .FirstOrDefault(i => i.IsPrimary)?.Url,
                                            ShopName = lp.Product.Shop?.Name
                                        })
                                        .ToList();

                                    return new
                                    {
                                        id = l.Id,
                                        title = l.Title,
                                        type = l.Type.ToString(),
                                        durationSeconds = l.DurationSeconds,
                                        contentUrl = l.ContentUrl,
                                        orderIndex = l.OrderIndex,

                                        hasLinkedProducts = linkedProducts != null && linkedProducts.Any(),
                                        linkedProducts = linkedProducts
                                    };
                                })
                        })
                };

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("products/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductDetailById(string id)
        {
            var response = new APIResponse();
            try
            {
                // 1️⃣ Lấy sản phẩm theo id (active & published)
                var product = await _uow.Products.GetAsync(
                    p => p.Id == id && p.Active == 1 && p.Status == Data.Enum.ProductStatus.Published,
                    includeProperties: "Category,Images,Shop"
                );

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Product không tìm thấy.");
                    return StatusCode((int)response.StatusCode, response);
                }

                // 2️⃣ Map dữ liệu trả về cho FE
                var result = new
                {
                    id = product.Id,
                    name = product.Name,
                    slug = product.Slug,
                    description = product.Description,
                    price = product.Price,
                    stock = product.Stock,
                    status = product.Status.ToString(),
                    category = new
                    {
                        id = product.Category.Id,
                        name = product.Category.Name
                    },
                    shop = new
                    {
                        id = product.Shop.Id,
                        name = product.Shop.Name,
                        avatar = product.Shop.ShopAvatar,
                        description = product.Shop.Description
                    },
                    images = product.Images
                        .OrderBy(i => i.OrderIndex)
                        .Select(i => new
                        {
                            url = i.Url,
                            orderIndex = i.OrderIndex,
                            isPrimary = i.IsPrimary
                        }),
                    thumbnailUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                };

                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return StatusCode((int)response.StatusCode, response);
        }

    }
}
