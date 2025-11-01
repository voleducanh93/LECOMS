using LECOMS.Data.DTOs.Order;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentService _paymentService;

        public OrderService(IUnitOfWork uow, IPaymentService paymentService)
        {
            _uow = uow;
            _paymentService = paymentService;
        }

        public async Task<(OrderDTO Order, string? PaymentUrl)> CreateOrderFromCartAsync(string userId, CheckoutRequestDTO checkout)
        {
            using var tx = await _uow.BeginTransactionAsync();
            try
            {
                var cart = await _uow.Carts.GetByUserIdAsync(userId, includeProperties: "Items,Items.Product");
                if (cart == null || !cart.Items.Any()) throw new InvalidOperationException("Cart is empty.");

                // Validate stock & compute subtotal
                decimal subtotal = 0m;
                foreach (var it in cart.Items)
                {
                    if (it.Product == null) it.Product = await _uow.Products.GetAsync(p => p.Id == it.ProductId);
                    if (it.Product == null) throw new InvalidOperationException($"Product {it.ProductId} not found.");
                    if (it.Product.Stock < it.Quantity) throw new InvalidOperationException($"Insufficient stock for product {it.Product.Name}.");
                    subtotal += it.Product.Price * it.Quantity;
                }

                // create order
                var order = new Order
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    ShipToName = checkout.ShipToName,
                    ShipToPhone = checkout.ShipToPhone,
                    ShipToAddress = checkout.ShipToAddress,
                    Subtotal = subtotal,
                    ShippingFee = 0m,
                    Discount = 0m,
                    Total = subtotal,
                    Status = OrderStatus.Pending
                };

                await _uow.Orders.AddAsync(order);

                // create details and reduce stock
                foreach (var it in cart.Items)
                {
                    var detail = new OrderDetail
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderId = order.Id,
                        ProductId = it.ProductId,
                        Quantity = it.Quantity,
                        UnitPrice = it.Product.Price
                    };
                    await _uow.OrderDetails.AddAsync(detail); // assume repository exists
                    // decrease stock
                    it.Product.Stock -= it.Quantity;
                    await _uow.Products.UpdateAsync(it.Product);
                }

                // create payment record (VietQr)
                var payment = new Payment
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderId = order.Id,
                    Amount = order.Total,
                    Provider = "VietQr",
                    Status = PaymentStatus.Pending
                };
                await _uow.Payments.AddAsync(payment);

                // clear cart
                foreach (var it in cart.Items.ToList())
                    await _uow.CartItems.DeleteAsync(it);

                await _uow.CompleteAsync();
                await tx.CommitAsync();

                // create payment url via payment service (stub)
                var paymentUrl = await _paymentService.CreateVietQrPaymentAsync(payment.Id, payment.Amount);

                // build DTO
                var dto = await GetByIdAsync(order.Id) ?? throw new Exception("Order created but cannot be loaded.");

                return (dto, paymentUrl);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDTO?> GetByIdAsync(string orderId)
        {
            var order = await _uow.Orders.GetByIdWithDetailsAsync(orderId);
            if (order == null) return null;

            return Map(order);
        }

        public async Task<IEnumerable<OrderDTO>> GetByUserAsync(string userId)
        {
            var orders = await _uow.Orders.GetByUserIdAsync(userId);
            return orders.Select(Map);
        }

        private static OrderDTO Map(Order o)
        {
            return new OrderDTO
            {
                Id = o.Id,
                UserId = o.UserId,
                ShipToName = o.ShipToName,
                ShipToPhone = o.ShipToPhone,
                ShipToAddress = o.ShipToAddress,
                Subtotal = o.Subtotal,
                ShippingFee = o.ShippingFee,
                Discount = o.Discount,
                Total = o.Total,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                Details = o.Details.Select(d => new OrderDetailDTO
                {
                    ProductId = d.ProductId,
                    ProductName = d.Product?.Name ?? string.Empty,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            };
        }
    }
}