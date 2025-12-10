
using BusinessAccessLayer.Hubs;
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
using DomainAccessLayer.Enums;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SapaFoRestRMSAPI.Services;
using System.Text;
using SapaFoRestRMSAPI.Hubs;
using BusinessAccessLayer.Services.Inventory;
using QuestPDF.Infrastructure;

// ✅ FIX: Configure QuestPDF License (Community - Free for commercial use)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyDatabase"), sqlOptions =>
    {
        sqlOptions.CommandTimeout(60); // 60 seconds command timeout
    });
});

//Show connection string in console
Console.WriteLine(builder.Configuration.GetConnectionString("MyDatabase"));


//check error sql
builder.Logging.AddConsole();
builder.Services.AddDbContext<SapaFoRestRmsContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MyDatabase"),
        sqlOptions =>
        {
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddEndpointsApiExplorer();
// Bật middleware Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SapaFoRestSMS API",
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
    options.Cookie.Name = "SapaFoRestRMS.Auth";

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
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IOrderTableRepository, OrderTableRepository>();
// Role Management
builder.Services.AddScoped<IRoleService, RoleService>();

// Position Management
builder.Services.AddScoped<IPositionService, PositionService>();

// Các dịch vụ khác
builder.Services.AddScoped<IOrderTableService, OrderTableService>();

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

builder.Services.AddScoped<IRestaurantIntroRepository, RestaurantIntroRepository>();
builder.Services.AddScoped<IRestaurantIntroService, RestaurantIntroService>();

builder.Services.AddScoped<IManagerCategoryService, ManagerCategoryService>();
builder.Services.AddScoped<IInventoryIngredientService, InventoryIngredientService>();
builder.Services.AddScoped<IManagerSupplierService, ManagerSupplierService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IStockTransactionService, StockTransactionService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IInventoryAnalyticsService, InventoryAnalyticsService>();
builder.Services.AddHostedService<ReorderLevelBackgroundJob>();

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPurchaseOrderDetailService, PurchaseOrderDetailService>();




builder.Services.AddScoped<IMarketingCampaignRepository, MarketingCampaignRepository>();
builder.Services.AddScoped<IMarketingCampaignService, MarketingCampaignService>();
builder.Services.AddScoped<IKitchenDisplayService, KitchenDisplayService>();
builder.Services.AddScoped<IWaiterOrderTrackingService, WaiterOrderTrackingService>();

builder.Services.AddScoped<ICloudinaryService, BusinessAccessLayer.Services.CloudinaryService>();

//UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationDepositRepository, ReservationDepositRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<ReservationDepositService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

// Unit of Work and User Repository mapping
builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Users);
builder.Services.AddScoped<IUserService, UserService>();

// Auth and User Management services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
builder.Services.AddScoped<BusinessAccessLayer.Services.OtpService>();
builder.Services.AddScoped<BusinessAccessLayer.Services.Interfaces.IPhoneAuthService, BusinessAccessLayer.Services.PhoneAuthService>();
//Table Service/Repository
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<ITableService, TableService>();
// Area Repository
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<IAreaService, AreaService>();
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


//voucher
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<IVoucherService, VoucherService>();

//Payrool
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();
builder.Services.AddScoped<IPayrollService, PayrollService>();

builder.Services.AddScoped<ICounterStaffRepository, CounterStaffRepository>();
builder.Services.AddScoped<ICounterStaffService, CounterStaffService>();


// Area Repository
builder.Services.AddScoped<IOrderTableRepository, OrderTableRepository>();
builder.Services.AddScoped<IOrderTableService, OrderTableService>();

//DashBoardTable
builder.Services.AddScoped<IDashboardTableRepository, DashboardTableRepository>();
builder.Services.AddScoped<IDashboardTableService, DashboardTableService>();
builder.Services.AddScoped<ISupplierManagerService, SupplierManagerService>();
// capacityStatistic
builder.Services.AddScoped<ICapacityStatisticsRepository, CapacityStatisticsRepository>();
builder.Services.AddScoped<ICapacityStatisticsService, CapacityStatisticsService>();

