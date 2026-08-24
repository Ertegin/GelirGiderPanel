using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using GelirGiderPanel.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

var builder = WebApplication.CreateBuilder(args);


// QuestPDF: yıllık geliri 1M USD altındaki işletmeler için ücretsiz Community lisansı.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// ============ SERVİSLER ============

// MVC
builder.Services.AddControllersWithViews();

/*Sayfaların önbelleklenmesini kapat
 builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new ResponseCacheAttribute
    {
        NoStore = true,
        Location = ResponseCacheLocation.None
    });
});
 */

builder.Services.AddWebMarkupMin(options =>
{
    options.AllowMinificationInDevelopmentEnvironment = false;
})
.AddHtmlMinification(options =>
{
    options.MinificationSettings.RemoveHtmlComments = true;
    options.MinificationSettings.MinifyEmbeddedCssCode = true;
    options.MinificationSettings.MinifyEmbeddedJsCode = true;
    options.MinificationSettings.RemoveRedundantAttributes = true;
    options.MinificationSettings.RemoveEmptyAttributes = false;
    options.MinificationSettings.WhitespaceMinificationMode = WebMarkupMin.Core.WhitespaceMinificationMode.Aggressive;
});


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
    opt.ExpireTimeSpan = TimeSpan.FromHours(2); //Cookie) varsayılan yaşam süresini
    opt.SlidingExpiration = true; //aktifliğine bağlı olarak oturum süresini dinamik olarak uzatan

    opt.Events.OnSignedIn = async context =>
    {
        // Kullanıcı giriş yaptıktan sonra yapılacak işlemler
        // Örneğin, kullanıcı bilgilerini güncelleme veya loglama
        try
        {
            var db = context.HttpContext.RequestServices
            .GetRequiredService<GelirGiderPanel.Data.AppDbContext>();

            db.LoginLogs.Add(new GelirGiderPanel.Models.LoginLog
            {
                UserName = context.Principal?.Identity?.Name ?? "Bilinmiyor",
                IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                LoginTime = DateTime.Now
            });
            await db.SaveChangesAsync();
        }
        catch 
        {
            /*
             Ve yayına çıktığınızda RemoteIpAddress büyük ihtimalle gerçek ziyaretçi IP'si yerine hosting'in proxy IP'sini gösterecek
            — gerçek IP isterseniz X-Forwarded-For başlığını okuyan ForwardedHeaders middleware'i eklemek gerekir, ama bu kritik değil, sonraya bırakabilirsiniz.
             */

        }
    };
});

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminPolicy", policy => policy.RequireClaim("role", "admin", "Admin"));
    opt.AddPolicy("GuvenPolicy", policy => policy.RequireClaim("role", "User", "Admin", "admin"));
});

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    using var serviceScope = app.Services.CreateScope();
//    var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
//    dbContext.Database.Migrate(); // Bekleyen migration'ları uygular

//}
// ============ MIDDLEWARE PIPELINE ============

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    db.Database.Migrate();   // Bekleyen migration'ları otomatik uygular
//    await DbSeeder.SeedAsync(scope.ServiceProvider);
//}
app.UseWebMarkupMin();
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
