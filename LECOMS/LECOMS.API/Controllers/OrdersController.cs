using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Order;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly UserManager<LECOMS.Data.Entities.User> _userManager;

        public OrdersController(IOrderService orderService, IPaymentService paymentService, UserManager<LECOMS.Data.Entities.User> userManager)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _userManager = userManager;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDTO dto)
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);
            try
            {
                var (order, paymentUrl) = await _orderService.CreateOrderFromCartAsync(userId, dto);
                response.StatusCode = HttpStatusCode.Created;
                response.Result = new { order, paymentUrl };
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = new APIResponse();
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.NotFound;
                response.ErrorMessages.Add("Order not found.");
            }
            else
            {
                response.StatusCode = HttpStatusCode.OK;
                response.Result = order;
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpGet("my")]
        public async Task<IActionResult> MyOrders()
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);
            var list = await _orderService.GetByUserAsync(userId);
            response.StatusCode = HttpStatusCode.OK;
            response.Result = list;
            return StatusCode((int)response.StatusCode, response);
        }

        // Webhook endpoint for VietQr (public)
        [AllowAnonymous]
        [HttpPost("payments/webhook")]
        public async Task<IActionResult> PaymentWebhook()
        {
            using var sr = new StreamReader(Request.Body);
            var payload = await sr.ReadToEndAsync();
            var ok = await _paymentService.HandleVietQrWebhookAsync(payload);
            return Ok(new { handled = ok });
        }
    }
}