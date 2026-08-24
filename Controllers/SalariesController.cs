using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using GelirGiderPanel.Data;
using GelirGiderPanel.Documents;
using GelirGiderPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace GelirGiderPanel.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class SalariesController : Controller
    {
        private readonly AppDbContext _db;
        public SalariesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Salaries?search=...
        public async Task<IActionResult> Index(string? search)
        {
            //         var salaries = await _db.Salaries
            //.AsNoTracking()
            //.OrderBy(s => s.Name)
            //.ToListAsync();
            var query = _db.Salaries.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(s => s.Name.Contains(term));
            }

            var salaries = await query.OrderBy(s => s.Name).ToListAsync();

            ViewBag.Total = salaries.Sum(s => s.Amount);
            ViewBag.Search = search;
            return View(salaries);

        }

        // POST: /Salaries/Save — modal formu (Id=0 yeni, Id>0 düzenleme)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Salary form)
        {
            if (string.IsNullOrWhiteSpace(form.Name))
            {
                TempData["Error"] = "İsim zorunludur.";
                return RedirectToAction(nameof(Index));
            }
            if (form.Amount <= 0)
            {
                TempData["Error"] = "Maaş tutarı sıfırdan büyük olmalıdır.";
                return RedirectToAction(nameof(Index));
            }

            if (form.Id > 0)
            {
                var existing = await _db.Salaries.FindAsync(form.Id);
                if (existing == null) return NotFound();

                existing.Name = form.Name.Trim();
                existing.Amount = Math.Round(form.Amount, 2);
                existing.Description = string.IsNullOrWhiteSpace(form.Description)
                    ? null : form.Description.Trim();
                TempData["Success"] = "Maaş kaydı güncellendi.";
            }
            else
            {
                _db.Salaries.Add(new Salary
                {
                    Name = form.Name.Trim(),
                    Amount = Math.Round(form.Amount, 2),
                    Description = string.IsNullOrWhiteSpace(form.Description)
                        ? null : form.Description.Trim(),
                    CreatedAt = DateTime.Now
                });
                TempData["Success"] = "Maaş kaydı eklendi.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Salaries/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var salary = await _db.Salaries.FindAsync(id);
            if (salary == null) return NotFound();

            _db.Salaries.Remove(salary);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{salary.Name}\" maaş kaydı silindi.";
            return RedirectToAction(nameof(Index));
        }


        // GET: /Salaries/ExportExcel
        public async Task<IActionResult> ExportExcel(string? search)
        {
            //var salaries = await _db.Salaries
            //    .AsNoTracking()
            //    .OrderBy(s => s.Name)
            //    .ToListAsync();

            var query = _db.Salaries.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(s => s.Name.Contains(term));
            }
            var salaries = await query.OrderBy(s => s.Name).ToListAsync();


            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Maaşlar");

            ws.Cell(1, 1).Value = "Maaş Listesi";
            ws.Cell(1, 1).Style.Font.SetBold().Font.FontSize = 14;
            ws.Range(1, 1, 1, 3).Merge();
            ws.Cell(2, 1).Value = string.IsNullOrWhiteSpace(search)
                ? $"Tarih: {DateTime.Now:dd.MM.yyyy}"
                : $"Tarih: {DateTime.Now:dd.MM.yyyy} | Filtre: \"{search.Trim()}\"";
            ws.Range(2, 1, 2, 3).Merge();

            int headerRow = 4;
            string[] headers = { "İsim", "Maaş (TL)", "Açıklama" };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(headerRow, i + 1);
                c.Value = headers[i];
                c.Style.Font.SetBold().Font.FontColor = XLColor.White;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#14231f");
            }

            int row = headerRow + 1;
            foreach (var s in salaries)
            {
                ws.Cell(row, 1).Value = s.Name;
                ws.Cell(row, 2).Value = s.Amount;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 3).Value = s.Description ?? "";
                row++;
            }

            // SUM formüllü toplam satırı
            ws.Cell(row, 1).Value = "TOPLAM";
            ws.Cell(row, 2).FormulaA1 = $"SUM(B{headerRow + 1}:B{row - 1})";
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            var totalRange = ws.Range(row, 1, row, 3);
            totalRange.Style.Font.SetBold();
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f7f6");
            ws.Cell(row, 2).Style.Font.FontColor = XLColor.FromHtml("#b3423a");

            ws.SheetView.FreezeRows(headerRow);
            ws.Range(headerRow, 1, row - 1, 3).SetAutoFilter();
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MaasListesi_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // GET: /Salaries/ExportPdf?search=...
        public async Task<IActionResult> ExportPdf(string? search)
        {
            //var salaries = await _db.Salaries
            //    .AsNoTracking()
            //    .OrderBy(s => s.Name)
            //    .ToListAsync();

            var query = _db.Salaries.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(s => s.Name.Contains(term));
            }
            var salaries = await query.OrderBy(s => s.Name).ToListAsync();

            var document = new SalaryPdfDocument(salaries, search?.Trim());

            // var document = new SalaryPdfDocument(salaries);
            byte[] pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", $"MaasListesi_{DateTime.Now:yyyyMMdd}.pdf");
        }

    }
}
