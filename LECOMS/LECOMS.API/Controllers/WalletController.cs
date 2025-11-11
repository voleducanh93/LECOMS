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
    /// Controller quản lý ví (Shop & Customer)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IShopWalletService _shopWalletService;
        private readonly ICustomerWalletService _customerWalletService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<WalletController> _logger;

        public WalletController(
            IShopWalletService shopWalletService,
            ICustomerWalletService customerWalletService,
            IUnitOfWork unitOfWork,
            ILogger<WalletController> logger)
        {
            _shopWalletService = shopWalletService;
            _customerWalletService = customerWalletService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // ==================== SHOP WALLET ====================

        /// <summary>
        /// Shop lấy wallet summary của mình
        /// GET: api/wallet/shop/summary
        /// </summary>
        [HttpGet("shop/summary")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopWalletSummary()
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

                var summary = await _shopWalletService.GetWalletSummaryAsync(shop.Id);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        shopId = shop.Id,
                        shopName = shop.Name,
                        availableBalance = summary.AvailableBalance,
                        pendingBalance = summary.PendingBalance,
                        totalEarned = summary.TotalEarned,
                        totalWithdrawn = summary.TotalWithdrawn,
                        totalRefunded = summary.TotalRefunded,
                        pendingOrdersCount = summary.PendingOrdersCount,
                        lastUpdated = summary.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop wallet summary for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Shop lấy transaction history
        /// GET: api/wallet/shop/transactions?page=1&pageSize=20
        /// </summary>
        [HttpGet("shop/transactions")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopWalletTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
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

                var wallet = await _shopWalletService.GetWalletWithTransactionsAsync(shop.Id, page, pageSize);

                if (wallet == null)
                {
                    return NotFound(new { success = false, message = "Wallet not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        walletId = wallet.Id,
                        shopId = shop.Id,
                        shopName = shop.Name,
                        availableBalance = wallet.AvailableBalance,
                        pendingBalance = wallet.PendingBalance,
                        totalEarned = wallet.TotalEarned,
                        totalWithdrawn = wallet.TotalWithdrawn,
                        totalRefunded = wallet.TotalRefunded,
                        lastUpdated = wallet.LastUpdated,
                        transactions = wallet.Transactions.Select(t => new
                        {
                            id = t.Id,
                            type = t.Type.ToString(),
                            amount = t.Amount,
                            balanceType = t.BalanceType,
                            balanceBefore = t.BalanceBefore,
                            balanceAfter = t.BalanceAfter,
                            description = t.Description,
                            referenceId = t.ReferenceId,
                            referenceType = t.ReferenceType,
                            createdAt = t.CreatedAt,
                            performedBy = t.PerformedBy
                        }).ToList(),
                        pagination = new
                        {
                            currentPage = page,
                            pageSize = pageSize,
                            totalItems = wallet.Transactions.Count
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop wallet transactions for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== CUSTOMER WALLET ====================

        /// <summary>
        /// Customer lấy balance của mình
        /// GET: api/wallet/customer/balance
        /// </summary>
        [HttpGet("customer/balance")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerWalletBalance()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var wallet = await _customerWalletService.GetOrCreateWalletAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        customerId = userId,
                        balance = wallet.Balance,
                        totalRefunded = wallet.TotalRefunded,
                        totalSpent = wallet.TotalSpent,
                        totalWithdrawn = wallet.TotalWithdrawn,
                        lastUpdated = wallet.LastUpdated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer wallet balance for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Customer lấy transaction history
        /// GET: api/wallet/customer/transactions?page=1&pageSize=20
        /// </summary>
        [HttpGet("customer/transactions")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetCustomerWalletTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var wallet = await _customerWalletService.GetWalletWithTransactionsAsync(userId, page, pageSize);

                if (wallet == null)
                {
                    return NotFound(new { success = false, message = "Wallet not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        walletId = wallet.Id,
                        customerId = userId,
                        balance = wallet.Balance,
                        totalRefunded = wallet.TotalRefunded,
                        totalSpent = wallet.TotalSpent,
                        totalWithdrawn = wallet.TotalWithdrawn,
                        lastUpdated = wallet.LastUpdated,
                        transactions = wallet.Transactions.Select(t => new
                        {
                            id = t.Id,
                            type = t.Type.ToString(),
                            amount = t.Amount,
                            balanceBefore = t.BalanceBefore,
                            balanceAfter = t.BalanceAfter,
                            description = t.Description,
                            referenceId = t.ReferenceId,
                            referenceType = t.ReferenceType,
                            createdAt = t.CreatedAt,
                            performedBy = t.PerformedBy
                        }).ToList(),
                        pagination = new
                        {
                            currentPage = page,
                            pageSize = pageSize,
                            totalItems = wallet.Transactions.Count
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer wallet transactions for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        // ==================== ADMIN - Platform Statistics ====================

        /// <summary>
        /// Admin lấy tổng balance của platform
        /// GET: api/wallet/admin/platform-statistics
        /// </summary>
        [HttpGet("admin/platform-statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPlatformStatistics()
        {
            try
            {
                // 1. Tổng balance của tất cả shops
                var totalShopAvailableBalance = await _unitOfWork.ShopWallets.GetTotalAvailableBalanceAsync();
                var totalShopPendingBalance = await _unitOfWork.ShopWallets.GetTotalPendingBalanceAsync();

                // 2. Tổng balance của tất cả customers
                var totalCustomerBalance = await _unitOfWork.CustomerWallets.GetTotalBalanceAsync();

                // 3. Tổng platform fee earned (từ transactions completed)
                var startDate = new DateTime(2020, 1, 1); // Hoặc lấy từ config
                var endDate = DateTime.UtcNow;
                var totalPlatformFee = await _unitOfWork.Transactions.GetTotalPlatformFeeAsync(startDate, endDate);

                // 4. Tổng số shops có wallet
                var totalShopsWithWallet = await _unitOfWork.ShopWallets.CountAsync(_ => true);

                // 5. Tổng số customers có wallet
                var totalCustomersWithWallet = await _unitOfWork.CustomerWallets.CountAsync(_ => true);

                // 6. Tổng số pending withdrawals
                var pendingShopWithdrawals = await _unitOfWork.WithdrawalRequests.GetPendingRequestsAsync();
                var pendingCustomerWithdrawals = await _unitOfWork.CustomerWithdrawalRequests.GetPendingRequestsAsync();

                // 7. Tổng số pending refunds
                var pendingRefunds = await _unitOfWork.RefundRequests.GetPendingRequestsAsync();

                // 8. Platform balance (available cash)
                // Platform balance = Total platform fee - (Shop available + Shop pending + Customer balance)
                decimal platformAvailableBalance = totalPlatformFee - (totalShopAvailableBalance + totalShopPendingBalance + totalCustomerBalance);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        // Shop statistics
                        shops = new
                        {
                            totalShopsWithWallet = totalShopsWithWallet,
                            totalAvailableBalance = totalShopAvailableBalance,
                            totalPendingBalance = totalShopPendingBalance,
                            totalShopBalance = totalShopAvailableBalance + totalShopPendingBalance
                        },

                        // Customer statistics
                        customers = new
                        {
                            totalCustomersWithWallet = totalCustomersWithWallet,
                            totalBalance = totalCustomerBalance
                        },

                        // Platform statistics
                        platform = new
                        {
                            totalFeeEarned = totalPlatformFee,
                            availableBalance = platformAvailableBalance,
                            totalLockedInSystem = totalShopAvailableBalance + totalShopPendingBalance + totalCustomerBalance
                        },

                        // Pending actions
                        pending = new
                        {
                            shopWithdrawals = new
                            {
                                count = pendingShopWithdrawals.Count(),
                                totalAmount = pendingShopWithdrawals.Sum(w => w.Amount)
                            },
                            customerWithdrawals = new
                            {
                                count = pendingCustomerWithdrawals.Count(),
                                totalAmount = pendingCustomerWithdrawals.Sum(w => w.Amount)
                            },
                            refundRequests = new
                            {
                                count = pendingRefunds.Count(),
                                totalAmount = pendingRefunds.Sum(r => r.RefundAmount)
                            }
                        },

                        // Summary
                        summary = new
                        {
                            totalMoneyInSystem = totalShopAvailableBalance + totalShopPendingBalance + totalCustomerBalance + platformAvailableBalance,
                            healthStatus = platformAvailableBalance >= 0 ? "Healthy" : "Warning: Negative platform balance"
                        },

                        generatedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting platform statistics");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Lấy top shops theo balance
        /// GET: api/wallet/admin/top-shops?limit=10
        /// </summary>
        [HttpGet("admin/top-shops")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopShopsByBalance([FromQuery] int limit = 10)
        {
            try
            {
                var allWallets = await _unitOfWork.ShopWallets.GetAllAsync(includeProperties: "Shop");

                var topShops = allWallets
                    .OrderByDescending(w => w.AvailableBalance + w.PendingBalance)
                    .Take(limit)
                    .Select(w => new
                    {
                        shopId = w.ShopId,
                        shopName = w.Shop.Name,
                        availableBalance = w.AvailableBalance,
                        pendingBalance = w.PendingBalance,
                        totalBalance = w.AvailableBalance + w.PendingBalance,
                        totalEarned = w.TotalEarned,
                        totalWithdrawn = w.TotalWithdrawn,
                        totalRefunded = w.TotalRefunded
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = topShops
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top shops by balance");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Lấy customers có balance cao nhất
        /// GET: api/wallet/admin/top-customers?limit=10
        /// </summary>
        [HttpGet("admin/top-customers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopCustomersByBalance([FromQuery] int limit = 10)
        {
            try
            {
                var topCustomers = await _unitOfWork.CustomerWallets.GetWalletsWithBalanceAsync(1, limit);

                return Ok(new
                {
                    success = true,
                    data = topCustomers.Select(w => new
                    {
                        customerId = w.CustomerId,
                        customerName = w.Customer.UserName,
                        customerEmail = w.Customer.Email,
                        balance = w.Balance,
                        totalRefunded = w.TotalRefunded,
                        totalSpent = w.TotalSpent,
                        totalWithdrawn = w.TotalWithdrawn
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers by balance");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Admin: Lấy revenue report theo thời gian
        /// GET: api/wallet/admin/revenue-report?from=2025-01-01&to=2025-01-31
        /// </summary>
        [HttpGet("admin/revenue-report")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = to ?? DateTime.UtcNow;

                // Tổng platform fee trong khoảng thời gian
                var totalPlatformFee = await _unitOfWork.Transactions.GetTotalPlatformFeeAsync(fromDate, toDate);

                // Tổng transactions
                var transactions = await _unitOfWork.Transactions.GetByDateRangeAsync(fromDate, toDate);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        period = new
                        {
                            from = fromDate,
                            to = toDate,
                            days = (toDate - fromDate).Days
                        },
                        revenue = new
                        {
                            totalPlatformFee = totalPlatformFee,
                            totalTransactions = transactions.Count(),
                            totalAmount = transactions.Sum(t => t.TotalAmount),
                            averageFeePerTransaction = transactions.Any() ? totalPlatformFee / transactions.Count() : 0
                        },
                        transactions = transactions.Select(t => new
                        {
                            id = t.Id,
                            orderId = t.OrderId,
                            totalAmount = t.TotalAmount,
                            platformFee = t.PlatformFeeAmount,
                            shopAmount = t.ShopAmount,
                            status = t.Status.ToString(),
                            createdAt = t.CreatedAt
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue report");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}