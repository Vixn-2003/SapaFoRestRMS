using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using WebSapaFoRestForCustomer.Services; // <-- Thêm using này

namespace WebSapaFoRestForCustomer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpClient(); // Dòng này đã có
            // Needed for ApiService to access HttpContext.User claims
            builder.Services.AddHttpContextAccessor();

            // === THÊM KHỐI NÀY VÀO (Lấy từ dự án cũ) ===
            // Cấu hình HttpClient tên "API"
            builder.Services.AddHttpClient("API", client =>
            {
                // Lấy BaseUrl từ appsettings.json
                // (ví dụ: "http://192.168.1.47:5180/api")
                var baseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl");
                var rootUrl = baseUrl.Replace("/api", "");

                client.BaseAddress = new Uri(rootUrl);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                // Đây là phần QUAN TRỌNG: Bỏ qua lỗi chứng chỉ SSL
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
            });
            // ===============================================
            // Register ApiService
            builder.Services.AddScoped<ApiService>();
            builder.Services.AddSession();

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

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();
            app.UseSession();

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