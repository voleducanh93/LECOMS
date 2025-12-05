using LECOMS.Data.Entities;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LECOMS.API.Controllers
{
    /// <summary>
    /// Controller quản lý cấu hình platform
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PlatformConfigController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlatformConfigController> _logger;

        public PlatformConfigController(
            IUnitOfWork unitOfWork,
            ILogger<PlatformConfigController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Lấy platform config (public - không cần auth)
        /// GET: api/platformconfig
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlatformConfig()
        {
            try
            {
                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        // Public info (visible to all users)
                        defaultCommissionRate = config.DefaultCommissionRate,
                        orderHoldingDays = config.OrderHoldingDays,
                        minWithdrawalAmount = config.MinWithdrawalAmount,
                        maxWithdrawalAmount = config.MaxWithdrawalAmount,
                        maxRefundDays = config.MaxRefundDays,
                        sellerRefundResponseHours = config.SellerRefundResponseHours,

                        // Hide sensitive info
                        // payOSEnvironment, clientId, apiKey, etc.

                        lastUpdated = config.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting platform config");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Lấy full platform config
        /// GET: api/platformconfig/admin/full
        /// </summary>
        [HttpGet("admin/full")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFullPlatformConfig()
        {
            try
            {
                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = config.Id,

                        // Commission & Fees
                        defaultCommissionRate = config.DefaultCommissionRate,

                        // Order Settings
                        orderHoldingDays = config.OrderHoldingDays,

                        // Withdrawal Settings
                        minWithdrawalAmount = config.MinWithdrawalAmount,
                        maxWithdrawalAmount = config.MaxWithdrawalAmount,
                        autoApproveWithdrawal = config.AutoApproveWithdrawal,

                        // Refund Settings
                        maxRefundDays = config.MaxRefundDays,
                        autoApproveRefund = config.AutoApproveRefund,

                        // PayOS Settings (sensitive)
                        payOSEnvironment = config.PayOSEnvironment,
                        payOSClientId = MaskSensitiveData(config.PayOSClientId),
                        payOSApiKey = MaskSensitiveData(config.PayOSApiKey),
                        payOSChecksumKey = MaskSensitiveData(config.PayOSChecksumKey),

                        // Notification Settings
                        enableEmailNotification = config.EnableEmailNotification,
                        enableSMSNotification = config.EnableSMSNotification,

                        // Metadata
                        lastUpdated = config.LastUpdated,
                        lastUpdatedBy = config.LastUpdatedBy
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting full platform config");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Update platform config
        /// PUT: api/platformconfig/admin/update
        /// </summary>
        [HttpPut("admin/update")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePlatformConfig([FromBody] UpdatePlatformConfigRequest request)
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                // Validate
                if (request.DefaultCommissionRate < 0 || request.DefaultCommissionRate > 100)
                {
                    return BadRequest(new { success = false, message = "Commission rate must be between 0 and 100" });
                }

                if (request.OrderHoldingDays < 0 || request.OrderHoldingDays > 90)
                {
                    return BadRequest(new { success = false, message = "Order holding days must be between 0 and 90" });
                }

                if (request.MinWithdrawalAmount < 0)
                {
                    return BadRequest(new { success = false, message = "Min withdrawal Số tiền phải dương" });
                }

                if (request.MaxWithdrawalAmount < request.MinWithdrawalAmount)
                {
                    return BadRequest(new { success = false, message = "Max Số tiền rút must be greater than min" });
                }

                if (request.MaxRefundDays < 0 || request.MaxRefundDays > 365)
                {
                    return BadRequest(new { success = false, message = "Max refund days must be between 0 and 365" });
                }

                if (request.SellerRefundResponseHours < 1 || request.SellerRefundResponseHours > 168)
                {
                    return BadRequest(new { success = false, message = "SellerRefundResponseHours phải từ 1 đến 168 (tối đa 7 ngày)" });
                }


                // Lấy config hiện tại
                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                // Update values
                config.DefaultCommissionRate = request.DefaultCommissionRate;
                config.OrderHoldingDays = request.OrderHoldingDays;
                config.MinWithdrawalAmount = request.MinWithdrawalAmount;
                config.MaxWithdrawalAmount = request.MaxWithdrawalAmount;
                config.AutoApproveWithdrawal = request.AutoApproveWithdrawal;
                config.MaxRefundDays = request.MaxRefundDays;
                config.SellerRefundResponseHours = request.SellerRefundResponseHours;
                config.AutoApproveRefund = request.AutoApproveRefund;
                config.EnableEmailNotification = request.EnableEmailNotification;
                config.EnableSMSNotification = request.EnableSMSNotification;
                config.LastUpdatedBy = adminId;
                config.LastUpdated = DateTime.UtcNow;

                // Update PayOS settings (nếu có)
                if (!string.IsNullOrWhiteSpace(request.PayOSEnvironment))
                {
                    config.PayOSEnvironment = request.PayOSEnvironment;
                }

                if (!string.IsNullOrWhiteSpace(request.PayOSClientId))
                {
                    config.PayOSClientId = request.PayOSClientId;
                }

                if (!string.IsNullOrWhiteSpace(request.PayOSApiKey))
                {
                    config.PayOSApiKey = request.PayOSApiKey;
                }

                if (!string.IsNullOrWhiteSpace(request.PayOSChecksumKey))
                {
                    config.PayOSChecksumKey = request.PayOSChecksumKey;
                }

                // Save
                var updatedConfig = await _unitOfWork.PlatformConfigs.UpdateConfigAsync(config);

                _logger.LogInformation("Platform config updated by admin {AdminId}", adminId);

                return Ok(new
                {
                    success = true,
                    message = "Platform config updated successfully",
                    data = new
                    {
                        defaultCommissionRate = updatedConfig.DefaultCommissionRate,
                        orderHoldingDays = updatedConfig.OrderHoldingDays,
                        minWithdrawalAmount = updatedConfig.MinWithdrawalAmount,
                        maxWithdrawalAmount = updatedConfig.MaxWithdrawalAmount,
                        autoApproveWithdrawal = updatedConfig.AutoApproveWithdrawal,
                        maxRefundDays = updatedConfig.MaxRefundDays,
                        autoApproveRefund = updatedConfig.AutoApproveRefund,
                        enableEmailNotification = updatedConfig.EnableEmailNotification,
                        enableSMSNotification = updatedConfig.EnableSMSNotification,
                        lastUpdated = updatedConfig.LastUpdated,
                        lastUpdatedBy = updatedConfig.LastUpdatedBy
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating platform config");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Update commission rate only (quick action)
        /// PATCH: api/platformconfig/admin/commission-rate
        /// </summary>
        [HttpPatch("admin/commission-rate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCommissionRate([FromBody] UpdateCommissionRateRequest request)
        {
            try
            {
                if (request.CommissionRate < 0 || request.CommissionRate > 100)
                {
                    return BadRequest(new { success = false, message = "Commission rate must be between 0 and 100" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                var oldRate = config.DefaultCommissionRate;
                config.DefaultCommissionRate = request.CommissionRate;
                config.LastUpdatedBy = adminId;
                config.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.PlatformConfigs.UpdateConfigAsync(config);

                _logger.LogInformation(
                    "Commission rate updated from {OldRate}% to {NewRate}% by admin {AdminId}",
                    oldRate, request.CommissionRate, adminId);

                return Ok(new
                {
                    success = true,
                    message = $"Commission rate updated from {oldRate}% to {request.CommissionRate}%",
                    data = new
                    {
                        oldRate = oldRate,
                        newRate = request.CommissionRate,
                        updatedAt = config.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commission rate");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Update holding period only (quick action)
        /// PATCH: api/platformconfig/admin/holding-period
        /// </summary>
        [HttpPatch("admin/holding-period")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateHoldingPeriod([FromBody] UpdateHoldingPeriodRequest request)
        {
            try
            {
                if (request.Days < 0 || request.Days > 90)
                {
                    return BadRequest(new { success = false, message = "Holding period must be between 0 and 90 days" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                var oldDays = config.OrderHoldingDays;
                config.OrderHoldingDays = request.Days;
                config.LastUpdatedBy = adminId;
                config.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.PlatformConfigs.UpdateConfigAsync(config);

                _logger.LogInformation(
                    "Holding period updated from {OldDays} to {NewDays} days by admin {AdminId}",
                    oldDays, request.Days, adminId);

                return Ok(new
                {
                    success = true,
                    message = $"Holding period updated from {oldDays} to {request.Days} days",
                    data = new
                    {
                        oldDays = oldDays,
                        newDays = request.Days,
                        updatedAt = config.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating holding period");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Reset config to default
        /// POST: api/platformconfig/admin/reset-to-default
        /// </summary>
        [HttpPost("admin/reset-to-default")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetToDefault()
        {
            try
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                // Reset to default values
                config.DefaultCommissionRate = 5.00m;
                config.OrderHoldingDays = 7;
                config.MinWithdrawalAmount = 100000m;
                config.MaxWithdrawalAmount = 50000000m;
                config.AutoApproveWithdrawal = false;
                config.MaxRefundDays = 30;
                config.AutoApproveRefund = false;
                config.EnableEmailNotification = true;
                config.EnableSMSNotification = false;
                config.LastUpdatedBy = adminId;
                config.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.PlatformConfigs.UpdateConfigAsync(config);

                _logger.LogWarning("Platform config reset to default by admin {AdminId}", adminId);

                return Ok(new
                {
                    success = true,
                    message = "Platform config reset to default values",
                    data = new
                    {
                        defaultCommissionRate = config.DefaultCommissionRate,
                        orderHoldingDays = config.OrderHoldingDays,
                        minWithdrawalAmount = config.MinWithdrawalAmount,
                        maxWithdrawalAmount = config.MaxWithdrawalAmount,
                        lastUpdated = config.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting platform config");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }


        [HttpPatch("admin/refund-response-hours")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRefundResponseHours([FromBody] UpdateRefundResponseHoursRequest request)
        {
            try
            {
                if (request.Hours < 1 || request.Hours > 168)
                {
                    return BadRequest(new { success = false, message = "Giờ phản hồi phải từ 1 đến 168 (tối đa 7 ngày)" });
                }

                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(adminId))
                    return Unauthorized();

                var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();

                var oldHours = config.SellerRefundResponseHours;
                config.SellerRefundResponseHours = request.Hours;
                config.LastUpdatedBy = adminId;
                config.LastUpdated = DateTime.UtcNow;

                await _unitOfWork.PlatformConfigs.UpdateConfigAsync(config);

                _logger.LogInformation(
                    "SellerRefundResponseHours updated from {OldHours} to {NewHours} by admin {AdminId}",
                    oldHours, request.Hours, adminId);

                return Ok(new
                {
                    success = true,
                    message = $"Thời gian phản hồi refund của seller đã đổi từ {oldHours}h → {request.Hours}h",
                    data = new
                    {
                        oldHours,
                        newHours = request.Hours,
                        updatedAt = config.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating SellerRefundResponseHours");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Mask sensitive data (API keys, passwords)
        /// </summary>
        private string? MaskSensitiveData(string? data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            if (data.Length <= 8)
                return new string('*', data.Length);

            // Show first 4 and last 4 characters
            return $"{data.Substring(0, 4)}{new string('*', data.Length - 8)}{data.Substring(data.Length - 4)}";
        }
    }

    // ==================== REQUEST DTOs ====================

    /// <summary>
    /// Request DTO cho update platform config
    /// </summary>
    public class UpdatePlatformConfigRequest
    {
        // Commission & Fees
        public decimal DefaultCommissionRate { get; set; }

        // Order Settings
        public int OrderHoldingDays { get; set; }

        public int SellerRefundResponseHours { get; set; }

        // Withdrawal Settings
        public decimal MinWithdrawalAmount { get; set; }
        public decimal MaxWithdrawalAmount { get; set; }
        public bool AutoApproveWithdrawal { get; set; }

        // Refund Settings
        public int MaxRefundDays { get; set; }
        public bool AutoApproveRefund { get; set; }

        // PayOS Settings (optional - chỉ update khi có giá trị)
        public string? PayOSEnvironment { get; set; }
        public string? PayOSClientId { get; set; }
        public string? PayOSApiKey { get; set; }
        public string? PayOSChecksumKey { get; set; }

        // Notification Settings
        public bool EnableEmailNotification { get; set; }
        public bool EnableSMSNotification { get; set; }
    }

    /// <summary>
    /// Request DTO cho update commission rate
    /// </summary>
    public class UpdateCommissionRateRequest
    {
        public decimal CommissionRate { get; set; }
    }

    /// <summary>
    /// Request DTO cho update holding period
    /// </summary>
    public class UpdateHoldingPeriodRequest
    {
        public int Days { get; set; }
    }

    /// <summary>
    /// Request DTO cho update ResponseHoursRequest
    /// </summary>
    public class UpdateRefundResponseHoursRequest
    {
        public int Hours { get; set; }
    }
}