using BusinessAccessLayer.Mapping;
using BusinessAccessLayer.Services;
using BusinessAccessLayer.Services.Interfaces;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Interfaces;
using DataAccessLayer;
using DataAccessLayer.Dbcontext;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SapaFoRestRMSAPI.Services;
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

            //Voucher
            builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
            builder.Services.AddScoped<IVoucherService, VoucherService>();

            //Payroll
            builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
            builder.Services.AddScoped<IPayrollService, PayrollService>();


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
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
