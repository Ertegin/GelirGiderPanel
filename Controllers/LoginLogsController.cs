using GelirGiderPanel.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoginLogsController : Controller
    {
        private readonly AppDbContext _db;
        public LoginLogsController(AppDbContext db)
        {
            _db = db;
        }
        // GET: /LoginLogs?startDate=...&endDate=...&search=...
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string? search)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                (startDate, endDate) = (endDate, startDate);

            var query = _db.LoginLogs.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
                query = query.Where(l => l.LoginTime >= startDate.Value.Date);
            if (endDate.HasValue)
                query = query.Where(l => l.LoginTime < endDate.Value.Date.AddDays(1));
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l => l.UserName.Contains(search.Trim()));

            // En yeni girişler üstte; ekranı boğmamak için son 500 kayıt
            var logs = await query
                .OrderByDescending(l => l.LoginTime)
                .Take(500)
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Search = search;
            ViewBag.TotalCount = await query.CountAsync();

            return View(logs);
        }
    }
}
