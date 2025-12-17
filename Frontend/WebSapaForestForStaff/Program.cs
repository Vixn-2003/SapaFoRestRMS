using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using WebSapaForestForStaff.Controllers;
using WebSapaForestForStaff.Hubs;
using WebSapaForestForStaff.Services;
using WebSapaForestForStaff.Services.Api;
using WebSapaForestForStaff.Services.Api.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri("https://localhost:7096/");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
});
// HttpClient is created directly in controllers following ManagerMenuController pattern
builder.Services.AddHttpClient<ApiService>(); // để inject HttpClient
builder.Services.AddScoped<ApiService>();     // để inject ApiService
builder.Services.AddHttpClient<KitchenDisplayService>(); // để inject HttpClient cho KitchenDisplayService
builder.Services.AddScoped<KitchenDisplayService>();     // để inject KitchenDisplayService
builder.Services.AddHttpContextAccessor();    // để dùng Session trong ApiService
builder.Services.AddSession();

// Register API Services with Dependency Injection
builder.Services.AddHttpClient<IAuthApiService, AuthApiService>();
builder.Services.AddHttpClient<IUserApiService, UserApiService>();
builder.Services.AddHttpClient<IProfileApiService, ProfileApiService>();
builder.Services.AddHttpClient<IPositionApiService, PositionApiService>();
builder.Services.AddHttpClient<IPaymentApiService, PaymentApiService>();
builder.Services.AddHttpClient<IShiftManagementApiService, ShiftManagementApiService>();
builder.Services.AddHttpClient<ICustomerManagementApiService, CustomerManagementApiService>();
builder.Services.AddHttpClient<IStaffManagementApiService, StaffManagementApiService>();

// Manager Customer API Service
builder.Services.AddHttpClient<ICustomerManagementApiService, CustomerManagementApiService>();

// Owner Dashboard API Services
builder.Services.AddHttpClient<IOwnerDashboardApiService, OwnerDashboardApiService>();
builder.Services.AddHttpClient<IOwnerRevenueApiService, OwnerRevenueApiService>();
builder.Services.AddHttpClient<IOwnerWarehouseAlertApiService, OwnerWarehouseAlertApiService>();

// Counter Staff Dashboard API Services
builder.Services.AddHttpClient<ICounterStaffDashboardApiService, CounterStaffDashboardApiService>();
builder.Services.AddHttpClient<ICounterStaffOrderApiService, CounterStaffOrderApiService>();
builder.Services.AddHttpClient<ICounterTransactionApiService, CounterTransactionApiService>();

// Admin Dashboard API Services
builder.Services.AddHttpClient<IAdminDashboardApiService, AdminDashboardApiService>();

// Keep backward compatibility with old ApiService (can be removed after migration)
builder.Services.AddHttpClient<ApiService>();
builder.Services.AddScoped<ApiService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "SapaFoRestRMS.Auth";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Owner", p => p.RequireRole("Owner"));
    options.AddPolicy("Admin", p => p.RequireRole("Admin", "Owner"));
    options.AddPolicy("Manager", p => p.RequireRole("Manager", "Admin", "Owner"));
    options.AddPolicy("Staff", p => p.RequireRole("Staff", "Manager", "Admin", "Owner"));
    options.AddPolicy("Customer", p => p.RequireRole("Customer"));

    bool HasManagementRole(ClaimsPrincipal user) =>
        user.IsInRole("Owner") || user.IsInRole("Admin") || user.IsInRole("Manager");

    bool HasPositionClaim(ClaimsPrincipal user, int positionId)
    {
        var positionValue = positionId.ToString();
        var hasSingle = user.Claims.Any(c =>
            string.Equals(c.Type, "PositionId", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, positionValue, StringComparison.OrdinalIgnoreCase));

        var hasFromList = user.Claims.Any(c =>
            string.Equals(c.Type, "PositionIds", StringComparison.OrdinalIgnoreCase) &&
            c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(v => string.Equals(v.Trim(), positionValue, StringComparison.OrdinalIgnoreCase)));

        return hasSingle || hasFromList;
    }

    options.AddPolicy("Position:Waiter", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 1))));

    options.AddPolicy("Position:Cashier", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 2))));

    options.AddPolicy("Position:Kitchen", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 3))));

    options.AddPolicy("Position:Inventory", policy =>
        policy.RequireAssertion(ctx => HasManagementRole(ctx.User) ||
            (ctx.User.IsInRole("Staff") && HasPositionClaim(ctx.User, 4))));
});

builder.Services.AddSignalR();


var app = builder.Build();
app.UseSession();
app.UseCors("AllowAll");


app.MapHub<ReservationHub>("/reservationHub");
app.MapHub<RestaurantHub>("/restaurantHub");


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var token = context.Session.GetString("Token");
        var refreshToken = context.Session.GetString("RefreshToken");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
        {
            await context.SignOutAsync("Cookies");
            context.Session.Clear();

            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Auth/Login");
            }
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();