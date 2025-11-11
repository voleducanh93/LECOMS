using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    /// <summary>
    /// Service xử lý hoàn tiền - MANUAL REFUND TO WALLET
    /// 
    /// BUSINESS RULES:
    /// 1. Tất cả refund đều vào ví nội bộ (KHÔNG dùng PayOS refund API)
    /// 2. Bắt buộc admin approval
    /// 3. Platform GIỮ LẠI phí sàn khi refund to customer (đã cung cấp dịch vụ)
    /// 4. Platform HOÀN LẠI phí sàn khi refund to shop (customer hủy)
    /// 
    /// Author: haupdse170479
    /// Created: 2025-01-06
    /// </summary>
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

        /// <summary>
        /// Tạo refund request
        /// </summary>
        public async Task<RefundRequest> CreateRefundRequestAsync(CreateRefundRequestDto dto)
        {
            _logger.LogInformation("Creating refund request for Order: {OrderId}", dto.OrderId);

            // 1. Validate order
            var order = await _unitOfWork.Orders.GetAsync(
                o => o.Id == dto.OrderId,
                includeProperties: "Shop,User");

            if (order == null)
                throw new InvalidOperationException($"Order {dto.OrderId} not found");

            if (order.PaymentStatus != PaymentStatus.Paid && order.PaymentStatus != PaymentStatus.PartiallyRefunded)
                throw new InvalidOperationException($"Order {dto.OrderId} payment status is {order.PaymentStatus}. Cannot refund.");

            // 2. Validate amount
            var refundableAmount = await GetRefundableAmountAsync(dto.OrderId);
            if (dto.RefundAmount > refundableAmount)
                throw new InvalidOperationException($"Refund amount {dto.RefundAmount} exceeds refundable amount {refundableAmount}");

            if (dto.RefundAmount <= 0)
                throw new ArgumentException("Refund amount must be positive");

            // 3. Auto-determine Recipient từ ReasonType
            var recipient = DetermineRecipient(dto.ReasonType);

            // 4. Validate requester
            if (recipient == RefundRecipient.Customer && dto.RequestedBy != order.UserId)
            {
                // Nếu refund to customer, chỉ customer hoặc shop có thể request
                var requester = await _unitOfWork.Users.GetUserByIdAsync(dto.RequestedBy);
                if (requester?.Shop?.Id != order.ShopId)
                {
                    throw new UnauthorizedAccessException("Only customer or shop can request refund to customer");
                }
            }

            // 5. Tạo RefundRequest
            var refundRequest = new RefundRequest
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = dto.OrderId,
                RequestedBy = dto.RequestedBy,
                Recipient = recipient,
                ReasonType = dto.ReasonType,
                ReasonDescription = dto.ReasonDescription,
                Type = dto.Type,
                RefundAmount = dto.RefundAmount,
                Status = RefundStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                AttachmentUrls = dto.AttachmentUrls
            };

            await _unitOfWork.RefundRequests.AddAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation(
                "Refund request created: {RefundId}, Order: {OrderId}, Amount: {Amount}, Recipient: {Recipient}",
                refundRequest.Id, dto.OrderId, dto.RefundAmount, recipient);

            // TODO: Send notification cho admin

            return refundRequest;
        }

        /// <summary>
        /// Auto-determine recipient từ reason type
        /// </summary>
        private RefundRecipient DetermineRecipient(RefundReason reasonType)
        {
            return reasonType switch
            {
                RefundReason.ShopIssue => RefundRecipient.Customer,
                RefundReason.ShopCancelled => RefundRecipient.Customer,
                RefundReason.CustomerCancelled => RefundRecipient.Shop,
                RefundReason.FraudulentOrder => RefundRecipient.Shop,
                RefundReason.Other => RefundRecipient.Customer, // Default
                _ => RefundRecipient.Customer
            };
        }

        /// <summary>
        /// Admin approve refund request
        /// </summary>
        public async Task<RefundRequest> ApproveRefundAsync(string refundId, string adminId, string? note = null)
        {
            _logger.LogInformation("Admin {AdminId} approving refund {RefundId}", adminId, refundId);

            var refundRequest = await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundId} not found");

            if (refundRequest.Status != RefundStatus.Pending)
                throw new InvalidOperationException($"Refund request {refundId} is not pending. Current status: {refundRequest.Status}");

            // Update status
            refundRequest.Status = RefundStatus.Approved;
            refundRequest.ProcessedBy = adminId;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.ProcessNote = note;

            await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            // Trigger process refund
            await ProcessRefundAsync(refundId);

            return refundRequest;
        }

        /// <summary>
        /// Admin reject refund request
        /// </summary>
        public async Task<RefundRequest> RejectRefundAsync(string refundId, string adminId, string reason)
        {
            _logger.LogInformation("Admin {AdminId} rejecting refund {RefundId}", adminId, refundId);

            var refundRequest = await _unitOfWork.RefundRequests.GetAsync(r => r.Id == refundId);

            if (refundRequest == null)
                throw new InvalidOperationException($"Refund request {refundId} not found");

            if (refundRequest.Status != RefundStatus.Pending)
                throw new InvalidOperationException($"Refund request {refundId} is not pending");

            refundRequest.Status = RefundStatus.Rejected;
            refundRequest.ProcessedBy = adminId;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.ProcessNote = reason;

            await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Refund request {RefundId} rejected", refundId);

            // TODO: Send notification cho requester

            return refundRequest;
        }

        /// <summary>
        /// Xử lý refund (CORE LOGIC)
        /// </summary>
        public async Task<RefundRequest> ProcessRefundAsync(string refundId)
        {
            _logger.LogInformation("Processing refund {RefundId}", refundId);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Lấy refund request với đầy đủ thông tin
                var refundRequest = await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundId);

                if (refundRequest == null)
                    throw new InvalidOperationException($"Refund request {refundId} not found");

                if (refundRequest.Status != RefundStatus.Approved)
                    throw new InvalidOperationException($"Refund request {refundId} is not approved. Current status: {refundRequest.Status}");

                var order = refundRequest.Order;

                // 2. Lấy transaction để tính platform fee
                var orderTransaction = await _unitOfWork.Transactions.GetByOrderIdAsync(order.Id);
                if (orderTransaction == null)
                    throw new InvalidOperationException($"Transaction not found for Order {order.Id}");

                // Update status
                refundRequest.Status = RefundStatus.Processing;
                await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
                await _unitOfWork.CompleteAsync();

                // 3. Xử lý theo recipient
                if (refundRequest.Recipient == RefundRecipient.Customer)
                {
                    await ProcessRefundToCustomerAsync(refundRequest, order, orderTransaction);
                }
                else // RefundRecipient.Shop
                {
                    await ProcessRefundToShopAsync(refundRequest, order, orderTransaction);
                }

                // 4. Update refund request status
                refundRequest.Status = RefundStatus.Completed;
                await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);

                // 5. Update order payment status
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

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Refund {RefundId} processed successfully", refundId);

                // TODO: Send notifications

                return refundRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund {RefundId}", refundId);

                // Update status to failed
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

        /// <summary>
        /// Xử lý refund TO CUSTOMER
        /// Shop Issue, Shop Cancelled → Customer nhận tiền
        /// </summary>
        private async Task ProcessRefundToCustomerAsync(RefundRequest refundRequest, Order order, Transaction orderTransaction)
        {
            _logger.LogInformation(
                "Processing refund to CUSTOMER for Order {OrderId}, Amount: {Amount}",
                order.Id, refundRequest.RefundAmount);

            /*
             * LOGIC:
             * 1. Customer nhận: RefundAmount (vào CustomerWallet)
             * 2. Shop bị trừ: RefundAmount (từ ShopWallet)
             * 3. Platform GIỮ LẠI phí sàn (đã cung cấp dịch vụ)
             * 
             * VD: Order total = 1,000,000, Platform fee = 50,000, Shop amount = 950,000
             * Refund 1,000,000 → Customer nhận 1,000,000, Shop mất 950,000, Platform giữ 50,000
             */

            // 1. Tính số tiền shop bị trừ (không bao gồm platform fee)
            decimal platformFeeRatio = orderTransaction.PlatformFeePercent / 100;
            decimal shopDeduction = refundRequest.RefundAmount * (1 - platformFeeRatio);

            // 2. Cộng tiền vào CustomerWallet
            await _customerWalletService.AddBalanceAsync(
                order.UserId,
                refundRequest.RefundAmount,
                refundRequest.Id,
                $"Hoàn tiền đơn hàng {order.OrderCode} - {refundRequest.ReasonDescription}");

            // 3. Trừ tiền từ ShopWallet
            await _shopWalletService.DeductBalanceAsync(
                order.ShopId,
                shopDeduction,
                WalletTransactionType.Refund,
                refundRequest.Id,
                $"Hoàn tiền cho khách hàng - Đơn {order.OrderCode}");

            // 4. Update transaction status
            var totalRefunded = await GetTotalRefundedAmountAsync(order.Id);
            if (totalRefunded >= orderTransaction.TotalAmount)
            {
                orderTransaction.Status = TransactionStatus.Refunded;
            }
            else if (totalRefunded > 0)
            {
                orderTransaction.Status = TransactionStatus.PartiallyRefunded;
            }

            await _unitOfWork.Transactions.UpdateAsync(orderTransaction);

            _logger.LogInformation(
                "Refund to customer completed. Customer received: {CustomerAmount}, Shop deducted: {ShopAmount}",
                refundRequest.RefundAmount, shopDeduction);
        }

        /// <summary>
        /// Xử lý refund TO SHOP
        /// Customer Cancelled → Shop nhận tiền (bao gồm phí sàn)
        /// </summary>
        private async Task ProcessRefundToShopAsync(RefundRequest refundRequest, Order order, Transaction orderTransaction)
        {
            _logger.LogInformation(
                "Processing refund to SHOP for Order {OrderId}, Amount: {Amount}",
                order.Id, refundRequest.RefundAmount);

            /*
             * LOGIC:
             * 1. Shop nhận: RefundAmount (TOÀN BỘ, bao gồm phí sàn)
             * 2. Customer bị trừ: RefundAmount (từ CustomerWallet nếu có)
             * 3. Platform HOÀN LẠI phí sàn cho shop
             * 
             * VD: Order total = 1,000,000
             * Refund → Shop nhận 1,000,000 (đã bao gồm phí sàn 50,000)
             */

            // 1. Cộng tiền vào ShopWallet (TOÀN BỘ refund amount)
            await _shopWalletService.AddAvailableBalanceAsync(
                order.ShopId,
                refundRequest.RefundAmount,
                refundRequest.Id,
                $"Hoàn tiền do khách hủy đơn - {order.OrderCode}");

            // 2. Trừ tiền từ CustomerWallet (nếu có balance)
            var customerBalance = await _customerWalletService.GetBalanceAsync(order.UserId);
            if (customerBalance >= refundRequest.RefundAmount)
            {
                await _customerWalletService.DeductBalanceAsync(
                    order.UserId,
                    refundRequest.RefundAmount,
                    WalletTransactionType.Refund,
                    refundRequest.Id,
                    $"Hoàn tiền cho shop do hủy đơn {order.OrderCode}");
            }
            else
            {
                // Customer không đủ balance → Log warning (có thể tạo debt record)
                _logger.LogWarning(
                    "Customer {CustomerId} insufficient balance for refund to shop. Required: {Required}, Available: {Available}",
                    order.UserId, refundRequest.RefundAmount, customerBalance);

                // TODO: Tạo CustomerDebt record (optional)
            }

            // 3. Update order status
            order.Status = OrderStatus.Cancelled;

            // 4. Update transaction status
            orderTransaction.Status = TransactionStatus.Refunded;
            await _unitOfWork.Transactions.UpdateAsync(orderTransaction);

            _logger.LogInformation("Refund to shop completed. Shop received: {Amount}", refundRequest.RefundAmount);
        }

        /// <summary>
        /// Lấy refund request by ID
        /// </summary>
        public async Task<RefundRequest?> GetRefundRequestAsync(string refundId)
        {
            return await _unitOfWork.RefundRequests.GetByIdWithDetailsAsync(refundId);
        }

        /// <summary>
        /// Lấy refund requests theo order
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetRefundRequestsByOrderAsync(string orderId)
        {
            return await _unitOfWork.RefundRequests.GetByOrderIdAsync(orderId);
        }

        /// <summary>
        /// Lấy pending refund requests
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetPendingRefundRequestsAsync()
        {
            return await _unitOfWork.RefundRequests.GetPendingRequestsAsync();
        }

        /// <summary>
        /// Lấy refund requests theo user
        /// </summary>
        public async Task<IEnumerable<RefundRequest>> GetRefundRequestsByUserAsync(string userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _unitOfWork.RefundRequests.GetByRequestedByAsync(userId, pageNumber, pageSize);
        }

        /// <summary>
        /// Kiểm tra order có thể refund không
        /// </summary>
        public async Task<bool> CanRefundOrderAsync(string orderId)
        {
            var order = await _unitOfWork.Orders.GetAsync(o => o.Id == orderId);

            if (order == null) return false;

            // Order phải đã thanh toán
            if (order.PaymentStatus != PaymentStatus.Paid && order.PaymentStatus != PaymentStatus.PartiallyRefunded)
                return false;

            // Check MaxRefundDays
            var config = await _unitOfWork.PlatformConfigs.GetConfigAsync();
            if (order.CompletedAt.HasValue)
            {
                var daysSinceCompleted = (DateTime.UtcNow - order.CompletedAt.Value).Days;
                if (daysSinceCompleted > config.MaxRefundDays)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tính số tiền có thể refund
        /// </summary>
        public async Task<decimal> GetRefundableAmountAsync(string orderId)
        {
            var order = await _unitOfWork.Orders.GetAsync(o => o.Id == orderId);
            if (order == null) return 0;

            var totalRefunded = await GetTotalRefundedAmountAsync(orderId);
            return order.Total - totalRefunded;
        }

        /// <summary>
        /// Tính tổng đã refund cho order
        /// </summary>
        private async Task<decimal> GetTotalRefundedAmountAsync(string orderId)
        {
            var refunds = await _unitOfWork.RefundRequests.GetByOrderIdAsync(orderId);
            return refunds
                .Where(r => r.Status == RefundStatus.Completed)
                .Sum(r => r.RefundAmount);
        }
    }
}