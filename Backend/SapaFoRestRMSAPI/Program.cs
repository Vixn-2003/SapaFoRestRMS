
﻿using Microsoft.EntityFrameworkCore;
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
            builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SapaFoRestRMSContext")));


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            // MAPPING
            builder.Services.AddAutoMapper(typeof(MappingProfile));


            //Scope
            builder.Services.AddScoped<IManagerMenuService, ManagerMenuService>();
            builder.Services.AddScoped<IManagerComboService, ManagerComboService>();


            //UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


            // Add services to the container.

            builder.Services.AddControllers();

            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
            builder.Services.AddAutoMapper(typeof(MappingProfile));


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

            builder.Services.AddScoped<IManagerMenuService, ManagerMenuService>();
            builder.Services.AddScoped<IManagerComboService, ManagerComboService>();

            //UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IReservationService, ReservationService>();

            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

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




            // Bật middleware Swagger

            app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    c.RoutePrefix = string.Empty; // để Swagger UI ở trang gốc: https://localhost:5001/
                });


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
