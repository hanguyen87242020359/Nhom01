using ShopBanHoaLyly.Models;
using Microsoft.EntityFrameworkCore;
using ShopBanHoaLyly.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Web;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Đăng ký HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Đăng ký VnPayService
builder.Services.AddScoped<VnPayService>();

// Đăng ký EmailService
builder.Services.AddScoped<EmailService>();

// Đăng ký ChatService
builder.Services.AddScoped<ChatService>();

// Đăng ký Gemini AI
builder.Services.AddSingleton<IGenerativeAI>(provider => 
{
    var apiKey = builder.Configuration["Gemini:ApiKey"];
    return new GoogleAI(apiKey);
});

builder.Services.AddDbContext<ShopHoaLyLyContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ShopHoaLyLyDb");
    options.UseSqlServer(connectionString);
});

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = "MyCookieAuth";
    options.DefaultSignInScheme = "MyCookieAuth";
    options.DefaultChallengeScheme = "MyCookieAuth";
})
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Home/Login"; // Trang đăng nhập
        options.AccessDeniedPath = "/Home/AccessDenied"; // Trang bị từ chối truy cập
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // Cookie tồn tại 2 giờ
        options.SlidingExpiration = true; // Tự động gia hạn khi sử dụng
        options.Cookie.Name = "ShopBanHoaLyly.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
     {
         //thông tin cấu hình google cloud
         options.ClientId = "499826579563-4asuj1n9n2q3o4qoftrqcbvbma28v4n6.apps.googleusercontent.com";
         options.ClientSecret = "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";
         options.CallbackPath = "/signin-google";
         options.SaveTokens = true;
         
         // Đảm bảo các scopes cần thiết
         options.Scope.Add("email");
         options.Scope.Add("profile");
         
         // Cấu hình events
         options.Events.OnRemoteFailure = context => {
             context.Response.Redirect("/Home/Login?error=GoogleLoginFailed");
             context.HandleResponse();
             return Task.CompletedTask;
         };
     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Sử dụng session
app.UseSession();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
