using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LecomDbContext _context;
        public IUserRepository Users { get; }
        public IShopRepository Shops { get; }
        public ICourseRepository Courses { get; }
        public ICourseSectionRepository Sections { get; }
        public ILessonRepository Lessons { get; }
        public ICourseProductRepository CourseProducts { get; }
        public ICourseCategoryRepository CourseCategories { get; }
        public IProductCategoryRepository ProductCategories { get; }
        public IProductRepository Products { get; }
        public IEnrollmentRepository Enrollments { get; }

        public IProductImageRepository ProductImages { get; }
        public ILessonProductRepository LessonProducts { get; }
        public ILandingPageRepository LandingPage { get; }
        public ICartRepository Carts { get; }
        public ICartItemRepository CartItems { get; }
        public IOrderRepository Orders { get; }
        public IOrderDetailRepository OrderDetails { get; }
        public IPaymentRepository Payments { get; }

        public ITransactionRepository Transactions { get; }
        public IShopWalletRepository ShopWallets { get; }
        public ICustomerWalletRepository CustomerWallets { get; }
        public IWalletTransactionRepository WalletTransactions { get; }
        public ICustomerWalletTransactionRepository CustomerWalletTransactions { get; }
        public IRefundRequestRepository RefundRequests { get; }
        public IWithdrawalRequestRepository WithdrawalRequests { get; }
        public ICustomerWithdrawalRequestRepository CustomerWithdrawalRequests { get; }
        public IPlatformConfigRepository PlatformConfigs { get; }
        public IConversationRepository Conversations { get; }
        public IMessageRepository Messages { get; }
        public IQuestDefinitionRepository QuestDefinitions { get; }
        public IUserQuestProgressRepository UserQuestProgresses { get; }

        public IEarnRuleRepository EarnRules { get; }
        public IRedeemRuleRepository RedeemRules { get; }
        public ILeaderboardRepository Leaderboards { get; }
        public ILeaderboardEntryRepository LeaderboardEntries { get; }
        public IVoucherRepository Vouchers { get; }
        public IUserVoucherRepository UserVouchers { get; }
        public IRankTierRepository RankTiers { get; }
        public IBoosterRepository Boosters { get; }
        public IUserBoosterRepository UserBoosters { get; }
        public IPointWalletRepository PointWallets { get; }
        public IPointLedgerRepository PointLedgers { get; }
        public IUserLessonProgressRepository UserLessonProgresses { get; }
        public ICommunityPostRepository CommunityPosts { get; }
        public ICommentRepository Comments { get; }
        public ITransactionOrderRepository TransactionOrders { get; }
        public ITransactionOrderBreakdownRepository TransactionOrderBreakdowns { get; }
        public IPlatformWalletRepository PlatformWallets { get; }
        public IPlatformWalletTransactionRepository PlatformWalletTransactions { get; }
        public IFeedbackRepository Feedbacks { get; }
        public IFeedbackReplyRepository FeedbackReplies { get; }
        public IFeedbackImageRepository FeedbackImages { get; }
        public IRepository<Badge> Badges { get;  }
        public IRepository<UserBadge> UserBadges { get; }
        public IRepository<AchievementDefinition> AchievementDefinitions { get; }
        public IRepository<UserAchievementProgress> UserAchievementProgresses { get; }
        public IShopAddressRepository ShopAddresses { get; }


        public INotificationRepository Notifications { get; }


        public UnitOfWork(LecomDbContext context, IUserRepository userRepository, IShopRepository shopRepository, ICourseRepository courseRepo, ICourseSectionRepository sectionRepo,
        ILessonRepository lessonRepo, ICourseProductRepository cpRepo, ICourseCategoryRepository courseCategories, IProductCategoryRepository productCategories, IProductRepository products, IEnrollmentRepository enrollmentRepository, IProductImageRepository productImageRepository, ILessonProductRepository lessonProductRepository, ILandingPageRepository landingPage, ICartRepository cartRepository, ICartItemRepository cartItemRepository, IOrderRepository orderRepository, IOrderDetailRepository orderDetailRepository, IPaymentRepository paymentRepository, ITransactionRepository transactionRepository, IShopWalletRepository shopWalletRepository, ICustomerWalletRepository customerWalletRepository, IWalletTransactionRepository walletTransactionRepository, ICustomerWalletTransactionRepository customerWalletTransactionRepository, IRefundRequestRepository refundRequestRepository, IWithdrawalRequestRepository withdrawalRequestRepository, ICustomerWithdrawalRequestRepository customerWithdrawalRequestRepository, IPlatformConfigRepository platformConfigRepository
            ,IConversationRepository conversationRepository, IMessageRepository messageRepository,


    // ⭐ thêm mấy thằng gamification ở đây
    IPointWalletRepository pointWalletRepository,
    IPointLedgerRepository pointLedgerRepository,
    IQuestDefinitionRepository questDefinitionRepository,
    IUserQuestProgressRepository userQuestProgressRepository,
    IEarnRuleRepository earnRuleRepository,
    IRedeemRuleRepository redeemRuleRepository,
    ILeaderboardRepository leaderboardRepository,
    ILeaderboardEntryRepository leaderboardEntryRepository,
    IVoucherRepository voucherRepository,
    IUserVoucherRepository userVoucherRepository,
    IRankTierRepository rankTierRepository,
    IBoosterRepository boosterRepository,
    IUserBoosterRepository userBoosterRepository, IUserLessonProgressRepository userLessonProgressRepository,
    ICommunityPostRepository communityPostRepository, ICommentRepository commentRepository, ITransactionOrderRepository transactionOrderRepository, 
    ITransactionOrderBreakdownRepository transactionOrderBreakdownRepository, IPlatformWalletRepository platformWalletRepository, 
    IPlatformWalletTransactionRepository platformWalletTransactionRepository, IFeedbackRepository feedbackRepository, 
    IFeedbackReplyRepository feedbackReplyRepository, IFeedbackImageRepository feedbackImageRepository, INotificationRepository notificationRepository, IShopAddressRepository shopAddressRepository)

        {
            _context = context;
            Users = userRepository;
            Shops = shopRepository;

            Courses = courseRepo;
            Sections = sectionRepo;
            Lessons = lessonRepo;
            CourseProducts = cpRepo;
            CourseCategories = courseCategories;
            ProductCategories = productCategories;
            Products = products;
            Enrollments = enrollmentRepository;
            ProductImages = productImageRepository;
            LessonProducts = lessonProductRepository;
            LandingPage = landingPage;
            Carts = cartRepository;
            CartItems = cartItemRepository;
            Orders = orderRepository;
            OrderDetails = orderDetailRepository;
            Payments = paymentRepository;
            Transactions = transactionRepository;
            ShopWallets = shopWalletRepository;
            CustomerWallets = customerWalletRepository;
            WalletTransactions = walletTransactionRepository;
            CustomerWalletTransactions = customerWalletTransactionRepository;
            RefundRequests = refundRequestRepository;
            WithdrawalRequests = withdrawalRequestRepository;
            CustomerWithdrawalRequests = customerWithdrawalRequestRepository;
            PlatformConfigs = platformConfigRepository;
            Conversations = conversationRepository;
            Messages = messageRepository;
            PointWallets = pointWalletRepository;
            PointLedgers = pointLedgerRepository;
            QuestDefinitions = questDefinitionRepository;
            UserQuestProgresses = userQuestProgressRepository;
            EarnRules = earnRuleRepository;
            RedeemRules = redeemRuleRepository;
            Leaderboards = leaderboardRepository;
            LeaderboardEntries = leaderboardEntryRepository;
            Vouchers = voucherRepository;
            UserVouchers = userVoucherRepository;
            RankTiers = rankTierRepository;
            Boosters = boosterRepository;
            UserBoosters = userBoosterRepository;
            UserLessonProgresses = userLessonProgressRepository;
            CommunityPosts = communityPostRepository;
            Comments = commentRepository;
            TransactionOrders = transactionOrderRepository;
            TransactionOrderBreakdowns = transactionOrderBreakdownRepository;
            PlatformWallets = platformWalletRepository;
            PlatformWalletTransactions = platformWalletTransactionRepository;
            Feedbacks = feedbackRepository;
            FeedbackReplies = feedbackReplyRepository;
            FeedbackImages = feedbackImageRepository;
            Badges = new Repository<Badge>(context);
            UserBadges = new Repository<UserBadge>(context);
            AchievementDefinitions = new Repository<AchievementDefinition>(context);
            UserAchievementProgresses = new Repository<UserAchievementProgress>(context);

            Notifications = notificationRepository;
            ShopAddresses = shopAddressRepository;

        }
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            // Nếu đã có transaction hiện tại thì trả về luôn
            if (_context.Database.CurrentTransaction != null)
            {
                return _context.Database.CurrentTransaction;
            }

            // Chưa có thì mới tạo transaction mới
            return await _context.Database.BeginTransactionAsync();
        }

        public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;


        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
