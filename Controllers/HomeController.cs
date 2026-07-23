using DocumentFormat.OpenXml.Wordprocessing;
using GelirGiderPanel.Data;
using GelirGiderPanel.Enums;
using GelirGiderPanel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Controllers
{
    /// <summary>
    /// Dashboard (özet kartlar) ve Chart.js için JSON veri endpoint'leri.
    /// TransactionTypeId: 1 = Gelir, 2 = Gider (seed data ile sabitlenmiştir).
    /// </summary>
    /// 
    [Authorize(Policy = "GuvenPolicy")]
    public class HomeController : Controller
    {
        //private const int GelirId = 1;
        //private const int GiderId = 2;

        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // ============ DASHBOARD ============
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            // Ters girilirse takas (Reports'taki NormalizeDates ile aynı mantık)
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                (startDate, endDate) = (endDate, startDate);

            var query = _context.Transactions.AsNoTracking().AsQueryable();
            if (startDate.HasValue) query = query.Where(t => t.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(t => t.Date <= endDate.Value.Date);

            var model = new DashboardViewModel
            {
                //TotalIncome = await _context.Transactions
                //    .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir)
                //    .SumAsync(t => (decimal?)t.Amount) ?? 0,

                TotalIncome = await query
        .Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Gelir)
        .SumAsync(t => (decimal?)t.Amount) ?? 0,

                TotalExpense = await query
        .Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Gider)
        .SumAsync(t => (decimal?)t.Amount) ?? 0,

                //TransactionCount = await _context.Transactions.CountAsync(),
                TransactionCount = await query.CountAsync(),


                RecentTransactions = await query
        .Include(t => t.Category)
        .Include(t => t.TransactionType)
        .OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
        .Take(5)
        .ToListAsync()
            };
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(model);
        }

        // ============ GRAFİK 1: Gelir/Gider Dağılımı (Doughnut) ============
        // GET /Home/GetIncomeExpenseSummary
        [HttpGet]
        public async Task<IActionResult> GetIncomeExpenseSummary(DateTime? startDate, DateTime? endDate)
        {

            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                (startDate, endDate) = (endDate, startDate);

            var query = _context.Transactions.AsNoTracking().AsQueryable();
            if (startDate.HasValue) query = query.Where(t => t.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(t => t.Date <= endDate.Value.Date);

            var income = await query
                .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expense = await query
                .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gider)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            return Json(new
            {
                labels = new[] { "Gelir", "Gider" },
                data = new[] { income, expense }
            });
        }

        // ============ GRAFİK 2: Kategorilere Göre Dağılım (Bar) ============
        // GET /Home/GetCategorySummary
        // Her kategori için gelir ve gider toplamlarını ayrı ayrı döner.
        [HttpGet]
        public async Task<IActionResult> GetCategorySummary(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                (startDate, endDate) = (endDate, startDate);

            var query = _context.Transactions.AsNoTracking().AsQueryable();
            if (startDate.HasValue) query = query.Where(t => t.Date >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(t => t.Date <= endDate.Value.Date);

            // 3) Kategoriye göre grupla, her grubun içinde gelir ve gideri ayrı topla
            var summary = await query
                .GroupBy(t => t.Category!.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Income = g.Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Gelir)
                              .Sum(t => (decimal?)t.Amount) ?? 0,
                    Expense = g.Where(t => t.TransactionTypeId == (int)TransactionTypeEnum.Gider)
                              .Sum(t => (decimal?)t.Amount) ?? 0
                })
                .OrderByDescending(x => x.Income + x.Expense)
                .ToListAsync();

            return Json(new
            {
                labels = summary.Select(s => s.Category),
                incomeData = summary.Select(s => s.Income),
                expenseData = summary.Select(s => s.Expense)
            });
        }
    }
}
