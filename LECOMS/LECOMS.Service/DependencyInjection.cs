using LECOMS.Common.Helper;
using LECOMS.Repository;
using LECOMS.Repository.Repositories;
using LECOMS.RepositoryContract.Interfaces;
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
            return services;
        }
    }
}

