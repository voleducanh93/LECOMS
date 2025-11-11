using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.Data.DTOs.Refund;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class RefundService : IRefundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IShopWalletService _shopWalletService;
        private readonly ICustomerWalletService _customerWalletService;
        private readonly ILogger<RefundService> _logger;

        public RefundService(
            IUnitOfWork unitOfWork,
            IShopWalletService shopWalletService,
            ICustomerWalletService customerWalletService,
            ILogger<RefundService> logger)
        {
            _unitOfWork = unitOfWork;
            _shopWalletService = shopWalletService;
            _customerWalletService = customerWalletService;
            _logger = logger;
        }

        public async Task<RefundRequest> CreateRefundRequestAsync(CreateRefundRequestDto dto)
        {
            _logger.LogInformation(
                "Customer {CustomerId} creating refund for Order {OrderId}",
                dto.RequestedBy, dto.OrderId);

            // 1. Validate order
            var order = await _unitOfWork.Orders.GetAsync(
                o => o.Id == dto.OrderId,
                includeProperties: "Shop,User");

            if (order == null)
                throw new InvalidOperationException($"Order {dto.OrderId} not found");

            if (order.UserId != dto.RequestedBy)
                throw new UnauthorizedAccessException("You can only refund your own orders");

            if (order.PaymentStatus != PaymentStatus.Paid &&
                order.PaymentStatus != PaymentStatus.PartiallyRefunded)
            {
                throw new InvalidOperationException(
                    $"Order payment status is {order.PaymentStatus}. Cannot refund unpaid orders.");
            }

            // 2. Check order status
            if (order.Status != OrderStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Can only refund delivered or completed orders");
            }

            // 3. Validate amount
            var refundableAmount = await GetRefundableAmountAsync(dto.OrderId);
            if (dto.RefundAmount > refundableAmount)
            {
                throw new InvalidOperationException(
                    $"Refund amount {dto.RefundAmount:N0} exceeds refundable amount {refundableAmount:N0}");
            }

            if (dto.RefundAmount <= 0)
                throw new ArgumentException("Refund amount must be positive");

            // 4. Check existing pending requests
            var existingPending = await _unitOfWork.RefundRequests.GetAsync(r =>
                r.OrderId == dto.OrderId &&
                r.Status == RefundStatus.PendingShopApproval);

            if (existingPending != null)
                throw new InvalidOperationException("Already have a pending refund request for this order");

            // 5. Fraud detection
            var customerStats = await GetCustomerRefundStatisticsAsync(dto.RequestedBy);
            bool isFlagged = customerStats.RefundRate > 0.30m;
            string? flagReason = isFlagged ? $"High refund rate: {customerStats.RefundRate:P0}" : null;

            if (customerStats.RefundRate > 0.50m)
            {
                throw new InvalidOperationException(
                    "Your account has been flagged for suspicious refund activity. Please contact support.");
            }

            // 6. Create RefundRequest
            var refundRequest = new RefundRequest
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = dto.OrderId,
                RequestedBy = dto.RequestedBy,
                ReasonType = dto.ReasonType,
                ReasonDescription = dto.ReasonDescription,
                Type = dto.Type,
                RefundAmount = dto.RefundAmount,
                Status = RefundStatus.PendingShopApproval,
                RequestedAt = DateTime.UtcNow,
                AttachmentUrls = dto.AttachmentUrls,
                IsFlagged = isFlagged,
                FlagReason = flagReason
            };

            await _unitOfWork.RefundRequests.AddAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "✅ Refund request created: {RefundId}. Shop can approve/reject anytime.",
                refundRequest.Id);

            return refundRequest;
        }

        public async Task<RefundRequest> ShopApproveRefundAsync(string refundId, string shopUserId)
        {
            _logger.LogInformation(
                "Shop user {ShopUserId} approving refund {RefundId}",
                shopUserId, refundId);

            var refundRequest = await _unitOfWork.RefundRequests
                .GetByIdWithDetailsAsync(refundId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundId} not found");

            // Verify shop ownership
            var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == shopUserId);
            if (shop == null || shop.Id != refundRequest.Order.ShopId)
                throw new UnauthorizedAccessException("You can only respond to your shop's refund requests");

            if (refundRequest.Status != RefundStatus.PendingShopApproval)
                throw new InvalidOperationException($"Cannot approve. Status: {refundRequest.Status}");

            // Update status
            refundRequest.Status = RefundStatus.Processing;
            refundRequest.ShopResponseBy = shopUserId;
            refundRequest.ShopRespondedAt = DateTime.UtcNow;

            await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            // Process refund
            await ProcessRefundAsync(refundId);

            _logger.LogInformation("✅ Shop approved refund {RefundId}. Refund processed.", refundId);

            return refundRequest;
        }

        public async Task<RefundRequest> ShopRejectRefundAsync(
            string refundId,
            string shopUserId,
            string reason)
        {
            _logger.LogInformation(
                "Shop user {ShopUserId} rejecting refund {RefundId}",
                shopUserId, refundId);

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 20)
                throw new ArgumentException("Reject reason must be at least 20 characters");

            var refundRequest = await _unitOfWork.RefundRequests
                .GetByIdWithDetailsAsync(refundId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundId} not found");

            // Verify shop ownership
            var shop = await _unitOfWork.Shops.GetAsync(s => s.SellerId == shopUserId);
            if (shop == null || shop.Id != refundRequest.Order.ShopId)
                throw new UnauthorizedAccessException("You can only respond to your shop's refund requests");

            if (refundRequest.Status != RefundStatus.PendingShopApproval)
                throw new InvalidOperationException($"Cannot reject. Status: {refundRequest.Status}");

            // Update status
            refundRequest.Status = RefundStatus.ShopRejected;
            refundRequest.ShopResponseBy = shopUserId;
            refundRequest.ShopRespondedAt = DateTime.UtcNow;
            refundRequest.ShopRejectReason = reason;

            await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "❌ Shop rejected refund {RefundId}. Customer can review shop.",
                refundId);

            return refundRequest;
        }

        public async Task<RefundRequest> CancelRefundRequestAsync(string refundId, string customerId)
        {
            var refundRequest = await _unitOfWork.RefundRequests.GetAsync(r => r.Id == refundId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundId} not found");

            if (refundRequest.RequestedBy != customerId)
                throw new UnauthorizedAccessException("You can only cancel your own refund requests");

            if (refundRequest.Status != RefundStatus.PendingShopApproval)
                throw new InvalidOperationException("Can only cancel pending requests");

            refundRequest.Status = RefundStatus.Cancelled;
            await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Customer cancelled refund {RefundId}", refundId);

            return refundRequest;
        }

        private async Task ProcessRefundAsync(string refundId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var refundRequest = await _unitOfWork.RefundRequests
                    .GetByIdWithDetailsAsync(refundId);

                if (refundRequest == null)
                    throw new InvalidOperationException($"Refund {refundId} not found");

                var order = refundRequest.Order;

                // Get transaction for platform fee
                var orderTransaction = await _unitOfWork.Transactions.GetByOrderIdAsync(order.Id);
                if (orderTransaction == null)
                    throw new InvalidOperationException($"Transaction not found for Order {order.Id}");

                // Calculate amounts
                decimal platformFeeRatio = orderTransaction.PlatformFeePercent / 100m;
                decimal shopDeduction = refundRequest.RefundAmount * (1 - platformFeeRatio);

                _logger.LogInformation(
                    "Processing refund: Customer +{CustomerAmount:N0}, Shop -{ShopAmount:N0}",
                    refundRequest.RefundAmount, shopDeduction);

                // Add to CustomerWallet
                await _customerWalletService.AddBalanceAsync(
                    order.UserId,
                    refundRequest.RefundAmount,
                    refundRequest.Id,
                    $"Hoàn tiền đơn hàng {order.OrderCode} - {refundRequest.ReasonDescription}");

                // Deduct from ShopWallet
                await _shopWalletService.DeductBalanceAsync(
                    order.ShopId,
                    shopDeduction,
                    WalletTransactionType.Refund,
                    refundRequest.Id,
                    $"Hoàn tiền cho khách - Đơn {order.OrderCode}");

                // Update refund status
                refundRequest.Status = RefundStatus.Completed;
                refundRequest.ProcessedAt = DateTime.UtcNow;
                await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);

                // Update order payment status
                var totalRefunded = await GetTotalRefundedAmountAsync(order.Id);
                if (totalRefunded >= order.Total)
                {
                    order.PaymentStatus = PaymentStatus.Refunded;
                }
                else if (totalRefunded > 0)
                {
                    order.PaymentStatus = PaymentStatus.PartiallyRefunded;
                }
                await _unitOfWork.Orders.UpdateAsync(order);

                // Update transaction status
                if (totalRefunded >= orderTransaction.TotalAmount)
                {
                    orderTransaction.Status = TransactionStatus.Refunded;
                }
                else if (totalRefunded > 0)
                {
                    orderTransaction.Status = TransactionStatus.PartiallyRefunded;
                }
                await _unitOfWork.Transactions.UpdateAsync(orderTransaction);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("✅ Refund {RefundId} processed successfully", refundId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing refund {RefundId}", refundId);

                var refundRequest = await _unitOfWork.RefundRequests.GetAsync(r => r.Id == refundId);
                if (refundRequest != null)
                {
                    refundRequest.Status = RefundStatus.Failed;
                    refundRequest.ProcessNote = $"Error: {ex.Message}";
                    await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
                    await _unitOfWork.CompleteAsync();
                }

                await transaction.RollbackAsync();
                throw;
            }
        }

        // Query methods
        public async Task<RefundRequest?> GetRefundRequestAsync(string refundId)
        {
            return await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundId);
        }

        public async Task<IEnumerable<RefundRequest>> GetRefundRequestsByUserAsync(
            string userId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await _unitOfWork.RefundRequests.GetByRequestedByAsync(userId, pageNumber, pageSize);
        }

        public async Task<IEnumerable<RefundRequest>> GetRefundRequestsByOrderAsync(string orderId)
        {
            return await _unitOfWork.RefundRequests.GetByOrderIdAsync(orderId);
        }

        public async Task<IEnumerable<RefundRequest>> GetShopPendingRefundsAsync(int shopId)
        {
            return await _unitOfWork.RefundRequests.GetByShopIdAsync(shopId, RefundStatus.PendingShopApproval);
        }

        public async Task<IEnumerable<RefundRequest>> GetShopRefundHistoryAsync(
            int shopId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            var allRefunds = await _unitOfWork.RefundRequests.GetByShopIdAsync(shopId);
            var skip = (pageNumber - 1) * pageSize;
            return allRefunds
                .OrderByDescending(r => r.RequestedAt)
                .Skip(skip)
                .Take(pageSize);
        }

        public async Task<RefundStatistics> GetShopRefundStatisticsAsync(int shopId)
        {
            var refunds = (await _unitOfWork.RefundRequests.GetByShopIdAsync(shopId)).ToList();

            var total = refunds.Count;
            var approved = refunds.Count(r => r.Status == RefundStatus.Completed);
            var rejected = refunds.Count(r => r.Status == RefundStatus.ShopRejected);
            var responded = refunds.Count(r => r.ShopRespondedAt.HasValue);

            var responseTimes = refunds
                .Where(r => r.ShopRespondedAt.HasValue)
                .Select(r => (r.ShopRespondedAt!.Value - r.RequestedAt).TotalHours);

            return new RefundStatistics
            {
                TotalRequests = total,
                ApprovedCount = approved,
                RejectedCount = rejected,
                ApprovalRate = total > 0 ? (decimal)approved / total : 0,
                ResponseRate = total > 0 ? (decimal)responded / total : 0,
                AverageResponseTimeHours = responseTimes.Any() ? responseTimes.Average() : 0
            };
        }

        public async Task<CustomerRefundStatistics> GetCustomerRefundStatisticsAsync(string customerId)
        {
            var orders = (await _unitOfWork.Orders.GetAllAsync(o => o.UserId == customerId)).ToList();
            var refunds = (await _unitOfWork.RefundRequests.GetAllAsync(r => r.RequestedBy == customerId)).ToList();

            var totalOrders = orders.Count;
            var totalRefunds = refunds.Count(r => r.Status == RefundStatus.Completed);
            var refundRate = totalOrders > 0 ? (decimal)totalRefunds / totalOrders : 0;

            return new CustomerRefundStatistics
            {
                TotalOrders = totalOrders,
                TotalRefunds = totalRefunds,
                RefundRate = refundRate,
                IsHighRisk = refundRate > 0.30m
            };
        }

        private async Task<decimal> GetRefundableAmountAsync(string orderId)
        {
            var order = await _unitOfWork.Orders.GetAsync(o => o.Id == orderId);
            if (order == null) return 0;

            var totalRefunded = await GetTotalRefundedAmountAsync(orderId);
            return order.Total - totalRefunded;
        }

        private async Task<decimal> GetTotalRefundedAmountAsync(string orderId)
        {
            var refunds = await _unitOfWork.RefundRequests.GetByOrderIdAsync(orderId);
            return refunds
                .Where(r => r.Status == RefundStatus.Completed)
                .Sum(r => r.RefundAmount);
        }
    }
}