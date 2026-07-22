using GelirGiderPanel.Data;
using GelirGiderPanel.Enums;
using GelirGiderPanel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Controllers
{
    /// <summary>
    /// Dashboard (özet kartlar) ve Chart.js için JSON veri endpoint'leri.
    /// TransactionTypeId: 1 = Gelir, 2 = Gider (seed data ile sabitlenmiştir).
    /// </summary>
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
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TotalIncome = await _context.Transactions
                    .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0,

                TotalExpense = await _context.Transactions
                    .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gider)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0,

                TransactionCount = await _context.Transactions.CountAsync(),

                RecentTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.TransactionType)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // ============ GRAFİK 1: Gelir/Gider Dağılımı (Doughnut) ============
        // GET /Home/GetIncomeExpenseSummary
        [HttpGet]
        public async Task<IActionResult> GetIncomeExpenseSummary()
        {
            var income = await _context.Transactions
                .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expense = await _context.Transactions
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
        public async Task<IActionResult> GetCategorySummary()
        {
            var summary = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new
                {
                    category = c.Name,
                    income = c.Transactions
                        .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir)
                        .Sum(t => (decimal?)t.Amount) ?? 0,
                    expense = c.Transactions
                        .Where(t => t.TransactionTypeId == (int)TransactionStatus.Gider)
                        .Sum(t => (decimal?)t.Amount) ?? 0
                })
                .OrderBy(x => x.category)
                .ToListAsync();

            return Json(new
            {
                labels = summary.Select(s => s.category),
                incomeData = summary.Select(s => s.income),
                expenseData = summary.Select(s => s.expense)
            });
        }
    }
}
