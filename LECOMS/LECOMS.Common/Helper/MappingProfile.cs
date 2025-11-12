using AutoMapper;
using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.DTOs.Cart;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Order;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.DTOs.Refund;
using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.DTOs.Transaction;
using LECOMS.Data.DTOs.User;
using LECOMS.Data.DTOs.Wallet;
using LECOMS.Data.DTOs.Withdrawal;
using LECOMS.Data.Entities;
using System;
using System.Linq;

namespace LECOMS.Common.Helper
{
    /// <summary>
    /// AutoMapper Profile configuration - UPDATED for Marketplace Payment
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ============================================================
            // USER MAPPING
            // ============================================================
            CreateMap<User, UserDTO>();

            CreateMap<UserRegisterDTO, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // ============================================================
            // SHOP & SELLER MAPPING
            // ============================================================
            CreateMap<Shop, ShopDTO>()
                .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ReverseMap();

            CreateMap<SellerRegistrationRequestDTO, Shop>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.ShopName))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.ShopDescription))
                .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.ShopPhoneNumber))
                .ForMember(d => d.Address, o => o.MapFrom(s => s.ShopAddress))
                .ForMember(d => d.BusinessType, o => o.MapFrom(s => s.BusinessType))
                .ForMember(d => d.OwnershipDocumentUrl, o => o.MapFrom(s => s.OwnershipDocumentUrl))
                .ForMember(d => d.ShopAvatar, o => o.MapFrom(s => s.ShopAvatar))
                .ForMember(d => d.ShopBanner, o => o.MapFrom(s => s.ShopBanner))
                .ForMember(d => d.ShopFacebook, o => o.MapFrom(s => s.ShopFacebook))
                .ForMember(d => d.ShopTiktok, o => o.MapFrom(s => s.ShopTiktok))
                .ForMember(d => d.ShopInstagram, o => o.MapFrom(s => s.ShopInstagram))
                .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId))
                .ForMember(d => d.AcceptedTerms, o => o.MapFrom(s => s.AcceptedTerms))
                .ForMember(d => d.OwnerFullName, o => o.MapFrom(s => s.OwnerFullName))
                .ForMember(d => d.OwnerDateOfBirth, o => o.MapFrom(s => s.OwnerDateOfBirth))
                .ForMember(d => d.OwnerPersonalIdNumber, o => o.MapFrom(s => s.OwnerPersonalIdNumber))
                .ForMember(d => d.OwnerPersonalIdFrontUrl, o => o.MapFrom(s => s.OwnerPersonalIdFrontUrl))
                .ForMember(d => d.OwnerPersonalIdBackUrl, o => o.MapFrom(s => s.OwnerPersonalIdBackUrl))
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.ApprovedAt, o => o.Ignore());

            CreateMap<ShopUpdateDTO, Shop>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // ============================================================
            // COURSE CATEGORY MAPPING
            // ============================================================
            CreateMap<CourseCategory, CourseCategoryDTO>().ReverseMap();

            CreateMap<CourseCategoryCreateDTO, CourseCategory>()
                .ForMember(d => d.Id, o => o.Ignore());

            // ============================================================
            // COURSE MAPPING
            // ============================================================
            CreateMap<Course, CourseDTO>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(dest => dest.ShopName, opt => opt.MapFrom(src => src.Shop.Name))
                .ForMember(d => d.ShopAvatar, o => o.MapFrom(s => s.Shop.ShopAvatar))
                .ReverseMap();

            CreateMap<CreateCourseDto, Course>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Active, opt => opt.MapFrom(_ => (byte)1))
                .ForMember(dest => dest.Shop, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            CreateMap<UpdateCourseDto, Course>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // ============================================================
            // LESSON & SECTION MAPPING
            // ============================================================
            CreateMap<Lesson, LessonDto>();

            CreateMap<CreateLessonDto, Lesson>()
                .ForMember(d => d.Id, o => o.Ignore());

            CreateMap<CourseSection, SectionDTO>()
                .ForMember(dest => dest.Lessons, opt => opt.MapFrom(src => src.Lessons))
                .ReverseMap();

            // ============================================================
            // PRODUCT CATEGORY MAPPING
            // ============================================================
            CreateMap<ProductCategory, ProductCategoryDTO>().ReverseMap();

            CreateMap<ProductCategoryCreateDTO, ProductCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Active, opt => opt.MapFrom(_ => (byte)1));

            // ============================================================
            // PRODUCT MAPPING
            // ============================================================
            CreateMap<ProductImage, ProductImageDTO>().ReverseMap();

            CreateMap<Product, ProductDTO>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.Images, o => o.MapFrom(s => s.Images))
                .ForMember(d => d.ThumbnailUrl,
                    o => o.MapFrom(s => s.Images.FirstOrDefault(i => i.IsPrimary).Url))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.ShopAvatar, o => o.MapFrom(s => s.Shop.ShopAvatar))
                .ForMember(d => d.ShopDescription, o => o.MapFrom(s => s.Shop.Description))
                .ReverseMap();

            CreateMap<ProductCreateDTO, Product>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Slug, o => o.Ignore())
                .ForMember(d => d.Active, o => o.MapFrom(_ => (byte)1))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status ?? Data.Enum.ProductStatus.Draft))
                .ForMember(d => d.Images, o => o.Ignore());

            CreateMap<ProductUpdateDTO, Product>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // ============================================================
            // ⭐ CART MAPPING
            // ============================================================
            CreateMap<Cart, CartDTO>()
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items))
                .ForMember(d => d.Subtotal, o => o.MapFrom(s =>
                    s.Items.Sum(i => i.Product.Price * i.Quantity)));

            CreateMap<CartItem, CartItemDTO>()
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
                .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.Product.Price))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => s.Quantity))
                .ForMember(d => d.ProductImage, o => o.MapFrom(s =>
                    s.Product.Images.FirstOrDefault(i => i.IsPrimary).Url));

            // ============================================================
            // ⭐ ORDER MAPPING
            // ============================================================
            CreateMap<Order, OrderDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.OrderCode, o => o.MapFrom(s => s.OrderCode))
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User.UserName))
                .ForMember(d => d.ShipToName, o => o.MapFrom(s => s.ShipToName))
                .ForMember(d => d.ShipToPhone, o => o.MapFrom(s => s.ShipToPhone))
                .ForMember(d => d.ShipToAddress, o => o.MapFrom(s => s.ShipToAddress))
                .ForMember(d => d.Subtotal, o => o.MapFrom(s => s.Subtotal))
                .ForMember(d => d.ShippingFee, o => o.MapFrom(s => s.ShippingFee))
                .ForMember(d => d.Discount, o => o.MapFrom(s => s.Discount))
                .ForMember(d => d.Total, o => o.MapFrom(s => s.Total))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()))
                .ForMember(d => d.BalanceReleased, o => o.MapFrom(s => s.BalanceReleased))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.CompletedAt, o => o.MapFrom(s => s.CompletedAt))
                .ForMember(d => d.Details, o => o.MapFrom(s => s.Details));

            CreateMap<OrderDetail, OrderDetailDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
                .ForMember(d => d.ProductImage, o => o.MapFrom(s =>
                    s.Product.Images.FirstOrDefault(i => i.IsPrimary).Url))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => s.Quantity))
                .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.UnitPrice))
                .ForMember(d => d.ProductSku, o => o.MapFrom(s => s.Product.Id))
                .ForMember(d => d.ProductCategory, o => o.MapFrom(s => s.Product.Category.Name));

            // ============================================================
            // ⭐ TRANSACTION MAPPING - FIXED
            // ============================================================
            CreateMap<Transaction, TransactionDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.OrderId, o => o.MapFrom(s => s.OrderId))
                // ❌ REMOVED: OrderCode mapping (vì không có navigation property Order)
                // .ForMember(d => d.OrderCode, o => o.MapFrom(s => s.Order.OrderCode))
                .ForMember(d => d.OrderCode, o => o.Ignore())  // Set manually if needed
                .ForMember(d => d.TotalAmount, o => o.MapFrom(s => s.TotalAmount))
                .ForMember(d => d.PlatformFeePercent, o => o.MapFrom(s => s.PlatformFeePercent))
                .ForMember(d => d.PlatformFeeAmount, o => o.MapFrom(s => s.PlatformFeeAmount))
                .ForMember(d => d.ShopAmount, o => o.MapFrom(s => s.ShopAmount))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod))
                .ForMember(d => d.PayOSTransactionId, o => o.MapFrom(s => s.PayOSTransactionId))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.CompletedAt, o => o.MapFrom(s => s.CompletedAt));

            // ============================================================
            // ⭐ WALLET MAPPING
            // ============================================================
            CreateMap<ShopWallet, ShopWalletDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.AvailableBalance, o => o.MapFrom(s => s.AvailableBalance))
                .ForMember(d => d.PendingBalance, o => o.MapFrom(s => s.PendingBalance))
                .ForMember(d => d.TotalEarned, o => o.MapFrom(s => s.TotalEarned))
                .ForMember(d => d.TotalWithdrawn, o => o.MapFrom(s => s.TotalWithdrawn))
                .ForMember(d => d.TotalRefunded, o => o.MapFrom(s => s.TotalRefunded))
                .ForMember(d => d.LastUpdated, o => o.MapFrom(s => s.LastUpdated));

            CreateMap<CustomerWallet, CustomerWalletDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.UserName))
                .ForMember(d => d.Balance, o => o.MapFrom(s => s.Balance))
                .ForMember(d => d.TotalRefunded, o => o.MapFrom(s => s.TotalRefunded))
                .ForMember(d => d.TotalSpent, o => o.MapFrom(s => s.TotalSpent))
                .ForMember(d => d.TotalWithdrawn, o => o.MapFrom(s => s.TotalWithdrawn))
                .ForMember(d => d.LastUpdated, o => o.MapFrom(s => s.LastUpdated));

            CreateMap<WalletTransaction, WalletTransactionDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount))
                .ForMember(d => d.BalanceType, o => o.MapFrom(s => s.BalanceType))
                .ForMember(d => d.BalanceBefore, o => o.MapFrom(s => s.BalanceBefore))
                .ForMember(d => d.BalanceAfter, o => o.MapFrom(s => s.BalanceAfter))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
                .ForMember(d => d.ReferenceId, o => o.MapFrom(s => s.ReferenceId))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

            CreateMap<CustomerWalletTransaction, CustomerWalletTransactionDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount))
                .ForMember(d => d.BalanceBefore, o => o.MapFrom(s => s.BalanceBefore))
                .ForMember(d => d.BalanceAfter, o => o.MapFrom(s => s.BalanceAfter))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
                .ForMember(d => d.ReferenceId, o => o.MapFrom(s => s.ReferenceId))
                .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt));

            // ============================================================
            // ⭐ REFUND MAPPING
            // ============================================================
            CreateMap<RefundRequest, RefundRequestDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.OrderId, o => o.MapFrom(s => s.OrderId))
                .ForMember(d => d.OrderCode, o => o.MapFrom(s => s.Order.OrderCode))
                .ForMember(d => d.RefundAmount, o => o.MapFrom(s => s.RefundAmount))
                .ForMember(d => d.ReasonType, o => o.MapFrom(s => s.ReasonType.ToString()))
                .ForMember(d => d.ReasonDescription, o => o.MapFrom(s => s.ReasonDescription))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.RequestedBy, o => o.MapFrom(s => s.RequestedBy))
                .ForMember(d => d.RequestedByName, o => o.MapFrom(s => s.RequestedByUser.UserName))
                .ForMember(d => d.RequestedAt, o => o.MapFrom(s => s.RequestedAt))
                .ForMember(d => d.ProcessedBy, o => o.MapFrom(s => s.ShopResponseBy))
                .ForMember(d => d.ProcessedByName, o => o.MapFrom(s => s.ShopResponseByUser != null ? s.ShopResponseByUser.UserName : null))
                .ForMember(d => d.ProcessedAt, o => o.MapFrom(s => s.ProcessedAt));

            // ============================================================
            // ⭐ WITHDRAWAL MAPPING
            // ============================================================
            CreateMap<WithdrawalRequest, WithdrawalRequestDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount))
                .ForMember(d => d.BankName, o => o.MapFrom(s => s.BankName))
                .ForMember(d => d.BankAccountNumber, o => o.MapFrom(s => s.BankAccountNumber))
                .ForMember(d => d.BankAccountName, o => o.MapFrom(s => s.BankAccountName))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.RequestedAt, o => o.MapFrom(s => s.RequestedAt))
                .ForMember(d => d.ApprovedAt, o => o.MapFrom(s => s.ApprovedAt))
                .ForMember(d => d.CompletedAt, o => o.MapFrom(s => s.CompletedAt));

            CreateMap<CustomerWithdrawalRequest, CustomerWithdrawalRequestDTO>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.UserName))
                .ForMember(d => d.Amount, o => o.MapFrom(s => s.Amount))
                .ForMember(d => d.BankName, o => o.MapFrom(s => s.BankName))
                .ForMember(d => d.BankAccountNumber, o => o.MapFrom(s => s.BankAccountNumber))
                .ForMember(d => d.BankAccountName, o => o.MapFrom(s => s.BankAccountName))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.RequestedAt, o => o.MapFrom(s => s.RequestedAt))
                .ForMember(d => d.ApprovedAt, o => o.MapFrom(s => s.ApprovedAt))
                .ForMember(d => d.CompletedAt, o => o.MapFrom(s => s.CompletedAt));
        }
    }
}