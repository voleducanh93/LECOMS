using LECOMS.API.Hubs;
using LECOMS.Common.Helper;
using LECOMS.Common.Hubs;
using LECOMS.Data.Entities;
using LECOMS.Service;
using LECOMS.Service.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Recombee.ApiClient;
using Recombee.ApiClient.Util;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add Swagger with Authentication support
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

// Add EmailSettings
builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("EmailSettings"));

// Add AddServices
builder.Services.AddServices(builder.Configuration);
builder.Services.Configure<RecombeeSettings>(builder.Configuration.GetSection("Recombee"));

// ===============================
// 🧠 Recommbee Client Integration
// ===============================
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RecombeeSettings>>().Value;
    return new RecombeeClient(settings.DatabaseId, settings.PrivateToken, region: Region.ApSe);
});

builder.Services.AddQuartz(q =>
{
    var recombeeJobKey = new JobKey("RecombeeSyncJob");
    q.AddJob<RecombeeSyncJob>(opts => opts.WithIdentity(recombeeJobKey));

    // Chạy mỗi ngày 3h sáng
    q.AddTrigger(opts => opts
        .ForJob(recombeeJobKey)
        .WithIdentity("RecombeeSyncJob-trigger")
        .WithCronSchedule("0 0 3 * * ?"));
    // ===============================
    // RECOMBEE SYNC COURSES JOB
    // ===============================
    var recombeeCourseJobKey = new JobKey("RecombeeSyncCoursesJob");
    q.AddJob<RecombeeSyncCoursesJob>(opts => opts.WithIdentity(recombeeCourseJobKey));

    q.AddTrigger(opts => opts
        .ForJob(recombeeCourseJobKey)
        .WithIdentity("RecombeeSyncCoursesJob-trigger")
        .WithCronSchedule("0 5 3 * * ?")    // chạy 3:05 sáng hằng ngày
    );
    // ===========================
    // VOUCHER EXPIRE JOB
    // ===========================
    var voucherExpireJobKey = new JobKey("VoucherExpireJob");
    q.AddJob<VoucherExpireJob>(o => o.WithIdentity(voucherExpireJobKey));

    q.AddTrigger(t => t
        .ForJob(voucherExpireJobKey)
        .WithIdentity("VoucherExpireJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInHours(1) // chạy mỗi 1 giờ
            .RepeatForever()
        )
    );

    // ===============================
    // USER VOUCHER EXPIRE JOB
    // ===============================
    var uvJobKey = new JobKey("UserVoucherExpireJob");
    q.AddJob<UserVoucherExpireJob>(o => o.WithIdentity(uvJobKey));

    q.AddTrigger(t => t
        .ForJob(uvJobKey)
        .WithIdentity("UserVoucherExpireJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInHours(1)
            .RepeatForever()
        )
    );

    // ==========================
    // AUTO RELEASE BALANCE JOB
    // ==========================
    var releaseJobKey = new JobKey("AutoReleaseBalanceJob");
    q.AddJob<AutoReleaseBalanceJob>(opts => opts.WithIdentity(releaseJobKey));

    q.AddTrigger(t => t
        .ForJob(releaseJobKey)
        .WithIdentity("AutoReleaseBalanceJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInMinutes(30) // Chạy mỗi 30 phút
            .RepeatForever()
        )
    );

    // ==========================
    // AUTO ESCALATE REFUND JOB
    // ==========================
    var escalateRefundKey = new JobKey("AutoEscalateRefundJob");
    q.AddJob<AutoEscalateRefundJob>(opts => opts.WithIdentity(escalateRefundKey));

    q.AddTrigger(t => t
        .ForJob(escalateRefundKey)
        .WithIdentity("AutoEscalateRefundJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInHours(1)  // chạy mỗi 1 giờ
            .RepeatForever()
        )
    );


});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddSignalR();

// Add UserManager and SignInManager for dependency injection
builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<SignInManager<User>>();

// Lưu DataProtection keys vào volume /keys
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .SetApplicationName("LECOMS");

// Add JWT Configuration
var jwtSecret = builder.Configuration["JWT:Key"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new ArgumentNullException(nameof(jwtSecret), "JWT Secret cannot be null or empty.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };

});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
});
// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

    });
});

// Đăng ký cấu hình Cloudinary để có thể inject IOptions<CloudinarySettings>
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

//child
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LECOMS.Data.Models.LecomDbContext>();
    db.Database.Migrate();
}

// Create default roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    // Define default roles
    var roles = new[] { "Admin", "Seller", "Customer", "Moderator" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Create default admin user
    var adminEmail = "admin@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var newUser = new User
        {
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newUser, "Admin12345@");
        if (result.Succeeded)
        {
            adminUser = await userManager.FindByEmailAsync(adminEmail);

            await userManager.AddToRoleAsync(newUser, "Admin");
        }
    }
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseWebSockets();
app.UseSwaggerUI();
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

app.UseCors("AllowFrontend"); // Enable CORS Policy

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok("Healthy"))
    .AllowAnonymous();

app.Run();
