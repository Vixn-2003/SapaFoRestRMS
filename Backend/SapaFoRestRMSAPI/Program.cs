using BusinessAccessLayer.Mapping;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.UnitOfWork;
using DataAccessLayer.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using SapaFoRestRMSAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using DomainAccessLayer.Enums;
namespace SapaFoRestRMSAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase")));


            // Add services to the container.

            builder.Services.AddControllers();

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

            // Add Repositories
            builder.Services.AddScoped<ISystemLogoRepository, SystemLogoRepository>();

            // Add Services
            builder.Services.AddScoped<ISystemLogoService, SystemLogoService>();

            builder.Services.AddScoped<IBrandBannerRepository, BrandBannerRepository>();
            builder.Services.AddScoped<IBrandBannerService, BrandBannerService>();

            builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
            builder.Services.AddScoped<IMenuItemService, MenuItemService>();

            builder.Services.AddScoped<IComboRepository, ComboRepository>();
            builder.Services.AddScoped<IComboService, ComboService>();

            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<IEventService, EventService>();


            // Unit of Work and User Repository mapping
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Users);

            // Auth and User Management services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserManagementService, UserManagementService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IVerificationService, VerificationService>();
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();


            builder.Services.AddSingleton<CloudinaryService>();

            builder.Services.AddSwaggerGen();
            // Thêm CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins("https://localhost:5158")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(Roles.Admin, p => p.RequireRole(Roles.Admin));
                options.AddPolicy(Roles.Manager, p => p.RequireRole(Roles.Manager));
                options.AddPolicy(Roles.Staff, p => p.RequireRole(Roles.Staff));
                options.AddPolicy(Roles.Customer, p => p.RequireRole(Roles.Customer));
                options.AddPolicy(Roles.Owner, p => p.RequireRole(Roles.Owner));
                options.AddPolicy("AdminOrManager", p => p.RequireRole(Roles.Admin, Roles.Manager));
                options.AddPolicy("StaffOrManager", p => p.RequireRole(Roles.Staff, Roles.Manager));
            });

            // JWT Authentication
            var jwtSection = builder.Configuration.GetSection("Jwt");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "replace-with-strong-key"));
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = signingKey,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Khi chưa đăng nhập mà vào trang yêu cầu auth, hệ thống sẽ redirect về /Auth/Login
        options.LoginPath = "/Auth/Login";

        // Khi logout thì redirect về /Auth/Logout
        options.LogoutPath = "/Auth/Logout";

        // Khi không đủ quyền truy cập (AccessDenied) thì redirect về /Auth/AccessDenied
        options.AccessDeniedPath = "/Auth/AccessDenied";

        // Tên của cookie lưu trữ thông tin đăng nhập
        options.Cookie.Name = "CellPhoneShop.Auth";

        // Cookie chỉ cho server đọc (client JS không đọc được) → tăng bảo mật
        options.Cookie.HttpOnly = true;

        // Thời gian sống của cookie (ở đây là 1 tiếng)
        options.ExpireTimeSpan = TimeSpan.FromHours(1);

        // Nếu người dùng hoạt động trong thời gian hiệu lực → hệ thống tự động kéo dài thêm hạn cookie
        options.SlidingExpiration = true;
    });

           
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseHttpsRedirection();
            // Bật CORS
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Seed admin user on startup
            //using (var scope = app.Services.CreateScope())
            //{
            //    var ctx = scope.ServiceProvider.GetRequiredService<SapaFoRestRmsContext>();
            //    await SapaFoRestRMSAPI.Services.DataSeeder.SeedAdminAsync(ctx);
            //}

            app.Run();
        }
    }
}
