using AutoMapper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class RecombeeService
    {
        private readonly RecombeeClient _client;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public RecombeeService(RecombeeClient client, IUnitOfWork uow, IMapper mapper)
        {
            _client = client;
            _uow = uow;
            _mapper = mapper;
        }

        // ===========================================================
        // 1️⃣ Đồng bộ toàn bộ PRODUCT
        // ===========================================================
        public async Task<int> SyncProductsAsync()
        {
            var products = await _uow.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Images)
                .ToListAsync();

            int count = 0;

            foreach (var p in products)
            {
                var values = new Dictionary<string, object>
                {
                    ["type"] = "product",
                    ["name"] = p.Name,
                    ["slug"] = p.Slug,
                    ["categoryId"] = p.CategoryId,
                    ["categoryName"] = p.Category?.Name,
                    ["price"] = Convert.ToDouble(p.Price),
                    ["thumbnailUrl"] = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                    ["shopId"] = p.ShopId,
                    ["shopName"] = p.Shop?.Name,
                    ["status"] = p.Status.ToString()
                };

                await _client.SendAsync(new SetItemValues(p.Id, values, cascadeCreate: true));
                count++;
            }

            return count;
        }

        // ===========================================================
        // 2️⃣ Đồng bộ COURSE
        // ===========================================================
        public async Task<int> SyncCoursesAsync()
        {
            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .ToListAsync();

            int count = 0;

            foreach (var c in courses)
            {
                var values = new Dictionary<string, object>
                {
                    ["type"] = "course",
                    ["name"] = c.Title,
                    ["slug"] = c.Slug,
                    ["categoryId"] = c.CategoryId,
                    ["categoryName"] = c.Category?.Name,
                    ["shopId"] = c.ShopId,
                    ["shopName"] = c.Shop?.Name,
                    ["thumbnailUrl"] = c.CourseThumbnail
                };

                await _client.SendAsync(new SetItemValues(c.Id, values, cascadeCreate: true));
                count++;
            }

            return count;
        }

        // ===========================================================
        // 3️⃣ Recommend sản phẩm FULL DTO
        // ===========================================================
        public async Task<IEnumerable<ProductDTO>> GetSimilarProductsFullAsync(string productId, string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToItem(productId, userId, 10, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var products = await _uow.Products.Query()
                .Include(x => x.Images)
                .Include(x => x.Category)
                .Include(x => x.Shop)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }

        // ===========================================================
        // 4️⃣ Recommend khóa học FULL DTO
        // ===========================================================
        public async Task<IEnumerable<CourseDTO>> GetSimilarCoursesFullAsync(string itemId, string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToItem(itemId, userId, 10, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var courses = await _uow.Courses.Query()
                .Include(x => x.Category)
                .Include(x => x.Shop)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDTO>>(courses);
        }

        // ===========================================================
        // 5️⃣ Recommend PRODUCTS + COURSES cho USER (homepage)
        // ===========================================================
        public async Task<object> RecommendForUserAsync(string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var products = await _uow.Products.Query()
                .Include(x => x.Images)
                .Include(x => x.Category)
                .Include(x => x.Shop)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            var courses = await _uow.Courses.Query()
                .Include(x => x.Category)
                .Include(x => x.Shop)
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            return new
            {
                recommendedProducts = _mapper.Map<IEnumerable<ProductDTO>>(products),
                recommendedCourses = _mapper.Map<IEnumerable<CourseDTO>>(courses)
            };
        }

        // ===========================================================
        // 6️⃣ Browse feed FULL
        // ===========================================================
        public async Task<object> GetBrowseFullDataAsync(string userId)
        {
            var recommended = await RecommendForUserAsync(userId);

            // Best seller (top sold)
            var bestSellerIds = await _uow.OrderDetails.Query()
                .GroupBy(o => o.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToListAsync();

            var bestSellers = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Where(p => bestSellerIds.Contains(p.Id))
                .ToListAsync();

            return new
            {
                recommended,
                bestSellers = _mapper.Map<IEnumerable<ProductDTO>>(bestSellers)
            };
        }
        public async Task<IEnumerable<CourseDTO>> RecommendCoursesForUserAsync(string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDTO>>(courses);
        }
        public async Task<IEnumerable<ProductDTO>> RecommendProductsForUserAsync(string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var products = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDTO>>(products);
        }


    }
}
