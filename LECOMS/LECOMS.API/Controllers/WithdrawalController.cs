using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
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
        private readonly ILogger<WithdrawalController> _logger;

        public WithdrawalController(
            IWithdrawalService withdrawalService,
            ICustomerWithdrawalService customerWithdrawalService,
            IUnitOfWork unitOfWork,
            ILogger<WithdrawalController> logger)
        {
            _withdrawalService = withdrawalService;
            _customerWithdrawalService = customerWithdrawalService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ==================== SHOP WITHDRAWAL ====================

        /// <summary>
        /// Shop tạo withdrawal request
        /// POST: api/withdrawal/shop/create
        /// </summary>
        [HttpPost("shop/create")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateShopWithdrawal([FromBody] CreateShopWithdrawalRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Lấy ShopId từ userId
                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                {
                    return NotFound(new { success = false, message = "Shop not found for this user" });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.BankAccountNumber))
                {
                    return BadRequest(new { success = false, message = "Bank account number is required" });
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

                return Ok(new
                {
                    success = true,
                    message = "Withdrawal request created successfully. Waiting for admin approval.",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        shopId = withdrawal.ShopId,
                        amount = withdrawal.Amount,
                        bankName = withdrawal.BankName,
                        bankAccountNumber = MaskBankAccount(withdrawal.BankAccountNumber),
                        status = withdrawal.Status.ToString(),
                        requestedAt = withdrawal.RequestedAt
                    }
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid withdrawal request");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shop withdrawal");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Shop lấy withdrawal history
        /// GET: api/withdrawal/shop/my-withdrawals?page=1&pageSize=20
        /// </summary>
        [HttpGet("shop/my-withdrawals")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopWithdrawals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                {
                    return NotFound(new { success = false, message = "Shop not found" });
                }

                var withdrawals = await _withdrawalService.GetWithdrawalRequestsByShopAsync(shop.Id, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = withdrawals.Select(w => new
                    {
                        id = w.Id,
                        amount = w.Amount,
                        bankName = w.BankName,
                        bankAccountNumber = MaskBankAccount(w.BankAccountNumber),
                        bankAccountName = w.BankAccountName,
                        status = w.Status.ToString(),
                        requestedAt = w.RequestedAt,
                        approvedAt = w.ApprovedAt,
                        processedAt = w.ProcessedAt,
                        completedAt = w.CompletedAt,
                        transactionReference = w.TransactionReference,
                        rejectionReason = w.RejectionReason,
                        failureReason = w.FailureReason,
                        note = w.Note,
                        adminNote = w.AdminNote
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop withdrawals");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Shop cancel withdrawal (chỉ khi Pending)
        /// POST: api/withdrawal/shop/cancel/{withdrawalId}
        /// </summary>
        [HttpPost("shop/cancel/{withdrawalId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CancelShopWithdrawal(string withdrawalId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var withdrawal = await _withdrawalService.CancelWithdrawalRequestAsync(withdrawalId, userId);

                return Ok(new
                {
                    success = true,
                    message = "Withdrawal request cancelled successfully",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        status = withdrawal.Status.ToString()
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation cancelling withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized withdrawal cancellation");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling withdrawal {WithdrawalId}", withdrawalId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== CUSTOMER WITHDRAWAL ====================

        /// <summary>
        /// Customer tạo withdrawal request
        /// POST: api/withdrawal/customer/create
        /// </summary>
        [HttpPost("customer/create")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateCustomerWithdrawal([FromBody] CreateCustomerWithdrawalRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Validate input
                if (string.IsNullOrWhiteSpace(request.BankAccountNumber))
                {
                    return BadRequest(new { success = false, message = "Bank account number is required" });
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

                return Ok(new
                {
                    success = true,
                    message = "Withdrawal request created successfully. Waiting for admin approval.",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        customerId = withdrawal.CustomerId,
                        amount = withdrawal.Amount,
                        bankName = withdrawal.BankName,
                        bankAccountNumber = MaskBankAccount(withdrawal.BankAccountNumber),
                        status = withdrawal.Status.ToString(),
                        requestedAt = withdrawal.RequestedAt
                    }
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid customer withdrawal request");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation creating customer withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer withdrawal");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Customer lấy withdrawal history
        /// GET: api/withdrawal/customer/my-withdrawals?page=1&pageSize=20
        /// </summary>
        [HttpGet("customer/my-withdrawals")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerWithdrawals(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var withdrawals = await _customerWithdrawalService.GetCustomerWithdrawalRequestsByCustomerAsync(userId, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = withdrawals.Select(w => new
                    {
                        id = w.Id,
                        amount = w.Amount,
                        bankName = w.BankName,
                        bankAccountNumber = MaskBankAccount(w.BankAccountNumber),
                        bankAccountName = w.BankAccountName,
                        status = w.Status.ToString(),
                        requestedAt = w.RequestedAt,
                        approvedAt = w.ApprovedAt,
                        processedAt = w.ProcessedAt,
                        completedAt = w.CompletedAt,
                        transactionReference = w.TransactionReference,
                        rejectionReason = w.RejectionReason,
                        failureReason = w.FailureReason,
                        note = w.Note,
                        adminNote = w.AdminNote
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer withdrawals");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Customer cancel withdrawal (chỉ khi Pending)
        /// POST: api/withdrawal/customer/cancel/{withdrawalId}
        /// </summary>
        [HttpPost("customer/cancel/{withdrawalId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelCustomerWithdrawal(string withdrawalId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var withdrawal = await _customerWithdrawalService.CancelCustomerWithdrawalRequestAsync(withdrawalId, userId);

                return Ok(new
                {
                    success = true,
                    message = "Withdrawal request cancelled successfully",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        status = withdrawal.Status.ToString()
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation cancelling customer withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized customer withdrawal cancellation");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling customer withdrawal {WithdrawalId}", withdrawalId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// Admin: Lấy pending shop withdrawals
        /// GET: api/withdrawal/admin/shop/pending
        /// </summary>
        [HttpGet("admin/shop/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingShopWithdrawals()
        {
            try
            {
                var withdrawals = await _withdrawalService.GetPendingWithdrawalRequestsAsync();

                return Ok(new
                {
                    success = true,
                    data = withdrawals.Select(w => new
                    {
                        id = w.Id,
                        shopId = w.ShopId,
                        shopName = w.Shop?.Name,
                        sellerName = w.Shop?.Seller?.UserName,
                        amount = w.Amount,
                        bankName = w.BankName,
                        bankAccountNumber = w.BankAccountNumber, // Admin thấy full
                        bankAccountName = w.BankAccountName,
                        bankBranch = w.BankBranch,
                        requestedAt = w.RequestedAt,
                        note = w.Note,
                        // Wallet info
                        shopWallet = new
                        {
                            availableBalance = w.ShopWallet?.AvailableBalance,
                            pendingBalance = w.ShopWallet?.PendingBalance
                        }
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending shop withdrawals");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Approve shop withdrawal
        /// POST: api/withdrawal/admin/shop/approve/{withdrawalId}
        /// </summary>
        [HttpPost("admin/shop/approve/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveShopWithdrawal(
            string withdrawalId,
            [FromBody] AdminWithdrawalActionRequest request)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var withdrawal = await _withdrawalService.ApproveWithdrawalAsync(withdrawalId, adminId, request.Note);

                return Ok(new
                {
                    success = true,
                    message = "Shop withdrawal approved successfully. Funds deducted from wallet.",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        status = withdrawal.Status.ToString(),
                        approvedAt = withdrawal.ApprovedAt,
                        amount = withdrawal.Amount
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation approving shop withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving shop withdrawal {WithdrawalId}", withdrawalId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Reject shop withdrawal
        /// POST: api/withdrawal/admin/shop/reject/{withdrawalId}
        /// </summary>
        [HttpPost("admin/shop/reject/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectShopWithdrawal(
            string withdrawalId,
            [FromBody] AdminWithdrawalActionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { success = false, message = "Rejection reason is required" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var withdrawal = await _withdrawalService.RejectWithdrawalAsync(withdrawalId, adminId, request.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Shop withdrawal rejected successfully",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        status = withdrawal.Status.ToString(),
                        rejectionReason = withdrawal.RejectionReason
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation rejecting shop withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting shop withdrawal {WithdrawalId}", withdrawalId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Lấy pending customer withdrawals
        /// GET: api/withdrawal/admin/customer/pending
        /// </summary>
        [HttpGet("admin/customer/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingCustomerWithdrawals()
        {
            try
            {
                var withdrawals = await _customerWithdrawalService.GetPendingCustomerWithdrawalRequestsAsync();

                return Ok(new
                {
                    success = true,
                    data = withdrawals.Select(w => new
                    {
                        id = w.Id,
                        customerId = w.CustomerId,
                        customerName = w.Customer?.UserName,
                        customerEmail = w.Customer?.Email,
                        amount = w.Amount,
                        bankName = w.BankName,
                        bankAccountNumber = w.BankAccountNumber, // Admin thấy full
                        bankAccountName = w.BankAccountName,
                        bankBranch = w.BankBranch,
                        requestedAt = w.RequestedAt,
                        note = w.Note,
                        customerWallet = new
                        {
                            balance = w.CustomerWallet?.Balance
                        }
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending customer withdrawals");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Approve customer withdrawal
        /// POST: api/withdrawal/admin/customer/approve/{withdrawalId}
        /// </summary>
        [HttpPost("admin/customer/approve/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCustomerWithdrawal(
            string withdrawalId,
            [FromBody] AdminWithdrawalActionRequest request)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var withdrawal = await _customerWithdrawalService.ApproveCustomerWithdrawalAsync(withdrawalId, adminId, request.Note);

                return Ok(new
                {
                    success = true,
                    message = "Customer withdrawal approved successfully. Funds deducted from wallet.",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        status = withdrawal.Status.ToString(),
                        approvedAt = withdrawal.ApprovedAt,
                        amount = withdrawal.Amount
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation approving customer withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving customer withdrawal {WithdrawalId}", withdrawalId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Reject customer withdrawal
        /// POST: api/withdrawal/admin/customer/reject/{withdrawalId}
        /// </summary>
        [HttpPost("admin/customer/reject/{withdrawalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectCustomerWithdrawal(
            string withdrawalId,
            [FromBody] AdminWithdrawalActionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { success = false, message = "Rejection reason is required" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var withdrawal = await _customerWithdrawalService.RejectCustomerWithdrawalAsync(withdrawalId, adminId, request.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Customer withdrawal rejected successfully",
                    data = new
                    {
                        withdrawalId = withdrawal.Id,
                        status = withdrawal.Status.ToString(),
                        rejectionReason = withdrawal.RejectionReason
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation rejecting customer withdrawal");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting customer withdrawal {WithdrawalId}", withdrawalId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Mask bank account number (hiển thị 4 số cuối)
        /// </summary>
        private string MaskBankAccount(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
                return accountNumber;

            return new string('*', accountNumber.Length - 4) + accountNumber.Substring(accountNumber.Length - 4);
        }
    }

    // ==================== REQUEST DTOs ====================

    /// <summary>
    /// Request DTO cho shop withdrawal
    /// </summary>
    public class CreateShopWithdrawalRequest
    {
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request DTO cho customer withdrawal
    /// </summary>
    public class CreateCustomerWithdrawalRequest
    {
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankAccountName { get; set; } = null!;
        public string? BankBranch { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>
    /// Request DTO cho admin approve/reject
    /// </summary>
    public class AdminWithdrawalActionRequest
    {
        public string? Note { get; set; }
        public string? Reason { get; set; }
    }
}