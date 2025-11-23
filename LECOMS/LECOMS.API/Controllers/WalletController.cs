using LECOMS.Common.Helper;
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

        // ----------------- Helpers -----------------

        private IActionResult Success(object data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return StatusCode((int)statusCode, new APIResponse
            {
                StatusCode = statusCode,
                IsSuccess = true,
                Result = data
            });
        }

        private IActionResult Error(string message, HttpStatusCode statusCode)
        {
            return StatusCode((int)statusCode, new APIResponse
            {
                StatusCode = statusCode,
                IsSuccess = false,
                ErrorMessages = new() { message }
            });
        }

        // ============================================================
        // =============== SHOP WALLET (SELLER) ========================
        // ============================================================

        [HttpGet("shop/summary")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopWalletSummary()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Error("Unauthorized", HttpStatusCode.Unauthorized);

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                    return Error("Shop not found for this user", HttpStatusCode.NotFound);

                var summary = await _shopWalletService.GetWalletSummaryAsync(shop.Id);

                return Success(new
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
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop wallet summary");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("shop/transactions")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetShopWalletTransactions(int page = 1, int pageSize = 20)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 20;

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Error("Unauthorized", HttpStatusCode.Unauthorized);

                var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == userId);
                if (shop == null)
                    return Error("Shop not found for this user", HttpStatusCode.NotFound);

                var wallet = await _shopWalletService.GetWalletWithTransactionsAsync(shop.Id, page, pageSize);
                if (wallet == null)
                    return Error("Wallet not found", HttpStatusCode.NotFound);

                // paginate trên bộ Transactions đã include
                var query = wallet.Transactions
                    .OrderByDescending(t => t.CreatedAt);

                var totalItems = query.Count();
                var items = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
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
                    });

                return Success(new
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
                    transactions = items,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop wallet transactions");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        // ============================================================
        // =============== CUSTOMER WALLET (CUSTOMER) ==================
        // ============================================================

        [HttpGet("customer/balance")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> GetCustomerWalletBalance()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Error("Unauthorized", HttpStatusCode.Unauthorized);

                var wallet = await _customerWalletService.GetOrCreateWalletAsync(userId);

                return Success(new
                {
                    customerId = userId,
                    balance = wallet.Balance,
                    totalRefunded = wallet.TotalRefunded,
                    totalSpent = wallet.TotalSpent,
                    totalWithdrawn = wallet.TotalWithdrawn,
                    lastUpdated = wallet.LastUpdated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer wallet balance");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("customer/transactions")]
        [Authorize(Roles = "Customer, Seller")]
        public async Task<IActionResult> GetCustomerWalletTransactions(int page = 1, int pageSize = 20)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 20;

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Error("Unauthorized", HttpStatusCode.Unauthorized);

                var wallet = await _customerWalletService.GetWalletWithTransactionsAsync(userId, page, pageSize);
                if (wallet == null)
                    return Error("Wallet not found", HttpStatusCode.NotFound);

                var query = wallet.Transactions
                    .OrderByDescending(t => t.CreatedAt);

                var totalItems = query.Count();
                var items = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
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
                    });

                return Success(new
                {
                    walletId = wallet.Id,
                    customerId = userId,
                    balance = wallet.Balance,
                    totalRefunded = wallet.TotalRefunded,
                    totalSpent = wallet.TotalSpent,
                    totalWithdrawn = wallet.TotalWithdrawn,
                    lastUpdated = wallet.LastUpdated,
                    transactions = items,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems,
                        totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer wallet transactions");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        // ============================================================
        // =============== ADMIN — Platform Statistics =================
        // ============================================================

        [HttpGet("admin/platform-statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPlatformStatistics()
        {
            try
            {
                var totalShopAvailableBalance = await _unitOfWork.ShopWallets.GetTotalAvailableBalanceAsync();
                var totalShopPendingBalance = await _unitOfWork.ShopWallets.GetTotalPendingBalanceAsync();
                var totalCustomerBalance = await _unitOfWork.CustomerWallets.GetTotalBalanceAsync();

                var startDate = new DateTime(2020, 1, 1);
                var endDate = DateTime.UtcNow;
                var totalPlatformFee = await _unitOfWork.Transactions.GetTotalPlatformFeeAsync(startDate, endDate);

                var totalShopsWithWallet = await _unitOfWork.ShopWallets.CountAsync(_ => true);
                var totalCustomersWithWallet = await _unitOfWork.CustomerWallets.CountAsync(_ => true);

                var pendingShopWithdrawals = await _unitOfWork.WithdrawalRequests.GetPendingRequestsAsync();
                var pendingCustomerWithdrawals = await _unitOfWork.CustomerWithdrawalRequests.GetPendingRequestsAsync();
                var pendingRefunds = await _unitOfWork.RefundRequests.GetByStatusAsync(RefundStatus.PendingShop);

                decimal platformAvailableBalance =
                    totalPlatformFee - (totalShopAvailableBalance + totalShopPendingBalance + totalCustomerBalance);

                return Success(new
                {
                    shops = new
                    {
                        totalShopsWithWallet,
                        totalAvailableBalance = totalShopAvailableBalance,
                        totalPendingBalance = totalShopPendingBalance,
                        totalShopBalance = totalShopAvailableBalance + totalShopPendingBalance
                    },
                    customers = new
                    {
                        totalCustomersWithWallet,
                        totalBalance = totalCustomerBalance
                    },
                    platform = new
                    {
                        totalFeeEarned = totalPlatformFee,
                        availableBalance = platformAvailableBalance,
                        totalLockedInSystem = totalShopAvailableBalance + totalShopPendingBalance + totalCustomerBalance
                    },
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
                    summary = new
                    {
                        totalMoneyInSystem =
                            totalShopAvailableBalance + totalShopPendingBalance + totalCustomerBalance + platformAvailableBalance,
                        healthStatus = platformAvailableBalance >= 0 ? "Healthy" : "Warning: Negative platform balance"
                    },
                    generatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting platform statistics");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("admin/top-shops")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopShopsByBalance(int limit = 10)
        {
            try
            {
                if (limit <= 0) limit = 10;

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

                return Success(topShops);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top shops");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("admin/top-customers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTopCustomersByBalance(int limit = 10)
        {
            try
            {
                if (limit <= 0) limit = 10;

                var wallets = await _unitOfWork.CustomerWallets.GetWalletsWithBalanceAsync(1, limit);

                return Success(wallets.Select(w => new
                {
                    customerId = w.CustomerId,
                    customerName = w.Customer.UserName,
                    customerEmail = w.Customer.Email,
                    balance = w.Balance,
                    totalRefunded = w.TotalRefunded,
                    totalSpent = w.TotalSpent,
                    totalWithdrawn = w.TotalWithdrawn
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet("admin/revenue-report")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRevenueReport(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
                var toDate = to ?? DateTime.UtcNow;

                var totalPlatformFee = await _unitOfWork.Transactions.GetTotalPlatformFeeAsync(fromDate, toDate);
                var transactions = await _unitOfWork.Transactions
                    .GetByDateRangeAsync(fromDate, toDate, includeProperties: "TransactionOrders,TransactionOrders.Order");

                return Success(new
                {
                    period = new
                    {
                        from = fromDate,
                        to = toDate,
                        days = (toDate - fromDate).Days
                    },
                    revenue = new
                    {
                        totalPlatformFee,
                        totalTransactions = transactions.Count(),
                        totalAmount = transactions.Sum(t => t.TotalAmount),
                        averageFeePerTransaction = transactions.Any()
                            ? totalPlatformFee / transactions.Count()
                            : 0
                    },
                    transactions = transactions.Select(t => new
                    {
                        id = t.Id,

                        // ⭐ NEW: Dùng mapping table để lấy mọi order liên quan
                        orderIds = t.TransactionOrders
                            .Select(to => to.OrderId)
                            .ToList(),

                        orderCodes = t.TransactionOrders
                            .Select(to => to.Order.OrderCode)
                            .ToList(),

                        // Original fields
                        totalAmount = t.TotalAmount,
                        platformFee = t.PlatformFeeAmount,
                        shopAmount = t.ShopAmount,
                        status = t.Status.ToString(),
                        createdAt = t.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating revenue report");
                return Error("Internal server error", HttpStatusCode.InternalServerError);
            }
        }

    }
}
