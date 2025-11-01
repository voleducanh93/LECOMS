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
            var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items,Items.Product,Items.Product.Images");
            if (cart == null) return new CartDTO { UserId = userId };
            var dto = new CartDTO
            {
                UserId = userId,
                Items = cart.Items.Select(i => new CartItemDTO
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    UnitPrice = i.Product.Price,
                    Quantity = i.Quantity,
                    ProductImage = i.Product.Images.FirstOrDefault(pi => pi.IsPrimary)?.Url
                }).ToList()
            };
            dto.Subtotal = dto.Items.Sum(x => x.LineTotal);
            return dto;
        }

        public async Task<CartDTO> AddItemAsync(string userId, string productId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity must be > 0", nameof(quantity));

            // ensure product exists and has stock
            var product = await _uow.Products.GetAsync(p => p.Id == productId);
            if (product == null) throw new InvalidOperationException("Product not found.");
            if (product.Stock < quantity) throw new InvalidOperationException("Not enough stock.");

            var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items");
            if (cart == null)
            {
                cart = new Cart
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
                await _uow.CartItems.AddAsync(item); // assume repository for CartItem exposed through generic repository
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
    }
}
