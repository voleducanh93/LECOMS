using AutoMapper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.DTOs.Recombee;
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

        // ===========================================================================
        // 1️⃣ SYNC PRODUCTS TO RECOMBEE
        // ===========================================================================
        public async Task<int> SyncProductsAsync()
        {
            var products = await _uow.Products.GetAllAsync(includeProperties: "Category,Shop,Images");
            int synced = 0;

            foreach (var p in products)
            {
                var itemValues = new Dictionary<string, object>
                {
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

                await _client.SendAsync(new SetItemValues(p.Id, itemValues, cascadeCreate: true));
                synced++;
            }

            return synced;
        }
        public async Task<int> SyncCoursesAsync()
        {
            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .ToListAsync();

            int synced = 0;

            foreach (var c in courses)
            {
                var itemValues = new Dictionary<string, object>
                {
                    ["type"] = "course",
                    ["title"] = c.Title,
                    ["slug"] = c.Slug,
                    ["categoryId"] = c.CategoryId,
                    ["categoryName"] = c.Category?.Name,
                    ["shopId"] = c.ShopId,
                    ["shopName"] = c.Shop?.Name,
                    ["thumbnailUrl"] = c.CourseThumbnail
                };

                await _client.SendAsync(
                    new SetItemValues(c.Id, itemValues, cascadeCreate: true)
                );

                synced++;
            }

            return synced;
        }

        // ===========================================================================
        // 2️⃣ HOMEPAGE BROWSE → RECOMMENDED + CATEGORY + BEST SELLER
        // ===========================================================================
        public async Task<BrowseResultDTO> GetBrowseDataAsync(string userId)
        {
            // Recommend items from Recombee
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var recIds = rec.Recomms.Select(r => r.Id).ToList();

            var recommendedProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Where(p => recIds.Contains(p.Id))
                .ToListAsync();

            var recommendedCategories = recommendedProducts
                .GroupBy(p => p.CategoryId)
                .Select(g => new
                {
                    id = g.Key,
                    name = g.First().Category.Name,
                    products = _mapper.Map<IEnumerable<ProductDTO>>(g.Take(4))
                })
                .ToList();

            var bestIds = await _uow.OrderDetails.Query()
                .GroupBy(o => o.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToListAsync();

            var bestSellerProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Where(p => bestIds.Contains(p.Id))
                .ToListAsync();

            return new BrowseResultDTO
            {
                RecommendedProducts = _mapper.Map<IEnumerable<ProductDTO>>(recommendedProducts),
                RecommendedCategories = recommendedCategories,
                BestSellerProducts = _mapper.Map<IEnumerable<ProductDTO>>(bestSellerProducts)
            };
        }


        // ===========================================================================
        // 3️⃣ SIMILAR PRODUCTS (ITEM → ITEM)
        // ===========================================================================
        public async Task<IEnumerable<ProductDTO>> GetSimilarProductsFullAsync(string productId, string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToItem(productId, userId, 20, cascadeCreate: true)
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

        // ===========================================================================
        // 4️⃣ SIMILAR COURSES (PRODUCT → COURSE)
        // ===========================================================================
        public async Task<IEnumerable<CourseDTO>> GetSimilarCoursesFullAsync(string itemId, string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToItem(itemId, userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDTO>>(courses);
        }

        // ===========================================================================
        // 5️⃣ RECOMMEND PRODUCTS FOR USER (FULL DTO)
        // ===========================================================================
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

        // ===========================================================================
        // 6️⃣ RECOMMEND COURSES FOR USER (FULL DTO)
        // ===========================================================================
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
    }
}
