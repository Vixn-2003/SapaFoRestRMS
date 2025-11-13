using WebSapaForestForStaff.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// HttpClient is created directly in controllers following ManagerMenuController pattern
builder.Services.AddHttpClient<ApiService>(); // để inject HttpClient
builder.Services.AddScoped<ApiService>();     // để inject ApiService
builder.Services.AddHttpClient<KitchenDisplayService>(); // để inject HttpClient cho KitchenDisplayService
builder.Services.AddScoped<KitchenDisplayService>();     // để inject KitchenDisplayService
builder.Services.AddHttpContextAccessor();    // để dùng Session trong ApiService
builder.Services.AddSession();

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