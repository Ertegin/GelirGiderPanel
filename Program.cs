using System.Globalization;
using GelirGiderPanel.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ============ SERVİSLER ============

// MVC
builder.Services.AddControllersWithViews();

// Entity Framework Core + MSSQL
// Bağlantı dizesi appsettings.json içindeki "DefaultConnection" anahtarından okunur.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
