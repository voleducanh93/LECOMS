using LECOMS.Data.DTOs.Cart;
using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _uow;

        public CartService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CartDTO> GetCartAsync(string userId)
        {
            var cart = await _uow.Carts.GetByUserIdAsync(
                userId,
                includeProperties: "Items,Items.Product,Items.Product.Images,Items.Product.Shop");

            if (cart == null || !cart.Items.Any())
            {
                return new CartDTO
                {
                    UserId = userId,
                    Items = new List<ShopGroupedItems>(),
                    Subtotal = 0
                };
            }

            // Group items theo Shop
            var groupedItems = cart.Items
                .Where(i => i.Product != null && i.Product.Shop != null)
                .GroupBy(i => new
                {
                    ShopId = i.Product.ShopId,
                    ShopName = i.Product.Shop.Name,
                    ShopAvatar = i.Product.Shop.ShopAvatar
                })
                .Select(g => new ShopGroupedItems
                {
                    ShopId = g.Key.ShopId,
                    ShopName = g.Key.ShopName,
                    ShopAvatar = g.Key.ShopAvatar,
                    Items = g.Select(i => new CartItemDTO
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        ProductSlug = i.Product.Slug,
                        UnitPrice = i.Product.Price,
                        Quantity = i.Quantity,
                        ProductImage = i.Product.Images
                            .FirstOrDefault(pi => pi.IsPrimary)?.Url
                    }).ToList()
                })
                .OrderBy(g => g.ShopId)
                .ToList();

            return new CartDTO
            {
                UserId = userId,
                Items = groupedItems,
                Subtotal = groupedItems.Sum(g => g.Subtotal)
            };
        }

        public async Task<CartDTO> AddItemAsync(string userId, string productId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Số lượng phải được > 0", nameof(quantity));

            var product = await _uow.Products.GetAsync(p => p.Id == productId);
            if (product == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
            if (product.Stock < quantity) throw new InvalidOperationException("Không đủ hàng.");

            var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items");
            if (cart == null)
            {
                cart = new LECOMS.Data.Entities.Cart
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId
                };
                await _uow.Carts.AddAsync(cart);
            }

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
            {
                item = new CartItem
                {
                    Id = Guid.NewGuid().ToString(),
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                await _uow.CartItems.AddAsync(item);
            }
            else
            {
                item.Quantity += quantity;
                await _uow.CartItems.UpdateAsync(item);
            }

            await _uow.CompleteAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDTO> RemoveItemAsync(string userId, string productId)
        {
            var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items");
            if (cart == null) return new CartDTO { UserId = userId };

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                await _uow.CartItems.DeleteAsync(item);
                await _uow.CompleteAsync();
            }
            return await GetCartAsync(userId);
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items");
            if (cart == null) return true;

            foreach (var item in cart.Items.ToList())
                await _uow.CartItems.DeleteAsync(item);

            await _uow.CompleteAsync();
            return true;
        }

        public async Task<CartDTO> UpdateItemQuantityAsync(string userId, string productId, int? absoluteQuantity, int? quantityChange)
        {
            if (!absoluteQuantity.HasValue && !quantityChange.HasValue)
            {
                throw new ArgumentException("Phải cung cấp Số lượng tuyệt đối hoặc Số lượng thay đổi.");
            }

            if (absoluteQuantity.HasValue && quantityChange.HasValue)
            {
                throw new ArgumentException("Không thể cung cấp cả Số lượng tuyệt đối và Số lượng Thay đổi.");
            }

            var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items");
            if (cart == null)
            {
                throw new InvalidOperationException("Không tìm thấy giỏ hàng");
            }

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null)
            {
                throw new InvalidOperationException("Không tìm thấy sản phẩm trong giỏ hàng.");
            }

            int newQuantity;
            if (absoluteQuantity.HasValue)
            {
                newQuantity = absoluteQuantity.Value;
            }
            else
            {
                newQuantity = item.Quantity + quantityChange.Value;
            }

            if (newQuantity <= 0)
            {
                await _uow.CartItems.DeleteAsync(item);
                await _uow.CompleteAsync();
                return await GetCartAsync(userId);
            }

            var product = await _uow.Products.GetAsync(p => p.Id == productId);
            if (product == null)
            {
                throw new InvalidOperationException("Không tìm thấy sản phẩm.");
            }

            if (product.Stock < newQuantity)
            {
                throw new InvalidOperationException($"Không đủ hàng. Có sẵn: {product.Stock}, được yêu cầu: {newQuantity}");
            }

            item.Quantity = newQuantity;
            await _uow.CartItems.UpdateAsync(item);
            await _uow.CompleteAsync();

            return await GetCartAsync(userId);
        }
    }
}