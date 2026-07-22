using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using GelirGiderPanel.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);


// QuestPDF: yıllık geliri 1M USD altındaki işletmeler için ücretsiz Community lisansı.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// ============ SERVİSLER ============

// MVC
builder.Services.AddControllersWithViews();

// Entity Framework Core + MSSQL
// Bağlantı dizesi appsettings.json içindeki "DefaultConnection" anahtarından okunur.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(opt =>
{
    opt.LoginPath = "/Auth/Login";
    opt.Cookie.Name = "UserDetail";
    opt.AccessDeniedPath = "/Auth/AccessDenied";
    // 
    opt.ExpireTimeSpan = TimeSpan.FromHours(1); //Cookie) varsayılan yaşam süresini
    opt.SlidingExpiration = true; //aktifliğine bağlı olarak oturum süresini dinamik olarak uzatan

});

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminPolicy", policy => policy.RequireClaim("role", "admin", "Admin"));
    opt.AddPolicy("GuvenPolicy", policy => policy.RequireClaim("role", "User", "Admin", "admin"));
});

var app = builder.Build();

// ============ MIDDLEWARE PIPELINE ============

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Kültür ayarı: Tarihler Türkçe gösterilsin, ancak sayısal model binding'de
// ondalık ayracı sorunlarını önlemek için sayı formatı nokta (.) bazlı tutulur.
// (HTML number input'ları her zaman nokta ile gönderir.)
var trCulture = new CultureInfo("tr-TR");
trCulture.NumberFormat.NumberDecimalSeparator = ".";
trCulture.NumberFormat.CurrencyDecimalSeparator = ".";

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(trCulture),
    SupportedCultures = new List<CultureInfo> { trCulture },
    SupportedUICultures = new List<CultureInfo> { trCulture }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
