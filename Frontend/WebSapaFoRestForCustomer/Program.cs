using Microsoft.AspNetCore.Authentication.Cookies;
using WebSapaFoRestForCustomer.Services;

namespace WebSapaFoRestForCustomer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient();
            builder.Services.AddHttpContextAccessor();

            // Register ApiService
            builder.Services.AddScoped<ApiService>();

            // Configure Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    options.SlidingExpiration = true;
                });

            // Configure Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
            });

            // Configure API settings
            builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }

    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:7096";
    }
}
