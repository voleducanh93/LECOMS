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

        public RecombeeService(RecombeeClient client, IUnitOfWork uow)
        {
            _client = client;
            _uow = uow;
        }

        // ✅ 1. Đồng bộ toàn bộ sản phẩm hiện có trong DB LECOMS sang Recommbee
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

        // ✅ 2. Lấy dữ liệu cho trang Shopping Browse
        public async Task<object> GetBrowseDataAsync(string userId)
        {
            // Nếu user chưa có trong Recommbee, cascadeCreate = true
            var recommendedItemsResponse = await _client.SendAsync(
                new RecommendItemsToUser(userId, count: 10, cascadeCreate: true)
            );

            var recommendedProductIds = recommendedItemsResponse.Recomms.Select(r => r.Id).ToList();

            var recommendedProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Where(p => recommendedProductIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    ThumbnailUrl = p.Images.FirstOrDefault(i => i.IsPrimary).Url,
                    Category = new { p.CategoryId, p.Category.Name }
                })
                .ToListAsync();

            // ✅ Best-seller: lấy các sản phẩm có nhiều order nhất (có thể đổi theo thực tế DB)
            var bestSellers = await _uow.OrderDetails.Query()
                .Include(o => o.Product)
                .ThenInclude(p => p.Images)
                .GroupBy(o => o.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new
                {
                    g.Key,
                    Name = g.First().Product.Name,
                    Price = g.First().Product.Price,
                    ThumbnailUrl = g.First().Product.Images.FirstOrDefault(i => i.IsPrimary).Url
                })
                .ToListAsync();

            // ✅ Gợi ý category: nhóm theo category trong recommendedProducts
            var recommendedCategories = recommendedProducts
                .GroupBy(p => p.Category.CategoryId)
                .Select(g => new
                {
                    Id = g.Key,
                    Name = g.First().Category.Name,
                    Products = g.Take(4).ToList()
                })
                .ToList();

            return new
            {
                recommendedProducts,
                recommendedCategories,
                bestSellerProducts = bestSellers
            };
        }

        // ✅ 3. Gợi ý sản phẩm tương tự
        public async Task<IEnumerable<object>> GetSimilarItemsAsync(string productId, string? userId = null)
        {
            userId ??= "guest";

            var response = await _client.SendAsync(
                new RecommendItemsToItem(
                    productId,
                    userId,             // 👈 SDK v6 bắt buộc targetUserId
                    count: 10,
                    cascadeCreate: true
                )
            );

            var ids = response.Recomms.Select(r => r.Id).ToList();

            var products = await _uow.Products.Query()
                .Include(p => p.Images)
                .Where(p => ids.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    ThumbnailUrl = p.Images.FirstOrDefault(i => i.IsPrimary).Url
                })
                .ToListAsync();

            return products;
        }
    }
}
