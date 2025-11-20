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
                    response.ErrorMessages.Add("Order not found.");
                    return NotFound(response);
                }

                response.IsSuccess = true;
                response.StatusCode = HttpStatusCode.OK;
                response.Result = order;

                return Ok(response);
            }
            catch (Exception)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add("Failed to fetch order.");
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
                    response.ErrorMessages.Add("Shop not found for this seller.");
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
        // DTO FOR STATUS UPDATE
        // =====================================================================
        public class UpdateOrderStatusRequest
        {
            public string Status { get; set; } = null!;
        }
    }
}
