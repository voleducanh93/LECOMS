using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Order;
using LECOMS.Data.Entities;
using LECOMS.Service.Services;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<User> _userManager;
        private readonly IFeedbackService _feedbackService;

        public OrdersController(
            IOrderService orderService,
            IPaymentService paymentService,
            UserManager<User> userManager,
            IFeedbackService feedbackService)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _userManager = userManager;
            _feedbackService = feedbackService;
        }

        // =====================================================================
        // CHECKOUT
        // =====================================================================
        [HttpPost("checkout")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDTO dto)
        {
            var response = new APIResponse();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _orderService.CreateOrderFromCartAsync(userId, dto);

                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccess = true;
                response.Result = result;

                return StatusCode((int)HttpStatusCode.Created, response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Checkout failed.");
                return StatusCode(500, response);
            }
        }

        // =====================================================================
        // GET ORDER BY ID
        // =====================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = new APIResponse();

            try
            {
                var order = await _orderService.GetByIdAsync(id);

                if (order == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Order không tìm thấy.");
                    return NotFound(response);
                }

                // Lấy userId nếu có (controller có [Authorize] nên thường sẽ có)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Bulk: lấy danh sách productId đã feedback trong order (tránh gọi existence từng item)
                var feedbackedProductIds = (userId != null)
                    ? (await _feedbackService.GetFeedbackedProductIdsInOrderAsync(userId, id))
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)
                    : new System.Collections.Generic.HashSet<string>();

                // Map order.Details -> OrderDetailDTO (nếu order trả entity có Details)
                var detailDtos = new System.Collections.Generic.List<OrderDetailDTO>();

                try
                {
                    // Nếu GetByIdAsync trả về Order entity với Details navigation
                    if (order is OrderDTO orderDto && orderDto.Details != null)
                    {
                        detailDtos = orderDto.Details.Select(d => new OrderDetailDTO
                        {
                            Id = d.Id,
                            ProductId = d.ProductId,
                            ProductName = d.ProductName,
                            ProductImage = d.ProductImage,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            ProductCategory = d.ProductCategory,
                            HasFeedback = !string.IsNullOrEmpty(d.ProductId) && feedbackedProductIds.Contains(d.ProductId),
                            FeedbackId = d.FeedbackId
                        }).ToList();
                    }
                    else
                    {
                        // Nếu order là DTO khác, cố gắng reflect Details property
                        var detailsProp = order.GetType().GetProperty("Details");
                        if (detailsProp != null)
                        {
                            var detailsObj = detailsProp.GetValue(order) as System.Collections.IEnumerable;
                            if (detailsObj != null)
                            {
                                foreach (var item in detailsObj)
                                {
                                    var idProp = item.GetType().GetProperty("Id");
                                    var productIdProp = item.GetType().GetProperty("ProductId");
                                    var qtyProp = item.GetType().GetProperty("Quantity");
                                    var priceProp = item.GetType().GetProperty("UnitPrice");
                                    var productProp = item.GetType().GetProperty("Product");

                                    var dto = new OrderDetailDTO();

                                    if (idProp != null) dto.Id = idProp.GetValue(item)?.ToString();
                                    if (productIdProp != null) dto.ProductId = productIdProp.GetValue(item)?.ToString() ?? string.Empty;
                                    if (qtyProp != null && int.TryParse(qtyProp.GetValue(item)?.ToString(), out int q)) dto.Quantity = q;
                                    if (priceProp != null && decimal.TryParse(priceProp.GetValue(item)?.ToString(), out decimal p)) dto.UnitPrice = p;

                                    if (productProp != null)
                                    {
                                        var prod = productProp.GetValue(item);
                                        if (prod != null)
                                        {
                                            var nameProp = prod.GetType().GetProperty("Name");
                                            var imgProp = prod.GetType().GetProperty("ImageUrl");
                                            var catProp = prod.GetType().GetProperty("Category");

                                            if (nameProp != null) dto.ProductName = nameProp.GetValue(prod)?.ToString() ?? string.Empty;
                                            if (imgProp != null) dto.ProductImage = imgProp.GetValue(prod)?.ToString();
                                            if (catProp != null)
                                            {
                                                var cat = catProp.GetValue(prod);
                                                if (cat != null)
                                                {
                                                    var catNameProp = cat.GetType().GetProperty("Name");
                                                    if (catNameProp != null) dto.ProductCategory = catNameProp.GetValue(cat)?.ToString();
                                                }
                                            }
                                        }
                                    }

                                    dto.HasFeedback = !string.IsNullOrEmpty(dto.ProductId) && feedbackedProductIds.Contains(dto.ProductId);
                                    dto.FeedbackId = null;

                                    detailDtos.Add(dto);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // nếu mapping fail, trả details rỗng (FE có thể gọi bulk endpoint thay thế)
                    detailDtos = new System.Collections.Generic.List<OrderDetailDTO>();
                }

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = new
                {
                    order = order,
                    details = detailDtos
                };

                return Ok(response);
            }
            catch (Exception)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Không thể tìm nạp đơn đặt hàng.");
                return StatusCode(500, response);
            }
        }

        // =====================================================================
        // GET ORDERS OF CURRENT USER
        // =====================================================================
        [HttpGet("my")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> MyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var response = new APIResponse();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _orderService.GetByUserAsync(userId, page, pageSize);

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;

                return Ok(response);
            }
            catch (Exception)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Failed to load user orders.");
                return StatusCode(500, response);
            }
        }

        // =====================================================================
        // GET ORDERS OF SHOP
        // =====================================================================
        [HttpGet("shop/my")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> MyShopOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var response = new APIResponse();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var shop = await _userManager.Users
                    .Include(u => u.Shop)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (shop?.Shop == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Shop không tìm thấy for this seller.");
                    return NotFound(response);
                }

                var result = await _orderService.GetByShopAsync(shop.Shop.Id, page, pageSize);

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;

                return Ok(response);
            }
            catch (Exception)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Failed to load shop orders.");
                return StatusCode(500, response);
            }
        }

        // =====================================================================
        // SELLER UPDATE ORDER STATUS
        // =====================================================================
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            var response = new APIResponse();

            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId))
                    return Unauthorized();

                var result = await _orderService.UpdateOrderStatusAsync(id, request.Status, sellerId);

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(403, response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
        }

        // =====================================================================
        // CUSTOMER CONFIRM RECEIVED
        // =====================================================================
        [HttpPost("{orderId}/confirm")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> ConfirmReceived(string orderId)
        {
            var response = new APIResponse();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            try
            {
                var result = await _orderService.ConfirmReceivedAsync(orderId, userId);

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }

        // =====================================================================
        // CANCEL ORDER
        // =====================================================================
        [HttpPost("{orderId}/cancel")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> CancelOrder(string orderId, [FromBody] CancelOrderRequest request)
        {
            var response = new APIResponse();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _orderService.CancelOrderAsync(orderId, userId, request.CancelReason);

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = result;

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(403, response);
            }
            catch (InvalidOperationException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode(500, response);
            }
        }

        // DTO for cancel request
        public class CancelOrderRequest
        {
            public string CancelReason { get; set; } = string.Empty;
        }

        // =====================================================================
        // DTO FOR STATUS UPDATE
        // =====================================================================
        public class UpdateOrderStatusRequest
        {
            public string Status { get; set; } = null!;
        }
    }
}
