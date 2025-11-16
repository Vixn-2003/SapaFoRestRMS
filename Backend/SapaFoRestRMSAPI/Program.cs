
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
using Microsoft.AspNetCore.Http.Features;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase")));

//Show connection string in console
Console.WriteLine(builder.Configuration.GetConnectionString("MyDatabase"));


builder.Services.AddEndpointsApiExplorer();
// Bật middleware Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CellPhoneShop API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập token theo dạng: Bearer {token}"
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
            new string[] {}
        }
    });
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
builder.Services.AddScoped<IManagerCategoryService, ManagerCategoryService>();
builder.Services.AddScoped<IInventoryIngredientService, InventoryIngredientService>();
builder.Services.AddScoped<IManagerSupplierService, ManagerSupplierService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IStockTransactionService, StockTransactionService>();





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
            //Table Service/Repository
            builder.Services.AddScoped<ITableRepository, TableRepository>();
            builder.Services.AddScoped<ITableService, TableService>();
            // Area Repository
            builder.Services.AddScoped<IAreaRepository, AreaRepository>();
            builder.Services.AddScoped<IAreaService, AreaService>();

builder.Services.AddScoped<IStaffProfileService, StaffProfileService>();

builder.Services.AddSingleton<SapaFoRestRMSAPI.Services.CloudinaryService>();


// ✅ Đảm bảo hỗ trợ multipart form data
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // Nếu dùng Newtonsoft.Json

// ✅ Cấu hình kích thước file upload (nếu cần)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
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

// Upsert Admin from configuration (Development)
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<SapaFoRestRmsContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var adminEmail = config["AdminAccount:Email"];
    var adminPassword = config["AdminAccount:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var adminRoleId = await ctx.Roles.Where(r => r.RoleName == "Admin").Select(r => r.RoleId).FirstOrDefaultAsync();
        if (adminRoleId == 0)
        {
            var adminRole = new DomainAccessLayer.Models.Role { RoleName = "Admin" };
            await ctx.Roles.AddAsync(adminRole);
            await ctx.SaveChangesAsync();
            adminRoleId = adminRole.RoleId;
        }

        string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        var existing = await ctx.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existing == null)
        {
            var admin = new DomainAccessLayer.Models.User
            {
                FullName = "System Admin",
                Email = adminEmail,
                PasswordHash = HashPassword(adminPassword),
                RoleId = adminRoleId,
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await ctx.Users.AddAsync(admin);
        }
        else
        {
            existing.RoleId = adminRoleId;
            existing.PasswordHash = HashPassword(adminPassword);
            ctx.Users.Update(existing);
        }
        await ctx.SaveChangesAsync();
    }
}

app.Run();
