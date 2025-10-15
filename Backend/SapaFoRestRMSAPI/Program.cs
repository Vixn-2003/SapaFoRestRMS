
using Microsoft.EntityFrameworkCore;
using DataAccessLayer;
using DataAccessLayer.Dbcontext;
using BusinessAccessLayer.Mapping;
using BusinessAccessLayer.Services.Interfaces;
using BusinessAccessLayer.Services;
using DataAccessLayer.UnitOfWork.Interfaces;
using DataAccessLayer.UnitOfWork;
using DataAccessLayer.Repositories;
using DataAccessLayer.Repositories.Interfaces;
using SapaFoRestRMSAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using DomainAccessLayer.Enums;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase")));

//Show connection string in console
Console.WriteLine(builder.Configuration.GetConnectionString("MyDatabase"));


builder.Services.AddEndpointsApiExplorer();
// Bật middleware Swagger
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

builder.Services.AddScoped<IMarketingCampaignRepository, MarketingCampaignRepository>();
builder.Services.AddScoped<IMarketingCampaignService, MarketingCampaignService>();
builder.Services.AddScoped<ICloudinaryService, BusinessAccessLayer.Services.CloudinaryService>();
//UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

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


builder.Services.AddSingleton<SapaFoRestRMSAPI.Services.CloudinaryService>();



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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

//Seed admin user on startup
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<SapaFoRestRmsContext>();
   await  SapaFoRestRMSAPI.Services.DataSeeder.SeedAdminAsync(ctx);
}

app.Run();
