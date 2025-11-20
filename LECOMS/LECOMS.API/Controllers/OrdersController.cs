using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Order;
using LECOMS.Data.Entities;
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
    /// <summary>
    /// Controller xử lý Orders
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<User> _userManager;

        public OrdersController(
            IOrderService orderService,
            IPaymentService paymentService,
            UserManager<User> userManager)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _userManager = userManager;
        }

        /// <summary>
        /// Checkout: Tạo order từ cart và payment link
        /// POST: api/orders/checkout
        /// </summary>
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
                response.Result = result;
                response.IsSuccess = true;

                return StatusCode((int)response.StatusCode, response);
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
                response.ErrorMessages.Add("An error occurred while processing your checkout.");
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Lấy order by ID
        /// GET: api/orders/{id}
        /// </summary>
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
                    response.ErrorMessages.Add("Order not found.");
                    return NotFound(response);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.Result = order;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Error retrieving order.");
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Lấy orders của current user
        /// GET: api/orders/my?page=1&pageSize=20
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> MyOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var response = new APIResponse();
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var orders = await _orderService.GetByUserAsync(userId, page, pageSize);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = orders;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Error retrieving orders.");
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Shop lấy orders của mình
        /// GET: api/orders/shop/my?page=1&pageSize=20
        /// </summary>
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

                // Lấy ShopId từ userId
                var shop = await _userManager.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Shop)
                    .FirstOrDefaultAsync();

                if (shop == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Shop not found for this user.");
                    return NotFound(response);
                }

                var orders = await _orderService.GetByShopAsync(shop.Id, page, pageSize);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = orders;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Error retrieving shop orders.");
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Shop update order status
        /// PUT: api/orders/{id}/status
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateStatus(
            string id,
            [FromBody] UpdateOrderStatusRequest request)
        {
            var response = new APIResponse();
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var order = await _orderService.UpdateOrderStatusAsync(id, request.Status, userId);

                response.StatusCode = HttpStatusCode.OK;
                response.Result = order;
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

        /// <summary>
        /// Customer xác nhận đã nhận hàng
        /// POST: api/orders/{id}/confirm-received
        /// </summary>
        [HttpPost("{orderId}/confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmReceived(string orderId)
        {
            var response = new APIResponse();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.ErrorMessages.Add("Unauthorized.");
                return Unauthorized(response);
            }

            try
            {
                var result = await _orderService.ConfirmReceivedAsync(orderId, userId);
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
                return StatusCode((int)response.StatusCode, response);
            }
        }


        /// <summary>
        /// Request DTO cho update status
        /// </summary>
        public class UpdateOrderStatusRequest
        {
            public string Status { get; set; } = null!;
        } 
    }
}