using LECOMS.Repository.Repositories;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository
{
    public static class DependencyInjcection
    {
        public static IServiceCollection AddRepository(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureDatabase(configuration);
            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<IEmailRepository, EmailRepository>();
            services.AddTransient<IShopRepository, ShopRepository>();
            services.AddTransient<ICourseRepository, CourseRepository>();
            services.AddTransient<ICourseSectionRepository, CourseSectionRepository>();
            services.AddTransient<ILessonRepository, LessonRepository>();
            services.AddTransient<ICourseProductRepository, CourseProductRepository>();
            services.AddTransient<ICourseCategoryRepository, CourseCategoryRepository>();
            services.AddTransient<IProductCategoryRepository, ProductCategoryRepository>();
            services.AddTransient<IProductRepository, ProductRepository>();
            services.AddTransient<IEnrollmentRepository, EnrollmentRepository>();
            services.AddTransient<ILessonProductRepository, LessonProductRepository>();
            services.AddTransient<IProductImageRepository, ProductImageRepository>();
            services.AddTransient<ILandingPageRepository, LandingPageRepository>();
            services.AddTransient<ICartRepository, CartRepository>();
            services.AddTransient<ICartItemRepository, CartItemRepository>();
            services.AddTransient<IOrderRepository, OrderRepository>();
            services.AddTransient<IOrderDetailRepository, OrderDetailRepository>();
            services.AddTransient<IPaymentRepository, PaymentRepository>();
            services.AddTransient<ITransactionRepository, TransactionRepository>();
            services.AddTransient<IShopWalletRepository, ShopWalletRepository>();
            services.AddTransient<ICustomerWalletRepository, CustomerWalletRepository>();
            services.AddTransient<IWalletTransactionRepository, WalletTransactionRepository>();
            services.AddTransient<ICustomerWalletTransactionRepository, CustomerWalletTransactionRepository>();
            services.AddTransient<IRefundRequestRepository, RefundRequestRepository>();
            services.AddTransient<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
            services.AddTransient<ICustomerWithdrawalRequestRepository, CustomerWithdrawalRequestRepository>();
            services.AddTransient<IPlatformConfigRepository, PlatformConfigRepository>();
            services.AddTransient<IConversationRepository, ConversationRepository>();
            services.AddTransient<IMessageRepository, MessageRepository>();
            services.AddScoped<IPointWalletRepository, PointWalletRepository>();
            services.AddScoped<IPointLedgerRepository, PointLedgerRepository>();
            services.AddScoped<IQuestDefinitionRepository, QuestDefinitionRepository>();
            services.AddScoped<IUserQuestProgressRepository, UserQuestProgressRepository>();
            services.AddScoped<IBoosterRepository, BoosterRepository>();
            services.AddScoped<IUserBoosterRepository, UserBoosterRepository>();
            services.AddTransient<IEarnRuleRepository, EarnRuleRepository>();
            services.AddTransient<IRedeemRuleRepository, RedeemRuleRepository>();
            services.AddTransient<ILeaderboardRepository, LeaderboardRepository>();
            services.AddTransient<ILeaderboardEntryRepository, LeaderboardEntryRepository>();
            services.AddTransient<IVoucherRepository, VoucherRepository>();
            services.AddTransient<IUserVoucherRepository, UserVoucherRepository>();
            services.AddTransient<IRankTierRepository, RankTierRepository>();
            services.AddTransient<IUserLessonProgressRepository, UserLessonProgressRepository>();
            services.AddTransient<ICommunityPostRepository, CommunityPostRepository>();
            services.AddTransient<ICommentRepository, CommentRepository>();
            services.AddTransient<ITransactionOrderBreakdownRepository, TransactionOrderBreakdownRepository>();
            services.AddTransient<ITransactionOrderRepository, TransactionOrderRepository>();
            services.AddScoped<IPlatformWalletRepository, PlatformWalletRepository>();
            services.AddScoped<IPlatformWalletTransactionRepository, PlatformWalletTransactionRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<IFeedbackImageRepository, FeedbackImageRepository>();
            services.AddScoped<IFeedbackReplyRepository, FeedbackReplyRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();  
            services.AddScoped<IShopAddressRepository, ShopAddressRepository>();
            //DI Unit Of Work
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
