using Microsoft.EntityFrameworkCore;
using DataAccessLayer;
using DataAccessLayer.Dbcontext;
using BusinessAccessLayer.Mapping;
using BusinessAccessLayer.Services.Interfaces;
using BusinessAccessLayer.Services;
using DataAccessLayer.UnitOfWork.Interfaces;
using DataAccessLayer.UnitOfWork;
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
            builder.Services.AddScoped<IMenuService, MenuService>();
            builder.Services.AddScoped<IComboService, ComboService>();


            //UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();




            // Bật middleware Swagger

            app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    c.RoutePrefix = string.Empty; // để Swagger UI ở trang gốc: https://localhost:5001/
                });


            // Configure the HTTP request pipeline.



            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
