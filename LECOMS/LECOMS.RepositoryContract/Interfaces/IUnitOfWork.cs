using LECOMS.Data.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.RepositoryContract.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IShopRepository Shops { get; }

        ICourseRepository Courses { get; }          // thêm
        ICourseSectionRepository Sections { get; }  // thêm
        ILessonRepository Lessons { get; }          // thêm
        ICourseProductRepository CourseProducts { get; } // thêm

        ICourseCategoryRepository CourseCategories { get; } // thêm 

        IProductCategoryRepository ProductCategories { get; }
        IProductRepository Products { get; }
        ILessonProductRepository LessonProducts { get; }
        IProductImageRepository ProductImages { get; }
        ILandingPageRepository LandingPage { get; }
        // Enrollment repository exposure
        IEnrollmentRepository Enrollments { get; }
        ICartRepository Carts { get; }
        IOrderRepository Orders { get; }
        IPaymentRepository Payments { get; }
        IOrderDetailRepository OrderDetails { get; }
        ICartItemRepository CartItems { get; }

        ITransactionRepository Transactions { get; }
        IShopWalletRepository ShopWallets { get; }
        ICustomerWalletRepository CustomerWallets { get; }
        IWalletTransactionRepository WalletTransactions { get; }
        ICustomerWalletTransactionRepository CustomerWalletTransactions { get; }
        IRefundRequestRepository RefundRequests { get; }
        IWithdrawalRequestRepository WithdrawalRequests { get; }
        ICustomerWithdrawalRequestRepository CustomerWithdrawalRequests { get; }
        IPlatformConfigRepository PlatformConfigs { get; }
        IMessageRepository Messages { get; }
        IConversationRepository Conversations { get; }
        IQuestDefinitionRepository QuestDefinitions { get; }
        IUserQuestProgressRepository UserQuestProgresses { get; }

        IEarnRuleRepository EarnRules { get; }
        IRedeemRuleRepository RedeemRules { get; }
        ILeaderboardRepository Leaderboards { get; }
        ILeaderboardEntryRepository LeaderboardEntries { get; }
        IVoucherRepository Vouchers { get; }
        IUserVoucherRepository UserVouchers { get; }
        IRankTierRepository RankTiers { get; }
        IBoosterRepository Boosters { get; }
        IUserBoosterRepository UserBoosters { get; }
        IPointWalletRepository PointWallets { get; }
        IPointLedgerRepository PointLedgers { get; }
        IUserLessonProgressRepository UserLessonProgresses { get; }
        ICommunityPostRepository CommunityPosts { get; }
        ICommentRepository Comments { get; }
        ITransactionOrderRepository TransactionOrders { get; }
        ITransactionOrderBreakdownRepository TransactionOrderBreakdowns { get; }
        IPlatformWalletRepository PlatformWallets { get; }
        IPlatformWalletTransactionRepository PlatformWalletTransactions { get; }
        IFeedbackRepository Feedbacks { get; }
        IFeedbackReplyRepository FeedbackReplies { get; }
        IFeedbackImageRepository FeedbackImages { get; }
        IRepository<Badge> Badges { get; }
        IRepository<UserBadge> UserBadges { get; }
        INotificationRepository Notifications { get; }
        // ACHIEVEMENTS (NEW)
        IRepository<AchievementDefinition> AchievementDefinitions { get; }
        IRepository<UserAchievementProgress> UserAchievementProgresses { get; }
        IShopAddressRepository ShopAddresses { get; }
        Task<int> CompleteAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        bool HasActiveTransaction { get; }
       
    }
}
