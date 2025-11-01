using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Cart;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LECOMS.API.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly UserManager<LECOMS.Data.Entities.User> _userManager;

        public CartController(ICartService cartService, UserManager<LECOMS.Data.Entities.User> userManager)
        {
            _cartService = cartService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);
            var cart = await _cartService.GetCartAsync(userId);
            response.StatusCode = HttpStatusCode.OK;
            response.Result = cart;
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest req)
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);
            try
            {
                var cart = await _cartService.AddItemAsync(userId, req.ProductId, req.Quantity);
                response.StatusCode = HttpStatusCode.OK;
                response.Result = cart;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }
            return StatusCode((int)response.StatusCode, response);
        }

        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> RemoveItem(string productId)
        {
            var response = new APIResponse();
            var userId = _userManager.GetUserId(User);
            var cart = await _cartService.RemoveItemAsync(userId, productId);
            response.StatusCode = HttpStatusCode.OK;
            response.Result = cart;
            return StatusCode((int)response.StatusCode, response);
        }

        // small DTO for request
        public class AddCartItemRequest
        {
            public string ProductId { get; set; } = null!;
            public int Quantity { get; set; }
        }
    }
}