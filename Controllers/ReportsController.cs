using ClosedXML.Excel;
using GelirGiderPanel.Data;
using GelirGiderPanel.Enums;
using GelirGiderPanel.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Controllers
{
    public class ReportsController : Controller
    {
        /// <summary>
        /// Tarih aralığına göre raporlama ve Excel çıktısı.
        /// NuGet: ClosedXML paketi gereklidir.
        /// </summary>
        //private const int GelirId = 1;
        //private const int GiderId = 2;

        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Reports?startDate=2026-07-01&endDate=2026-07-31
        // Tarih verilmezse varsayılan olarak içinde bulunulan ay gösterilir.
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = NormalizeDates(startDate, endDate);
            var model = await BuildReportAsync(start, end);
            return View(model);
        }

        // GET: /Reports/ExportToExcel?startDate=...&endDate=...
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = NormalizeDates(startDate, endDate);
            var report = await BuildReportAsync(start, end);

            using var workbook = new XLWorkbook();

            // ================= SAYFA 1: ÖZET =================
            var wsSummary = workbook.Worksheets.Add("Özet");

            wsSummary.Cell(1, 1).Value = "GELİR - GİDER RAPORU";
            wsSummary.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
            wsSummary.Range(1, 1, 1, 4).Merge();

            wsSummary.Cell(2, 1).Value = $"Dönem: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            wsSummary.Cell(2, 1).Style.Font.SetItalic();
            wsSummary.Range(2, 1, 2, 4).Merge();

            wsSummary.Cell(4, 1).Value = "Toplam Gelir";
            wsSummary.Cell(4, 2).Value = report.TotalIncome;
            wsSummary.Cell(5, 1).Value = "Toplam Gider";
            wsSummary.Cell(5, 2).Value = report.TotalExpense;
            wsSummary.Cell(6, 1).Value = "Net Bakiye";
            wsSummary.Cell(6, 2).Value = report.NetBalance;

            wsSummary.Range(4, 1, 6, 1).Style.Font.SetBold();
            wsSummary.Range(4, 2, 6, 2).Style.NumberFormat.SetFormat("#,##0.00 ₺");
            wsSummary.Cell(4, 2).Style.Font.SetFontColor(XLColor.FromHtml("#0e7a5f"));
            wsSummary.Cell(5, 2).Style.Font.SetFontColor(XLColor.FromHtml("#b3423a"));
            wsSummary.Cell(6, 2).Style.Font.SetBold();

            // Kategori dökümü tablosu
            int row = 8;
            wsSummary.Cell(row, 1).Value = "Kategori";
            wsSummary.Cell(row, 2).Value = "Gelir";
            wsSummary.Cell(row, 3).Value = "Gider";
            wsSummary.Cell(row, 4).Value = "Net";
            wsSummary.Cell(row, 5).Value = "İşlem Sayısı";
            var headerRange = wsSummary.Range(row, 1, row, 5);
            headerRange.Style.Font.SetBold();
            headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#14231f"));
            headerRange.Style.Font.SetFontColor(XLColor.White);

            foreach (var c in report.CategorySummary)
            {
                row++;
                wsSummary.Cell(row, 1).Value = c.CategoryName;
                wsSummary.Cell(row, 2).Value = c.Income;
                wsSummary.Cell(row, 3).Value = c.Expense;
                wsSummary.Cell(row, 4).Value = c.Net;
                wsSummary.Cell(row, 5).Value = c.TransactionCount;
            }
            wsSummary.Range(9, 2, row, 4).Style.NumberFormat.SetFormat("#,##0.00 ₺");
            wsSummary.Columns().AdjustToContents();

            // ================= SAYFA 2: İŞLEM DETAYLARI =================
            var wsDetail = workbook.Worksheets.Add("İşlem Detayları");

            wsDetail.Cell(1, 1).Value = "Tarih";
            wsDetail.Cell(1, 2).Value = "Açıklama";
            wsDetail.Cell(1, 3).Value = "Kategori";
            wsDetail.Cell(1, 4).Value = "Tür";
            wsDetail.Cell(1, 5).Value = "Tutar";
            var detailHeader = wsDetail.Range(1, 1, 1, 5);
            detailHeader.Style.Font.SetBold();
            detailHeader.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#14231f"));
            detailHeader.Style.Font.SetFontColor(XLColor.White);

            int detailRow = 1;
            foreach (var t in report.Transactions)
            {
                detailRow++;
                wsDetail.Cell(detailRow, 1).Value = t.Date;
                wsDetail.Cell(detailRow, 1).Style.DateFormat.SetFormat("dd.MM.yyyy");
                wsDetail.Cell(detailRow, 2).Value = t.Description;
                wsDetail.Cell(detailRow, 3).Value = t.Category?.Name;
                wsDetail.Cell(detailRow, 4).Value = t.TransactionType?.Name;

                // Gider tutarlarını negatif yazıyoruz ki Excel'de toplam alınca net bakiye çıksın.
                var signedAmount = t.TransactionTypeId == (int)TransactionStatus.Gider ? -t.Amount : t.Amount;
                wsDetail.Cell(detailRow, 5).Value = signedAmount;
                wsDetail.Cell(detailRow, 5).Style.NumberFormat.SetFormat("#,##0.00 ₺");
                wsDetail.Cell(detailRow, 5).Style.Font.SetFontColor(
                    t.TransactionTypeId == (int)TransactionStatus.Gider
                        ? XLColor.FromHtml("#b3423a")
                        : XLColor.FromHtml("#0e7a5f"));
            }

            // Toplam satırı (SUM formülü ile — kullanıcı satır silerse kendini günceller)
            if (report.Transactions.Any())
            {
                detailRow++;
                wsDetail.Cell(detailRow, 4).Value = "NET TOPLAM";
                wsDetail.Cell(detailRow, 4).Style.Font.SetBold();
                wsDetail.Cell(detailRow, 5).FormulaA1 = $"=SUM(E2:E{detailRow - 1})";
                wsDetail.Cell(detailRow, 5).Style.NumberFormat.SetFormat("#,##0.00 ₺");
                wsDetail.Cell(detailRow, 5).Style.Font.SetBold();
            }

            wsDetail.SheetView.FreezeRows(1);           // Başlık satırı sabit kalsın
            wsDetail.RangeUsed()?.SetAutoFilter();      // Filtre okları
            wsDetail.Columns().AdjustToContents();

            // ================= DOSYAYI GÖNDER =================
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"GelirGiderRaporu_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ============ YARDIMCI METOTLAR ============

        /// <summary>
        /// Tarih verilmemişse içinde bulunulan ayı kullanır;
        /// başlangıç > bitiş ise tarihleri yer değiştirir.
        /// </summary>
        private static (DateTime start, DateTime end) NormalizeDates(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.Today;
            var start = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
            var end = endDate?.Date ?? start.AddMonths(1).AddDays(-1);

            if (start > end)
                (start, end) = (end, start);

            return (start, end);
        }

        private async Task<ReportViewModel> BuildReportAsync(DateTime start, DateTime end)
        {
            // Tek sorguda aralıktaki tüm işlemleri çek, özetleri bellekte hesapla.
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionType)
                .Where(t => t.Date >= start && t.Date <= end)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToListAsync();

            var categorySummary = transactions
                .GroupBy(t => t.Category!.Name)
                .Select(g => new CategoryReportRow
                {
                    CategoryName = g.Key,
                    Income = g.Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir).Sum(t => t.Amount),
                    Expense = g.Where(t => t.TransactionTypeId == (int)TransactionStatus.Gider).Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(c => c.Expense + c.Income)
                .ToList();

            return new ReportViewModel
            {
                StartDate = start,
                EndDate = end,
                Transactions = transactions,
                TotalIncome = transactions.Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir).Sum(t => t.Amount),
                TotalExpense = transactions.Where(t => t.TransactionTypeId == (int)TransactionStatus.Gider).Sum(t => t.Amount),
                CategorySummary = categorySummary
            };
        }
    }
}
