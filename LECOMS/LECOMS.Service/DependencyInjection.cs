    using LECOMS.Common.Helper;
    using LECOMS.Repository;
    using LECOMS.Repository.Repositories;
    using LECOMS.RepositoryContract.Interfaces;
    using LECOMS.Service.Jobs;
    using LECOMS.Service.Services;
    using LECOMS.ServiceContract.Interfaces;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace LECOMS.Service
    {
        public static class DependencyInjcection
        {
            public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
            {
                services.AddRepository(configuration);
                services.AddScoped<IEmailService, EmailService>();

                services.AddScoped<APIResponse>();
                services.AddTransient<IUserService, UserService>();
                services.AddTransient<IAuthService, AuthService>();
                services.AddTransient<IShopService, ShopService>();
                services.AddTransient<ISellerCourseService, SellerCourseService>();
                services.AddTransient<ICourseCategoryService, CourseCategoryService>();
                services.AddTransient<IProductCategoryService, ProductCategoryService>();
                services.AddTransient<IProductService, ProductService>();
                services.AddTransient<IEnrollmentService, EnrollmentService>();
                services.AddScoped<IPhotoService, PhotoService>();
                services.AddTransient<ILandingPageService, LandingPageService>();
                services.AddTransient<ICartService, CartService>();
                services.AddTransient<IOrderService, OrderService>();
                services.AddTransient<IPaymentService, PaymentService>();
                services.AddScoped<RecombeeService>(); // Service chính để sync & recommend
                services.AddSingleton<LECOMS.Common.Helper.RecombeeBootstrap>(); // Khởi tạo schema
                services.AddScoped<RecombeeTrackingService>();
                services.AddScoped<IPaymentService, PaymentService>();
                services.AddScoped<IShopWalletService, ShopWalletService>();
                services.AddScoped<ICustomerWalletService, CustomerWalletService>();
                services.AddScoped<IRefundService, RefundService>();
                services.AddScoped<IWithdrawalService, WithdrawalService>();
                services.AddScoped<ICustomerWithdrawalService, CustomerWithdrawalService>();
                services.AddScoped<ICommunityService, CommunityService>();
            // Chat System
            services.AddScoped<IChatService, ChatService>();

            // AI Chat (Gemini)
            services.AddHttpClient<IAIProductChatService, AIProductChatService>();

            // ⭐ Gamification Service
            services.AddTransient<IGamificationService, GamificationService>();

            services.AddTransient<IVoucherService, VoucherService>();
            services.AddTransient<IGamificationAdminService, GamificationAdminService>();
            services.AddTransient<ITransactionOrderService, TransactionOrderService>();
            services.AddTransient<IPlatformWalletService, PlatformWalletService>();
            services.AddTransient<IFeedbackService, FeedbackService>();
            services.AddTransient<ISellerDashboardService, SellerDashboardService>();
            services.AddTransient<IAchievementService, AchievementService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddHttpClient<IShippingService, GHNShippingService>();
            services.AddScoped<IShopAddressService, ShopAddressService>();
            return services;
            }
        }
    }

