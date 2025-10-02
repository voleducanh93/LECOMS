using LECOMS.Data.Entities;
using LECOMS.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LECOMS.Repository
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection ConfigureDatabase(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<LecomDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<LecomDbContext>()
                .AddDefaultTokenProviders();

            return services;
        }
    }

}
