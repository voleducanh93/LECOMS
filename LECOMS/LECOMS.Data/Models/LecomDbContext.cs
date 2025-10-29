using LECOMS.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Reflection.Emit;

namespace LECOMS.Data.Models
{
    public class LecomDbContext : IdentityDbContext<User, IdentityRole, string>

    {
        public LecomDbContext(DbContextOptions<LecomDbContext> options) : base(options) { }

        // ==== DbSet ====
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
        public DbSet<Refund> Refunds => Set<Refund>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Shipment> Shipments => Set<Shipment>();
        public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
        public DbSet<UserBadge> UserBadges => Set<UserBadge>();
        public DbSet<UserTierHistory> UserTierHistories => Set<UserTierHistory>();
        public DbSet<UserVoucher> UserVouchers => Set<UserVoucher>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
        public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
        public DbSet<Shop> Shops { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
public DbSet<LessonProduct> LessonProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // =====================================================================
            // 0) CHẶN multiple cascade paths: mặc định Restrict cho mọi FK,
            //    rồi bật Cascade có chọn lọc cho các aggregate nội bộ.
            // =====================================================================
            foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict; // tương đương NO ACTION
            }

            // =====================================================================
            // 1) Identity & User indexes
            // =====================================================================
            b.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique(false); // Email có thể null
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
                // DeleteBehavior đã là Restrict từ bước 0
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

            // BẬT CASCADE cho aggregate Course -> Section -> Lesson
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

            // Link Course-Product: unique
            b.Entity<CourseProduct>()
                .HasIndex(x => new { x.CourseId, x.ProductId })
                .IsUnique();

            // Enrollment: 1 user chỉ 1 enrollment / course
            b.Entity<Enrollment>()
                .HasIndex(x => new { x.UserId, x.CourseId })
                .IsUnique();

            // Certificate: 1 user/course 1 certificate
            b.Entity<Certificate>()
                .HasIndex(x => new { x.UserId, x.CourseId })
                .IsUnique();

            // User quan hệ xuống Enrollment/Certificate: giữ Restrict (đã từ bước 0)

            // =====================================================================
            // 4) Cart / Order / Payment / Shipment
            // =====================================================================
            b.Entity<CartItem>()
                .HasIndex(x => new { x.CartId, x.ProductId })
                .IsUnique();

            // BẬT CASCADE cho Order aggregate
            b.Entity<OrderDetail>()
                .HasIndex(x => new { x.OrderId, x.ProductId })
                .IsUnique();

            b.Entity<Order>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.CreatedAt });
                e.Property(x => x.Status).HasConversion<int>();
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

            // Enum conversions
            b.Entity<Shipment>().Property(x => x.Status).HasConversion<int>();
            b.Entity<PointLedger>().Property(x => x.Type).HasConversion<int>();
            b.Entity<WalletTransaction>().Property(x => x.Type).HasConversion<int>();

            // Rank / Tier history
            b.Entity<UserTierHistory>()
                .HasOne(h => h.Tier)
                .WithMany()
                .HasForeignKey(h => h.TierID);

            // =====================================================================
            // 8) Community & Notification (FIX multiple cascade paths)
            // =====================================================================
            // Không cascade từ User -> Post để tránh đường trùng tới Comment
            b.Entity<CommunityPost>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Xóa Post sẽ xóa Comment cùng Post (cascade NỘI BỘ)
            b.Entity<Comment>()
             .HasOne(c => c.Post)
             .WithMany(p => p.Comments)
             .HasForeignKey(c => c.CommunityPostId)
             .OnDelete(DeleteBehavior.Restrict);   // đổi từ Cascade -> Restrict


            // Không cascade từ User -> Comment
            b.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Notification>()
                .HasIndex(x => new { x.UserId, x.IsRead });

            // =====================================================================
            // 9) Wallet
            // =====================================================================
            // Không cascade từ User -> WalletAccount (tránh lan truyền lớn)
            b.Entity<WalletAccount>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Giao dịch thuộc về ví: cascade trong aggregate
            b.Entity<WalletTransaction>()
                .HasOne(t => t.Wallet)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WalletAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================================
            // 10) Decimal precision tiền tệ (áp cho mọi decimal chưa set)
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
            // Thêm cấu hình mối quan hệ 1-1 giữa User và Shop
            b.Entity<User>()
             .HasOne(u => u.Shop)
             .WithOne(s => s.Seller)
             .HasForeignKey<Shop>(s => s.SellerId)
             .OnDelete(DeleteBehavior.Cascade);
            // Cấu hình mối quan hệ giữa Product và ProductImage
            b.Entity<ProductImage>()
             .HasOne(pi => pi.Product)
             .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            b.Entity<LessonProduct>()
                .HasIndex(lp => new { lp.LessonId, lp.ProductId })
                .IsUnique();
        }
    }
}
