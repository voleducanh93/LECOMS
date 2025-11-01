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
            //DI Unit Of Work
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
