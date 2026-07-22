using ClosedXML.Excel;
using GelirGiderPanel.Data;
using GelirGiderPanel.Documents;
using GelirGiderPanel.Enums;
using GelirGiderPanel.Models;
using GelirGiderPanel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GelirGiderPanel.Controllers
{

    [Authorize(Policy = "GuvenPolicy")]
    public class ReportsController : Controller
    {
        /// <summary>
        /// Tarih aralığına göre raporlama ve Excel çıktısı.
        /// NuGet: ClosedXML paketi gereklidir.
        /// </summary>

        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        //// GET: /Reports?startDate=...&endDate=...&categoryId=1&transactionTypeId=2
        // Tarih verilmezse varsayılan olarak içinde bulunulan ay gösterilir.
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? categoryId, int? transactionTypeId)
        {
            var (start, end) = NormalizeDates(startDate, endDate);
            var model = await BuildReportAsync(start, end, categoryId, transactionTypeId);
            await LoadFilterDropdownsAsync(categoryId, transactionTypeId);
            return View(model);
        }

        //  GET: /Reports/ExportToExcel?startDate=...&endDate=...&categoryId=...&transactionTypeId=...
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate, int? categoryId, int? transactionTypeId)
        {
            var (start, end) = NormalizeDates(startDate, endDate);
            var report = await BuildReportAsync(start, end , categoryId, transactionTypeId);

            // Dosya adı ve başlıkta gösterilecek filtre açıklaması
            string? categoryName = categoryId.HasValue
                ? (await _context.Categories.FindAsync(categoryId.Value))?.Name
                : null;
            string? typeName = transactionTypeId.HasValue
                ? (await _context.TransactionTypes.FindAsync(transactionTypeId.Value))?.Name
                : null;

            var filterParts = new List<string>();
            if (categoryName != null) filterParts.Add($"Kategori: {categoryName}");
            if (typeName != null) filterParts.Add($"Tür: {typeName}");
            string filterText = filterParts.Any() ? string.Join(" | ", filterParts) : "Tüm kategoriler ve türler";


            using var workbook = new XLWorkbook();

            // ================= SAYFA 1: ÖZET =================
            var wsSummary = workbook.Worksheets.Add("Özet");

            wsSummary.Cell(1, 1).Value = "GELİR - GİDER RAPORU";
            wsSummary.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
            wsSummary.Range(1, 1, 1, 5).Merge();

            wsSummary.Cell(2, 1).Value = $"Dönem: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            wsSummary.Cell(2, 1).Style.Font.SetItalic();
            wsSummary.Range(2, 1, 2, 5).Merge();

            wsSummary.Cell(3, 1).Value = $"Filtre: {filterText}";
            wsSummary.Cell(3, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.Gray);
            wsSummary.Range(3, 1, 3, 5).Merge();

            wsSummary.Cell(5, 1).Value = "Toplam Gelir";
            wsSummary.Cell(5, 2).Value = report.TotalIncome;
            wsSummary.Cell(6, 1).Value = "Toplam Gider";
            wsSummary.Cell(6, 2).Value = report.TotalExpense;
            wsSummary.Cell(7, 1).Value = "Net Bakiye";
            wsSummary.Cell(7, 2).Value = report.NetBalance;

            wsSummary.Range(5, 1, 7, 1).Style.Font.SetBold();
            wsSummary.Range(5, 2, 7, 2).Style.NumberFormat.SetFormat("#,##0.00 ₺");
            wsSummary.Cell(5, 2).Style.Font.SetFontColor(XLColor.FromHtml("#0e7a5f"));
            wsSummary.Cell(6, 2).Style.Font.SetFontColor(XLColor.FromHtml("#b3423a"));
            wsSummary.Cell(7, 2).Style.Font.SetBold();

            // Kategori dökümü tablosu
            int row = 9;
            wsSummary.Cell(row, 1).Value = "Kategori";
            wsSummary.Cell(row, 2).Value = "Gelir";
            wsSummary.Cell(row, 3).Value = "Gider";
            wsSummary.Cell(row, 4).Value = "Net";
            wsSummary.Cell(row, 5).Value = "İşlem Sayısı";
            var headerRange = wsSummary.Range(row, 1, row, 5);
            headerRange.Style.Font.SetBold();
            headerRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#14231f"));
            headerRange.Style.Font.SetFontColor(XLColor.White);

            int summaryDataStart = row + 1;
            foreach (var c in report.CategorySummary)
            {
                row++;
                wsSummary.Cell(row, 1).Value = c.CategoryName;
                wsSummary.Cell(row, 2).Value = c.Income;
                wsSummary.Cell(row, 3).Value = c.Expense;
                wsSummary.Cell(row, 4).Value = c.Net;
                wsSummary.Cell(row, 5).Value = c.TransactionCount;
            }
            if (report.CategorySummary.Any())
                wsSummary.Range(summaryDataStart, 2, row, 4).Style.NumberFormat.SetFormat("#,##0.00 ₺");
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

                // Giderler negatif yazılır: Excel'de sütun toplamı = net bakiye
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

            var namePart = categoryName != null ? $"_{categoryName.Replace(' ', '_')}" : "";
            var fileName = $"GelirGiderRaporu{namePart}_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        // GET: /Reports/ExportToPdf?startDate=...&endDate=...&categoryId=...&transactionTypeId=...
        public async Task<IActionResult> ExportToPdf(
            DateTime? startDate, DateTime? endDate, int? categoryId, int? transactionTypeId)
        {
            var (start, end) = NormalizeDates(startDate, endDate);
            var report = await BuildReportAsync(start, end, categoryId, transactionTypeId);
            var (filterText, categoryName) = await BuildFilterTextAsync(categoryId, transactionTypeId);

            var document = new ReportPdfDocument(report, filterText);
            byte[] pdfBytes = document.GeneratePdf();

            var namePart = categoryName != null ? $"_{categoryName.Replace(' ', '_')}" : "";
            var fileName = $"GelirGiderRaporu{namePart}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }


        // ============ YARDIMCI METOTLAR ============

        /// <summary>
        /// Excel ve PDF başlıklarında gösterilen filtre açıklamasını ve
        /// dosya adında kullanılan kategori adını üretir.
        /// </summary>
        private async Task<(string filterText, string? categoryName)> BuildFilterTextAsync(
            int? categoryId, int? transactionTypeId)
        {
            string? categoryName = categoryId.HasValue
                ? (await _context.Categories.FindAsync(categoryId.Value))?.Name
                : null;
            string? typeName = transactionTypeId.HasValue
                ? (await _context.TransactionTypes.FindAsync(transactionTypeId.Value))?.Name
                : null;

            var filterParts = new List<string>();
            if (categoryName != null) filterParts.Add($"Kategori: {categoryName}");
            if (typeName != null) filterParts.Add($"Tür: {typeName}");
            string filterText = filterParts.Any()
                ? string.Join(" | ", filterParts)
                : "Tüm kategoriler ve türler";

            return (filterText, categoryName);
        }


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

        /// <summary>
        /// Filtreli rapor verisini üretir. categoryId ve transactionTypeId null ise filtre uygulanmaz.
        /// </summary>
        private async Task<ReportViewModel> BuildReportAsync(DateTime start, DateTime end, int? categoryId, int? transactionTypeId)
        {
            /*
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
                .ToList();*/

            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionType)
                .Where(t => t.Date >= start && t.Date <= end);

            // ---- Dinamik filtreler ----
            if (categoryId.HasValue)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            if (transactionTypeId.HasValue)
                query = query.Where(t => t.TransactionTypeId == transactionTypeId.Value);

            var transactions = await query
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
                CategoryId = categoryId,
                TransactionTypeId = transactionTypeId,
                Transactions = transactions,
                TotalIncome = transactions.Where(t => t.TransactionTypeId == (int)TransactionStatus.Gelir).Sum(t => t.Amount),
                TotalExpense = transactions.Where(t => t.TransactionTypeId == (int)TransactionStatus.Gider).Sum(t => t.Amount),
                CategorySummary = categorySummary
            };
        }

        /// <summary>
        /// Filtre formundaki dropdown'ları hazırlar (seçili değerler korunur).
        /// </summary>
        private async Task LoadFilterDropdownsAsync(int? selectedCategoryId, int? selectedTypeId)
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                nameof(Category.Id), nameof(Category.Name), selectedCategoryId);

            ViewBag.TransactionTypes = new SelectList(
                await _context.TransactionTypes.ToListAsync(),
                nameof(TransactionType.Id), nameof(TransactionType.Name), selectedTypeId);
        }

    }
}
