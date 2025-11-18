using WebSapaForestForStaff.Services;
using WebSapaForestForStaff.Services.Api;
using WebSapaForestForStaff.Services.Api.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();    // để dùng Session trong ApiService
builder.Services.AddSession();

// Register API Services with Dependency Injection
builder.Services.AddHttpClient<IAuthApiService, AuthApiService>();
builder.Services.AddHttpClient<IUserApiService, UserApiService>();
builder.Services.AddHttpClient<IProfileApiService, ProfileApiService>();
builder.Services.AddHttpClient<IPositionApiService, PositionApiService>();

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
});


var app = builder.Build();
app.UseSession();
app.UseCors("AllowAll");




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
