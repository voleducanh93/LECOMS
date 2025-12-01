using LECOMS.Common.Helper;
using LECOMS.Data.DTOs.Withdrawal;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    /// <summary>
    /// Controller xử lý rút tiền (Shop & Customer)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WithdrawalController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;
        private readonly ICustomerWithdrawalService _customerWithdrawalService;
        private readonly IUnitOfWork _unitOfWork;

        public WithdrawalController(
            IWithdrawalService withdrawalService,
            ICustomerWithdrawalService customerWithdrawalService,
            IUnitOfWork unitOfWork)
        {
            _withdrawalService = withdrawalService;
            _customerWithdrawalService = customerWithdrawalService;
            _unitOfWork = unitOfWork;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ========== SHOP ==========

        [HttpPost("shop/create")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateShopWithdrawal([FromBody] CreateShopWithdrawalRequest request)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Không tìm thấy shop");
                    return NotFound(response);
                }

                var dto = new CreateWithdrawalRequestDto
                {
                    ShopId = shop.Id,
                    Amount = request.Amount,
                    BankName = request.BankName,
                    BankAccountNumber = request.BankAccountNumber,
                    BankAccountName = request.BankAccountName,
                    BankBranch = request.BankBranch,
                    Note = request.Note
                };

                var withdrawal = await _withdrawalService.CreateWithdrawalRequestAsync(dto);
                response.Result = new
                {
                    withdrawal.Id,
                    withdrawal.Amount,
                    withdrawal.BankName,
                    BankAccountNumber = MaskBankAccount(withdrawal.BankAccountNumber),
                    withdrawal.BankAccountName,
                    withdrawal.Status,
                    withdrawal.RequestedAt
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [Authorize(Roles = "Seller")]
        [HttpGet("shop/my/{withdrawalId}")]
        public async Task<IActionResult> GetShopWithdrawalById(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _withdrawalService.GetByIdAsync(withdrawalId);

                if (result == null)
                {
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Withdrawal not found");
                    return Ok(response);
                }

                // Check ownership
                var shop = await _unitOfWork.Shops.GetAsync(s => s.Id == result.Shop.ShopId);
                if (shop == null || shop.SellerId != userId)
                {
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Unauthorized");
                    return Ok(response);
                }

                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }


        [HttpGet("shop/my-withdrawals")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopWithdrawals(int page = 1, int pageSize = 20)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Không tìm thấy shop");
                    return NotFound(response);
                }

                var list = await _withdrawalService.GetWithdrawalRequestsByShopAsync(shop.Id, page, pageSize);

                response.Result = list.Select(w => new
                {
                    w.Id,
                    w.Amount,
                    w.BankName,
                    BankAccountNumber = MaskBankAccount(w.BankAccountNumber),
                    w.BankAccountName,
                    w.Status,
                    w.RequestedAt,
                    w.ApprovedAt,
                    w.CompletedAt,
                    w.RejectionReason,
                    w.Note,
                    w.AdminNote
                }).ToList();

                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("shop/cancel/{withdrawalId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CancelShopWithdrawal(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _withdrawalService.CancelWithdrawalRequestAsync(withdrawalId, userId);
                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.RejectionReason
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.Forbidden, response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        // ========== CUSTOMER ==========

        [HttpPost("customer/create")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> CreateCustomerWithdrawal([FromBody] CreateCustomerWithdrawalRequest request)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var dto = new CreateCustomerWithdrawalRequestDto
                {
                    CustomerId = userId,
                    Amount = request.Amount,
                    BankName = request.BankName,
                    BankAccountNumber = request.BankAccountNumber,
                    BankAccountName = request.BankAccountName,
                    BankBranch = request.BankBranch,
                    Note = request.Note
                };

                var withdrawal = await _customerWithdrawalService.CreateCustomerWithdrawalRequestAsync(dto);

                response.Result = new
                {
                    withdrawal.Id,
                    withdrawal.Amount,
                    withdrawal.BankName,
                    BankAccountNumber = MaskBankAccount(withdrawal.BankAccountNumber),
                    withdrawal.BankAccountName,
                    withdrawal.Status,
                    withdrawal.RequestedAt
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [Authorize(Roles = "Customer, Seller")]
        [HttpGet("customer/my/{withdrawalId}")]
        public async Task<IActionResult> GetCustomerWithdrawalById(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _customerWithdrawalService.GetByIdAsync(withdrawalId);

                if (result == null)
                {
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Withdrawal not found");
                    return Ok(response);
                }

                // Check ownership

                if (result.Customer == null || result.Customer.CustomerId != userId)
                {
                    response.IsSuccess = false;
                    response.ErrorMessages.Add("Unauthorized");
                    return Ok(response);
                }

                response.Result = result;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpGet("customer/my-withdrawals")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> GetCustomerWithdrawals(int page = 1, int pageSize = 20)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var list = await _customerWithdrawalService.GetCustomerWithdrawalRequestsByCustomerAsync(userId, page, pageSize);

                response.Result = list.Select(w => new
                {
                    w.Id,
                    w.Amount,
                    w.BankName,
                    BankAccountNumber = MaskBankAccount(w.BankAccountNumber),
                    w.BankAccountName,
                    w.Status,
                    w.RequestedAt,
                    w.ApprovedAt,
                    w.CompletedAt,
                    w.RejectionReason,
                    w.Note,
                    w.AdminNote
                }).ToList();

                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("customer/cancel/{withdrawalId}")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> CancelCustomerWithdrawal(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _customerWithdrawalService.CancelCustomerWithdrawalRequestAsync(withdrawalId, userId);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.RejectionReason
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (UnauthorizedAccessException ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.ErrorMessages.Add(ex.Message);
                return StatusCode((int)HttpStatusCode.Forbidden, response);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        // ========== ADMIN – SHOP ==========

        [HttpGet("admin/shop/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingShopWithdrawals()
        {
            var response = new APIResponse();

            try
            {
                var list = await _withdrawalService.GetPendingWithdrawalRequestsAsync();

                response.Result = list.Select(w => new
                {
                    w.Id,
                    w.ShopId,
                    ShopName = w.Shop.Name,
                    SellerName = w.Shop.Seller.UserName,
                    w.Amount,
                    w.BankName,
                    w.BankAccountNumber,
                    w.BankAccountName,
                    w.BankBranch,
                    w.RequestedAt,
                    w.Note,
                    ShopWallet = new
                    {
                        w.ShopWallet.AvailableBalance,
                        w.ShopWallet.PendingBalance
                    }
                }).ToList();

                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpGet("admin/shop/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetShopWithdrawalById_Admin(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var dto = await _withdrawalService.GetByIdAsync(withdrawalId);

                if (dto == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Withdrawal not found");
                    return NotFound(response);
                }

                response.Result = dto;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }


        [HttpPost("admin/shop/approve/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveShopWithdrawal(string withdrawalId, [FromBody] AdminWithdrawalActionRequest request)
        {
            var response = new APIResponse();

            try
            {
                var adminId = GetUserId();
                if (string.IsNullOrEmpty(adminId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _withdrawalService.ApproveWithdrawalAsync(withdrawalId, adminId, request.Note);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.Amount,
                    result.ApprovedAt
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("admin/shop/complete/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteShopWithdrawal(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var adminId = GetUserId();
                if (string.IsNullOrEmpty(adminId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _withdrawalService.CompleteWithdrawalAsync(withdrawalId, adminId);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.CompletedAt
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("admin/shop/reject/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectShopWithdrawal(string withdrawalId, [FromBody] AdminWithdrawalActionRequest request)
        {
            var response = new APIResponse();

            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                    throw new ArgumentException("Rejection reason is required");

                var adminId = GetUserId();
                if (string.IsNullOrEmpty(adminId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _withdrawalService.RejectWithdrawalAsync(withdrawalId, adminId, request.Reason);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.RejectionReason
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        // ========== ADMIN – CUSTOMER ==========

        [HttpGet("admin/customer/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingCustomerWithdrawals()
        {
            var response = new APIResponse();

            try
            {
                var list = await _customerWithdrawalService.GetPendingCustomerWithdrawalRequestsAsync();

                response.Result = list.Select(w => new
                {
                    w.Id,
                    w.CustomerId,
                    CustomerName = w.Customer.UserName,
                    w.Amount,
                    w.BankName,
                    w.BankAccountNumber,
                    w.BankAccountName,
                    w.BankBranch,
                    w.RequestedAt,
                    w.Note,
                    CustomerWallet = new
                    {
                        w.CustomerWallet.Balance
                    }
                }).ToList();

                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpGet("admin/customer/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCustomerWithdrawalById_Admin(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var dto = await _customerWithdrawalService.GetByIdAsync(withdrawalId);

                if (dto == null)
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.ErrorMessages.Add("Withdrawal not found");
                    return NotFound(response);
                }

                response.Result = dto;
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }


        [HttpPost("admin/customer/approve/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCustomerWithdrawal(string withdrawalId, [FromBody] AdminWithdrawalActionRequest request)
        {
            var response = new APIResponse();

            try
            {
                var adminId = GetUserId();
                if (string.IsNullOrEmpty(adminId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _customerWithdrawalService.ApproveCustomerWithdrawalAsync(withdrawalId, adminId, request.Note);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.Amount,
                    result.ApprovedAt
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("admin/customer/complete/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteCustomerWithdrawal(string withdrawalId)
        {
            var response = new APIResponse();

            try
            {
                var adminId = GetUserId();
                if (string.IsNullOrEmpty(adminId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _customerWithdrawalService.CompleteCustomerWithdrawalAsync(withdrawalId, adminId);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.CompletedAt
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        [HttpPost("admin/customer/reject/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectCustomerWithdrawal(string withdrawalId, [FromBody] AdminWithdrawalActionRequest request)
        {
            var response = new APIResponse();

            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                    throw new ArgumentException("Rejection reason is required");

                var adminId = GetUserId();
                if (string.IsNullOrEmpty(adminId))
                {
                    response.IsSuccess = false;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.ErrorMessages.Add("Unauthorized");
                    return Unauthorized(response);
                }

                var result = await _customerWithdrawalService.RejectCustomerWithdrawalAsync(withdrawalId, adminId, request.Reason);

                response.Result = new
                {
                    result.Id,
                    result.Status,
                    result.RejectionReason
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.StatusCode = HttpStatusCode.BadRequest;
                response.ErrorMessages.Add(ex.Message);
            }

            return Ok(response);
        }

        // ========== HELPERS ==========

        private string MaskBankAccount(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
                return accountNumber;

            return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
        }
    }

    // ====== REQUEST DTOs cho Controller ======

    public class CreateShopWithdrawalRequest
    {
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }

    public class CreateCustomerWithdrawalRequest
    {
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }

    public class AdminWithdrawalActionRequest
    {
        public string? Note { get; set; }
        public string? Reason { get; set; }
    }
}