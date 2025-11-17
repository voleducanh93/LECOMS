using AutoMapper;
using LECOMS.Data.DTOs.Auth;
using LECOMS.Data.DTOs.Cart;
using LECOMS.Data.DTOs.Chat;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Gamification;
using LECOMS.Data.DTOs.Order;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.DTOs.Refund;
using LECOMS.Data.DTOs.Seller;
using LECOMS.Data.DTOs.Transaction;
using LECOMS.Data.DTOs.User;
using LECOMS.Data.DTOs.Wallet;
using LECOMS.Data.DTOs.Withdrawal;
using LECOMS.Data.Entities;
using System.Linq;

namespace LECOMS.Common.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ============================================================
            // USER
            // ============================================================
            CreateMap<User, UserDTO>();

            CreateMap<UserRegisterDTO, User>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.IsActive, o => o.MapFrom(_ => true));


            // ============================================================
            // SHOP + SELLER
            // ============================================================
            CreateMap<Shop, ShopDTO>()
                .ForMember(d => d.CategoryId, o => o.MapFrom(s => s.CategoryId))
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name));

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
                .ForMember(d => d.OwnerFullName, o => o.MapFrom(s => s.OwnerFullName))
                .ForMember(d => d.OwnerDateOfBirth, o => o.MapFrom(s => s.OwnerDateOfBirth))
                .ForMember(d => d.OwnerPersonalIdNumber, o => o.MapFrom(s => s.OwnerPersonalIdNumber))
                .ForMember(d => d.OwnerPersonalIdFrontUrl, o => o.MapFrom(s => s.OwnerPersonalIdFrontUrl))
                .ForMember(d => d.OwnerPersonalIdBackUrl, o => o.MapFrom(s => s.OwnerPersonalIdBackUrl))
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.ApprovedAt, o => o.Ignore());


            // ============================================================
            // COURSE CATEGORY
            // ============================================================
            CreateMap<CourseCategory, CourseCategoryDTO>();
            CreateMap<CourseCategoryCreateDTO, CourseCategory>()
                .ForMember(d => d.Id, o => o.Ignore());


            // ============================================================
            // COURSE
            // ============================================================
            CreateMap<Course, CourseDTO>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.ShopAvatar, o => o.MapFrom(s => s.Shop.ShopAvatar));

            CreateMap<CreateCourseDto, Course>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Slug, o => o.Ignore())
                .ForMember(d => d.Active, o => o.MapFrom(_ => (byte)1))
                .ForMember(d => d.Shop, o => o.Ignore())
                .ForMember(d => d.Category, o => o.Ignore());

            CreateMap<UpdateCourseDto, Course>()
                .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));


            // ============================================================
            // PRODUCT CATEGORY
            // ============================================================
            CreateMap<ProductCategory, ProductCategoryDTO>();

            CreateMap<ProductCategoryCreateDTO, ProductCategory>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Slug, o => o.Ignore())
                .ForMember(d => d.Active, o => o.MapFrom(_ => (byte)1));


            // ============================================================
            // PRODUCT
            // ============================================================
            CreateMap<ProductImage, ProductImageDTO>();

            CreateMap<Product, ProductDTO>()
                .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
                .ForMember(d => d.Images, o => o.MapFrom(s => s.Images))
                .ForMember(d => d.ThumbnailUrl,
                    o => o.MapFrom(s => s.Images.FirstOrDefault(i => i.IsPrimary).Url))
                .ForMember(d => d.ShopId, o => o.MapFrom(s => s.ShopId))
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.ShopAvatar, o => o.MapFrom(s => s.Shop.ShopAvatar))
                .ForMember(d => d.ShopDescription, o => o.MapFrom(s => s.Shop.Description));

            CreateMap<ProductCreateDTO, Product>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.Slug, o => o.Ignore())
                .ForMember(d => d.Active, o => o.MapFrom(_ => (byte)1))
                .ForMember(d => d.Images, o => o.Ignore());

            CreateMap<ProductUpdateDTO, Product>()
                .ForAllMembers(o => o.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Product, ProductMiniDTO>()
                .ForMember(d => d.Thumbnail,
                    o => o.MapFrom(s => s.Images.FirstOrDefault(i => i.IsPrimary).Url));


            // ============================================================
            // CART
            // ============================================================
            CreateMap<Cart, CartDTO>()
                .ForMember(d => d.Subtotal,
                    o => o.MapFrom(s =>
                        s.Items.Sum(i => i.Product.Price * i.Quantity)));

            CreateMap<CartItem, CartItemDTO>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
                .ForMember(d => d.UnitPrice, o => o.MapFrom(s => s.Product.Price))
                .ForMember(d => d.ProductImage,
                    o => o.MapFrom(s =>
                        s.Product.Images.FirstOrDefault(i => i.IsPrimary).Url));


            // ============================================================
            // ⭐ ORDER + ORDERDETAIL (CHUẨN THEO ENTITY)
            // ============================================================
            CreateMap<Order, OrderDTO>()
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User.FullName))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()))
                .ForMember(d => d.Details, o => o.MapFrom(s => s.Details));

            CreateMap<OrderDetail, OrderDetailDTO>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
                .ForMember(d => d.ProductImage,
                    o => o.MapFrom(s => s.Product.Images.FirstOrDefault(i => i.IsPrimary).Url))
                .ForMember(d => d.ProductCategory,
                    o => o.MapFrom(s => s.Product.Category.Name));
               


            // ============================================================
            // TRANSACTION
            // ============================================================
            CreateMap<Transaction, TransactionDTO>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));


            // ============================================================
            // WALLET
            // ============================================================
            CreateMap<ShopWallet, ShopWalletDTO>()
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name));

            CreateMap<CustomerWallet, CustomerWalletDTO>()
                .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.UserName));

            CreateMap<CustomerWalletTransaction, CustomerWalletTransactionDTO>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

            CreateMap<WalletTransaction, WalletTransactionDTO>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));


            // ============================================================
            // REFUND
            // ============================================================
            CreateMap<RefundRequest, RefundRequestDTO>()
                .ForMember(d => d.OrderCode, o => o.MapFrom(s => s.Order.OrderCode))
                .ForMember(d => d.ReasonType, o => o.MapFrom(s => s.ReasonType.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.RequestedByName,
                    o => o.MapFrom(s => s.RequestedByUser.UserName))
                .ForMember(d => d.ProcessedByName,
                    o => o.MapFrom(s =>
                        s.ShopResponseByUser != null
                            ? s.ShopResponseByUser.UserName
                            : null));


            // ============================================================
            // WITHDRAWAL
            // ============================================================
            CreateMap<WithdrawalRequest, WithdrawalRequestDTO>()
                .ForMember(d => d.ShopName, o => o.MapFrom(s => s.Shop.Name))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            CreateMap<CustomerWithdrawalRequest, CustomerWithdrawalRequestDTO>()
                .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.UserName))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));


            // ============================================================
            // CHAT
            // ============================================================
            CreateMap<Message, MessageDTO>();
            CreateMap<Conversation, ConversationDTO>();


            // ============================================================
            // GAMIFICATION
            // ============================================================
            CreateMap<UserQuestProgress, QuestDTO>()
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Quest.Title))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.Quest.Description))
                .ForMember(d => d.RewardXP, o => o.MapFrom(s => s.Quest.RewardXP))
                .ForMember(d => d.RewardPoints, o => o.MapFrom(s => s.Quest.RewardPoints))
                .ForMember(d => d.TargetValue, o => o.MapFrom(s => s.Quest.TargetValue))
                .ForMember(d => d.Period, o => o.MapFrom(s => s.Quest.Period.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s =>
                    s.IsClaimed ? "Claimed" :
                    s.IsCompleted ? "Completed" :
                    "InProgress"));
        }
    }
}