//
builder.Services.Configure<MomoOptions>(builder.Configuration.GetSection("Momo"));
builder.Services.AddSingleton<IMomoService, MomoService>();
builder.Services.AddScoped<IStaffProfileService, StaffProfileService>();
//daytype
builder.Services.AddScoped<IDayTypeRepository, DayTypeRepository>();
builder.Services.AddScoped<IDayTypeService, DayTypeService>();
//shifttemplate
builder.Services.AddScoped<IShiftTemplateRepository, ShiftTemplateRepository>();
builder.Services.AddScoped<IShiftTemplateService, ShiftTemplateService>();
//department
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
//shift 
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IShiftService, ShiftService>();
//shiftassignment
builder.Services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();
builder.Services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();
// Payment Service/Repository
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICustomerVipService, CustomerVipService>();

// AuditLog Service
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddScoped<IShiftManagementService, ShiftManagementService>();

// Receipt Service - Pass WebRootPath from IWebHostEnvironment
builder.Services.AddScoped<IReceiptService>(sp =>
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logger = sp.GetRequiredService<ILogger<ReceiptService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var serviceProvider = sp; // Pass service provider to access ICloudinaryService
    var webRootPath = string.IsNullOrWhiteSpace(env.WebRootPath)
        ? Path.Combine(env.ContentRootPath, "wwwroot")
        : env.WebRootPath;

    if (!Directory.Exists(webRootPath))
    {
        Directory.CreateDirectory(webRootPath);
    }

    return new ReceiptService(unitOfWork, webRootPath, logger, configuration, serviceProvider);
});

// SalaryChangeRequest Service/Repository
builder.Services.AddScoped<ISalaryChangeRequestRepository, SalaryChangeRequestRepository>();
builder.Services.AddScoped<ISalaryChangeRequestService, SalaryChangeRequestService>();

builder.Services.AddSingleton<SapaFoRestRMSAPI.Services.CloudinaryService>();



builder.Services.AddSignalR();
// Đăng ký dịch vụ chạy ngầm của chúng ta
builder.Services.AddHostedService<OrderStatusUpdaterService>();
builder.Services.AddSignalR();

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

// ================================
// ✅ CORS CONFIGURATION (Centralized)
// ================================
// Đọc danh sách origins từ appsettings.json
// Mỗi dev chỉ cần chỉnh sửa appsettings.Development.json với IP của mình
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        // Đọc từ appsettings.json hoặc appsettings.Development.json
        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5123" }; // Fallback mặc định nếu không có config

        // Log để dễ debug
        Console.WriteLine("🔒 CORS Allowed Origins:");
        foreach (var origin in allowedOrigins)
        {
            Console.WriteLine($"   ✅ {origin}");
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Bắt buộc nếu frontend dùng fetch hoặc jQuery.ajax
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins); // <-- THÊM DÒNG NÀY
//// Bật CORS
//app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<KitchenHub>("/kitchenHub");


app.MapHub<ReservationHub>("/reservationHub");
app.MapHub<RestaurantHub>("/restaurantHub");
app.MapControllers();

//await app.EnsureSeededAsync();

// Upsert Admin + seed demo data for development/testing
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<SapaFoRestRmsContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    // Seed core lookup data
    //await DataSeeder.SeedPositionsAsync(ctx);
    //await DataSeeder.SeedTestStaffAndManagerAsync(ctx);
    //await DataSeeder.SeedTestCustomerAsync(ctx);
    //await MenuDataSeeder.SeedMenuItemsAsync(ctx); // Seed menu items first (always runs)
    //await MenuDataSeeder.SeedInventoryDataAsync(ctx); // Seed ingredients, recipes, batches, and export transactions
    //await MenuDataSeeder.SeedKitchenOrdersAsync(ctx);

    // 🔹 Seed thêm dữ liệu workflow thu ngân + combo cho bếp (gồm Order 3–8)
    //await DataSeeder.SeedCashierWorkflowTestAsync(ctx);
    //await MenuDataSeeder.SeedStaffWithAllPositionsAsync(ctx); // Seed staff with all positions for testing
    var adminEmail = config["AdminAccount:Email"];
    var adminPassword = config["AdminAccount:Password"];
    Console.WriteLine("AdminAccount Email: " + builder.Configuration["AdminAccount:Email"]);
    Console.WriteLine("Environment: " + builder.Environment.EnvironmentName);
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