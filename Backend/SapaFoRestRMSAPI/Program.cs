using Microsoft.EntityFrameworkCore;
using DataAccessLayer;
using DataAccessLayer.Dbcontext;
using BusinessAccessLayer.Mapping;
using BusinessAccessLayer.Services.Interfaces;
using BusinessAccessLayer.Services;
using DataAccessLayer.UnitOfWork.Interfaces;
using DataAccessLayer.UnitOfWork;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Interfaces;
using SapaFoRestRMSAPI.Services;
using DataAccessLayer.Repositories.Interfaces;
using DataAccessLayer.Repositories;

namespace SapaFoRestRMSAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== DbContext =====
            builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("SapaFoRestRMSContext")));

            // ===== Swagger =====
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                // ✅ BẮT BUỘC: khai báo SwaggerDoc có version hợp lệ
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "SapaFoRestRMS API",
                    Version = "v1",
                    Description = "API documentation for SapaFoRestRMS system"
                });
            });


            // ===== AutoMapper =====
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // ===== UnitOfWork =====
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ===== Repository & Service DI =====
            builder.Services.AddScoped<ISystemLogoRepository, SystemLogoRepository>();
            builder.Services.AddScoped<ISystemLogoService, SystemLogoService>();

            builder.Services.AddScoped<IBrandBannerRepository, BrandBannerRepository>();
            builder.Services.AddScoped<IBrandBannerService, BrandBannerService>();

            builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
            builder.Services.AddScoped<IMenuItemService, MenuItemService>();

            builder.Services.AddScoped<IComboRepository, ComboRepository>();
            builder.Services.AddScoped<IComboService, ComboService>();

            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<IEventService, EventService>();

            builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
            builder.Services.AddScoped<IVoucherService, VoucherService>();

            builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
            builder.Services.AddScoped<IPayrollService, PayrollService>();

            builder.Services.AddScoped<IManagerMenuService, ManagerMenuService>();
            builder.Services.AddScoped<IManagerComboService, ManagerComboService>();

            builder.Services.AddSingleton<CloudinaryService>();

            // ===== Controllers =====
            builder.Services.AddControllers();

            // ===== CORS =====
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("https://localhost:5158")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // ===== Swagger Middleware =====
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SapaFoRestRMS API v1");
                    // ⚠️ KHÔNG đặt RoutePrefix = string.Empty;
                });
            }


            // ===== Pipeline =====
            app.UseHttpsRedirection();
            app.UseCors("AllowFrontend");
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
