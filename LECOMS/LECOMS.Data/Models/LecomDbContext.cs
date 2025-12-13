using LECOMS.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace LECOMS.Data.Models
{
    /// <summary>
    /// Database Context cho LECOMS E-Commerce Platform
    /// Updated: 2025-11-11 - Marketplace Payment Model
    /// </summary>
    public class LecomDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public LecomDbContext(DbContextOptions<LecomDbContext> options) : base(options) { }

        // =====================================================================
        // ==== EXISTING DbSets ====
        // =====================================================================
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
        public DbSet<CourseProduct> CourseProducts => Set<CourseProduct>();
        public DbSet<CourseSection> CourseSections => Set<CourseSection>();
        public DbSet<EarnRule> EarnRules => Set<EarnRule>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Leaderboard> Leaderboards => Set<Leaderboard>();
        public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
        public DbSet<PointWallet> PointWallets => Set<PointWallet>();
        public DbSet<PointLedger> PointLedgers => Set<PointLedger>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<RankTier> RankTiers => Set<RankTier>();
        public DbSet<RedeemRule> RedeemRules => Set<RedeemRule>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
        public DbSet<UserBadge> UserBadges => Set<UserBadge>();
        public DbSet<UserTierHistory> UserTierHistories => Set<UserTierHistory>();
        public DbSet<UserVoucher> UserVouchers => Set<UserVoucher>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Shop> Shops { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<LessonProduct> LessonProducts { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<TransactionOrder> TransactionOrders { get; set; }
        public DbSet<TransactionOrderBreakdown> TransactionOrderBreakdowns { get; set; }
        public DbSet<PlatformWallet> PlatformWallets { get; set; }
        public DbSet<PlatformWalletTransaction> PlatformWalletTransactions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<FeedbackImage> FeedbackImages { get; set; }
        public DbSet<FeedbackReply> FeedbackReplies { get; set; }

        // =====================================================================
        // ==== NEW PAYMENT SYSTEM DbSets ⭐ ====
        // =====================================================================

        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<ShopWallet> ShopWallets => Set<ShopWallet>();
        public DbSet<CustomerWallet> CustomerWallets => Set<CustomerWallet>();
        public DbSet<Entities.WalletTransaction> ShopWalletTransactions => Set<Entities.WalletTransaction>();
        public DbSet<CustomerWalletTransaction> CustomerWalletTransactions => Set<CustomerWalletTransaction>();
        public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
        public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
        public DbSet<CustomerWithdrawalRequest> CustomerWithdrawalRequests => Set<CustomerWithdrawalRequest>();
        public DbSet<PlatformConfig> PlatformConfigs => Set<PlatformConfig>();
        public DbSet<QuestDefinition> QuestDefinitions { get; set; }
        public DbSet<UserQuestProgress> UserQuestProgresses { get; set; }

        public DbSet<Booster> Boosters { get; set; }
        public DbSet<UserBooster> UserBoosters { get; set; }
        public DbSet<UserLessonProgress> UserLessonProgresses { get; set; }
        public DbSet<AchievementDefinition> AchievementDefinitions { get; set; }
        public DbSet<UserAchievementProgress> UserAchievementProgresses { get; set; }
        public DbSet<ShopAddress> ShopAddresses { get; set; }

        // =====================================================================
        // ==== MODEL CONFIGURATION ====
        // =====================================================================
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // =====================================================================
            // 0) CHẶN multiple cascade paths: mặc định Restrict cho mọi FK
            // =====================================================================
            foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // =====================================================================
            // 1) Identity & User indexes
            // =====================================================================
            b.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique(false);
                e.HasIndex(x => x.UserName).IsUnique(true);
            });

            // =====================================================================
            // 2) Product / Category
            // =====================================================================
            b.Entity<ProductCategory>().HasIndex(x => x.Slug).IsUnique();

            b.Entity<Product>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasOne(x => x.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(x => x.CategoryId);
            });

            // =====================================================================
            // 3) Course domain
            // =====================================================================
            b.Entity<CourseCategory>().HasIndex(x => x.Slug).IsUnique();

            b.Entity<Course>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.HasOne(x => x.Category)
                 .WithMany(c => c.Courses)
                 .HasForeignKey(x => x.CategoryId);
            });

            b.Entity<CourseSection>()
                .HasOne(s => s.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Lesson>()
                .HasOne(l => l.Section)
                .WithMany(s => s.Lessons)
                .HasForeignKey(l => l.CourseSectionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<CourseProduct>()
                .HasIndex(x => new { x.CourseId, x.ProductId })
                .IsUnique();

            b.Entity<Enrollment>()
                .HasIndex(x => new { x.UserId, x.CourseId })
                .IsUnique();

            b.Entity<Certificate>()
                .HasIndex(x => new { x.UserId, x.CourseId })
                .IsUnique();

            // =====================================================================
            // 4) Cart / Order / Payment / Shipment
            // =====================================================================
            b.Entity<CartItem>()
                .HasIndex(x => new { x.CartId, x.ProductId })
                .IsUnique();

            b.Entity<OrderDetail>()
                .HasIndex(x => new { x.OrderId, x.ProductId })
                .IsUnique();

            b.Entity<Order>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.CreatedAt });
                e.HasIndex(x => new { x.ShopId, x.CreatedAt });
                e.HasIndex(x => x.OrderCode).IsUnique();
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.PaymentStatus);

                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.PaymentStatus).HasConversion<int>();

                e.HasOne(o => o.Shop)
                 .WithMany(s => s.Orders)
                 .HasForeignKey(o => o.ShopId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<OrderDetail>()
                .HasOne(d => d.Order)
                .WithMany(o => o.Details)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Payment>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();
                e.HasOne(x => x.Order)
                 .WithMany(o => o.Payments)
                 .HasForeignKey(x => x.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<PaymentAttempt>()
                .Property(x => x.Result).HasConversion<int>();

            b.Entity<Shipment>()
                .HasOne(s => s.Order)
                .WithMany(o => o.Shipments)
                .HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<ShipmentItem>()
                .HasOne(i => i.Shipment)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================================
            // 5) Review
            // =====================================================================
            b.Entity<Review>()
                .HasIndex(x => new { x.UserId, x.ProductId })
                .IsUnique();

            // =====================================================================
            // 6) Voucher / UserVoucher
            // =====================================================================
            b.Entity<Voucher>().HasIndex(x => x.Code).IsUnique();

            b.Entity<UserVoucher>()
                .HasIndex(x => new { x.UserId, x.VoucherId })
                .IsUnique();

            // =====================================================================
            // 7) Gamification / Loyalty
            // =====================================================================
            b.Entity<UserBadge>()
                .HasIndex(x => new { x.UserId, x.BadgeId })
                .IsUnique();

            b.Entity<PointLedger>()
                .HasIndex(x => new { x.PointWalletId, x.CreatedAt });

            b.Entity<Leaderboard>().HasIndex(x => x.Code).IsUnique();

            b.Entity<LeaderboardEntry>()
                .HasIndex(x => new { x.LeaderboardId, x.UserId })
                .IsUnique();

            b.Entity<Shipment>().Property(x => x.Status).HasConversion<int>();
            b.Entity<PointLedger>().Property(x => x.Type).HasConversion<int>();

            b.Entity<UserTierHistory>()
                .HasOne(h => h.Tier)
                .WithMany()
                .HasForeignKey(h => h.TierID);

            // =====================================================================
            // 8) Community & Notification
            // =====================================================================
            b.Entity<CommunityPost>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.CommunityPostId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Notification>()
                .HasIndex(x => new { x.UserId, x.IsRead });

            // =====================================================================
            // 10) Shop & Product
            // =====================================================================
            b.Entity<User>()
                .HasOne(u => u.Shop)
                .WithOne(s => s.Seller)
                .HasForeignKey<Shop>(s => s.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<LessonProduct>()
                .HasIndex(lp => new { lp.LessonId, lp.ProductId })
                .IsUnique();

            // =====================================================================
            // ⭐ 11) NEW PAYMENT SYSTEM CONFIGURATION - MARKETPLACE MODEL
            // =====================================================================

            // --- Transaction ---
            b.Entity<Transaction>(e =>
            {
                e.ToTable("Transactions");
                e.HasIndex(x => x.PayOSTransactionId).IsUnique();
                e.HasIndex(x => x.PayOSOrderCode).IsUnique();
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.CreatedAt);
                e.Property(x => x.Status).HasConversion<int>();

            });

            b.Entity<TransactionOrderBreakdown>(e =>
            {
                e.HasOne(b => b.TransactionOrder)
                 .WithMany()
                 .HasForeignKey(b => b.TransactionOrderId)
                 .OnDelete(DeleteBehavior.Cascade);
            });


            // --- ShopWallet ---
            b.Entity<ShopWallet>(e =>
            {
                e.ToTable("ShopWallets");
                e.HasIndex(x => x.ShopId).IsUnique();

                e.HasOne(w => w.Shop)
                 .WithOne()
                 .HasForeignKey<ShopWallet>(w => w.ShopId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- CustomerWallet ---
            b.Entity<CustomerWallet>(e =>
            {
                e.ToTable("CustomerWallets");
                e.HasIndex(x => x.CustomerId).IsUnique();

                e.HasOne(w => w.Customer)
                 .WithOne()
                 .HasForeignKey<CustomerWallet>(w => w.CustomerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- ShopWalletTransaction ---
            b.Entity<Entities.WalletTransaction>(e =>
            {
                e.ToTable("ShopWalletTransactions");
                e.HasIndex(x => new { x.ShopWalletId, x.CreatedAt });
                e.HasIndex(x => x.Type);
                e.HasIndex(x => x.ReferenceId);

                e.Property(x => x.Type).HasConversion<int>();

                e.HasOne(t => t.ShopWallet)
                 .WithMany(w => w.Transactions)
                 .HasForeignKey(t => t.ShopWalletId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- CustomerWalletTransaction ---
            b.Entity<CustomerWalletTransaction>(e =>
            {
                e.ToTable("CustomerWalletTransactions");
                e.HasIndex(x => new { x.CustomerWalletId, x.CreatedAt });
                e.HasIndex(x => x.Type);
                e.HasIndex(x => x.ReferenceId);

                e.Property(x => x.Type).HasConversion<int>();

                e.HasOne(t => t.CustomerWallet)
                 .WithMany(w => w.Transactions)
                 .HasForeignKey(t => t.CustomerWalletId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- RefundRequest ---
            // --- RefundRequest ---
            b.Entity<RefundRequest>(e =>
            {
                e.ToTable("RefundRequests");
                e.HasIndex(x => x.OrderId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.RequestedBy);
                e.HasIndex(x => x.RequestedAt);

                e.Property(x => x.ReasonType).HasConversion<int>();
                e.Property(x => x.Type).HasConversion<int>();
                e.Property(x => x.Status).HasConversion<int>();

                e.HasOne(r => r.Order)
                 .WithMany(o => o.RefundRequests)
                 .HasForeignKey(r => r.OrderId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.RequestedByUser)
                 .WithMany()
                 .HasForeignKey(r => r.RequestedBy)
                 .OnDelete(DeleteBehavior.Restrict);

                // FIX: Use ShopResponseByUser instead of ProcessedByUser
                e.HasOne(r => r.ShopResponseByUser)
                 .WithMany()
                 .HasForeignKey(r => r.ShopResponseBy)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- WithdrawalRequest (Shop) ---
            b.Entity<WithdrawalRequest>(e =>
            {
                e.ToTable("WithdrawalRequests");
                e.HasIndex(x => x.ShopWalletId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.RequestedAt);

                e.Property(x => x.Status).HasConversion<int>();

                e.HasOne(w => w.ShopWallet)
                 .WithMany(sw => sw.WithdrawalRequests)
                 .HasForeignKey(w => w.ShopWalletId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(w => w.Shop)
                 .WithMany()
                 .HasForeignKey(w => w.ShopId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(w => w.ApprovedByUser)
                 .WithMany()
                 .HasForeignKey(w => w.ApprovedBy)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- CustomerWithdrawalRequest ---
            b.Entity<CustomerWithdrawalRequest>(e =>
            {
                e.ToTable("CustomerWithdrawalRequests");
                e.HasIndex(x => x.CustomerWalletId);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.RequestedAt);

                e.Property(x => x.Status).HasConversion<int>();

                e.HasOne(w => w.CustomerWallet)
                 .WithMany(cw => cw.WithdrawalRequests)
                 .HasForeignKey(w => w.CustomerWalletId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(w => w.Customer)
                 .WithMany()
                 .HasForeignKey(w => w.CustomerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(w => w.ApprovedByUser)
                 .WithMany()
                 .HasForeignKey(w => w.ApprovedBy)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // --- PlatformConfig ---
            b.Entity<PlatformConfig>(e =>
            {
                e.ToTable("PlatformConfigs");

                // Seed data
                e.HasData(new PlatformConfig
                {
                    Id = "PLATFORM_CONFIG_SINGLETON",
                    DefaultCommissionRate = 5.00m,
                    OrderHoldingDays = 7,
                    MinWithdrawalAmount = 100000m,
                    MaxWithdrawalAmount = 50000000m,
                    AutoApproveWithdrawal = false,
                    MaxRefundDays = 30,
                    AutoApproveRefund = false,
                    PayOSEnvironment = "sandbox",
                    PayOSClientId = null,
                    PayOSApiKey = null,
                    PayOSChecksumKey = null,
                    EnableEmailNotification = true,
                    EnableSMSNotification = false,
                    LastUpdated = new DateTime(2025, 11, 11, 4, 5, 30, DateTimeKind.Utc),
                    LastUpdatedBy = "haupdse170479"
                });
            });

            // =====================================================================
            // 12) Decimal precision
            // =====================================================================
            foreach (var prop in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                if (prop.GetPrecision() == null && prop.GetScale() == null)
                {
                    prop.SetPrecision(18);
                    prop.SetScale(2);
                }
            }
            // =====================================================================
            // ⭐ CHAT SYSTEM CONFIGURATION — REMOVE FK Sender → User
            // =====================================================================
            b.Entity<Message>(e =>
            {
                e.Ignore(m => m.Sender); // QUAN TRỌNG
                e.HasOne(m => m.Conversation)
                 .WithMany(c => c.Messages)
                 .HasForeignKey(m => m.ConversationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================================
            // --- PlatformWallet ---
            // =====================================================================

            b.Entity<PlatformWallet>(e =>
            {
                e.ToTable("PlatformWallets");
                e.HasKey(x => x.Id);

                e.Property(x => x.Balance).HasPrecision(18, 2);
                e.Property(x => x.TotalCommissionEarned).HasPrecision(18, 2);
                e.Property(x => x.TotalCommissionRefunded).HasPrecision(18, 2);
                e.Property(x => x.TotalPayout).HasPrecision(18, 2);
            });

            // =====================================================================
            // --- PlatformWalletTransaction ---
            // =====================================================================

            b.Entity<PlatformWalletTransaction>(e =>
            {
                e.ToTable("PlatformWalletTransactions");
                e.HasKey(x => x.Id);

                e.Property(x => x.Amount).HasPrecision(18, 2);
                e.Property(x => x.BalanceBefore).HasPrecision(18, 2);
                e.Property(x => x.BalanceAfter).HasPrecision(18, 2);

                e.Property(x => x.Type).HasConversion<int>();

                e.HasIndex(x => x.CreatedAt);
                e.HasIndex(x => new { x.ReferenceId, x.ReferenceType });

                e.HasOne(x => x.PlatformWallet)
                 .WithMany(w => w.Transactions)
                 .HasForeignKey(x => x.PlatformWalletId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================================
            // ⭐ ShopAddress Configuration
            // =====================================================================
            b.Entity<ShopAddress>(e =>
            {
                e.ToTable("ShopAddresses");
                e.HasKey(x => x.Id);

                e.HasIndex(x => x.ShopId);
                e.HasIndex(x => new { x.ShopId, x.IsDefault });

                e.HasOne(sa => sa.Shop)
                 .WithMany()
                 .HasForeignKey(sa => sa.ShopId)
                 .OnDelete(DeleteBehavior.Cascade);  // Xóa shop → xóa address
            });

        }
    }
}