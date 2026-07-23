using ClosedXML.Excel;
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
    public class CariController : Controller
    {
        AppDbContext _context;
        public CariController(AppDbContext context)
        {
            _context = context;
        }
        // ============ HESAPLAR ============

        // GET: /Cari
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.CariAccounts
                .Select(a => new CariAccountSummaryVm
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    IsActive = a.IsActive,
                    TransactionCount = a.Transactions.Count,
                    Balance = a.OpeningBalance
                              + a.Transactions.Sum(t => (decimal?)t.DebitAmount ?? 0)
                              - a.Transactions.Sum(t => (decimal?)t.CreditAmount ?? 0)
                })
                .OrderByDescending(a => a.IsActive)
                .ThenBy(a => a.Name)
                .ToListAsync();

            var vm = new CariIndexVm
            {
                Accounts = accounts,
                // Pozitif bakiye = müşteri size borçlu (sizin alacağınız)
                TotalReceivable = accounts.Where(a => a.Balance > 0).Sum(a => a.Balance),
                // Negatif bakiye = siz müşteriye borçlusunuz
                TotalPayable = accounts.Where(a => a.Balance < 0).Sum(a => -a.Balance)
            };

            return View(vm);
        }

        // GET: /Cari/CreateAccount
        public IActionResult CreateAccount() => View(new CariAccount());

        // POST: /Cari/CreateAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CariAccount account)
        {
            if (await _context.CariAccounts.AnyAsync(a => a.Name == account.Name))
                ModelState.AddModelError(nameof(account.Name), "Bu isimde bir cari hesap zaten var.");

            if (!ModelState.IsValid) return View(account);

            account.CreatedAt = DateTime.Now;
            _context.CariAccounts.Add(account);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{account.Name}\" cari hesabı oluşturuldu.";
            return RedirectToAction(nameof(Details), new { id = account.Id });
        }

        // GET: /Cari/EditAccount/5
        public async Task<IActionResult> EditAccount(int id)
        {
            var account = await _context.CariAccounts.FindAsync(id);
            if (account == null) return NotFound();
            return View(account);
        }

        // POST: /Cari/EditAccount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(int id, CariAccount account)
        {
            if (id != account.Id) return NotFound();

            if (await _context.CariAccounts.AnyAsync(a => a.Name == account.Name && a.Id != id))
                ModelState.AddModelError(nameof(account.Name), "Bu isimde başka bir cari hesap var.");

            if (!ModelState.IsValid) return View(account);

            var existing = await _context.CariAccounts.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = account.Name;
            existing.Description = account.Description;
            existing.OpeningBalance = account.OpeningBalance;
            existing.IsActive = account.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{existing.Name}\" hesabı güncellendi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Cari/DeleteAccount/5 — hareketi olan hesap silinmez, pasife alınır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.CariAccounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (account == null) return NotFound();
            Console.WriteLine("test55", account.Transactions);
            if (account.Transactions.Any())
            {
                if (!account.IsActive)
                {
                    if (account.Transactions != null)
                    {
                        _context.CariTransactions.RemoveRange(account.Transactions);
                    }
                    _context.CariAccounts.Remove(account);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"\"{account.Name}\" hesabı silindi.";
                }
                else
                {
                    account.IsActive = false;
                    await _context.SaveChangesAsync();
                    TempData["Warning"] = $"\"{account.Name}\" hesabında hareket bulunduğu için silinmedi, pasife alındı.";
                }

            }
            else
            {
                _context.CariAccounts.Remove(account);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"\"{account.Name}\" hesabı silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============ DEFTER (HESAP DETAYI) ============

        // GET: /Cari/Details/5
        public async Task<IActionResult> Details(int id, DateTime? startDate, DateTime? endDate)
        {
            /*
            var account = await _context.CariAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
            if (account == null) return NotFound();

            var transactions = await _context.CariTransactions
                .AsNoTracking()
                .Where(t => t.CariAccountId == id)
                .OrderByDescending(t => t.Date).ThenBy(t => t.Id)  //OrderBy
                .ToListAsync();

            // Kasaya bağlı hareketlerin kategori bilgisi (modalda ön seçim için)
            var linkedIds = transactions
                .Where(t => t.LinkedTransactionId.HasValue)
                .Select(t => t.LinkedTransactionId!.Value)
                .ToList();
            var linkedCategories = linkedIds.Any()
                ? await _context.Transactions
                    .Where(x => linkedIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.CategoryId)
                : new Dictionary<int, int>();

            // 2. Değişiklik: Yürüyen bakiyenin doğru hesaplanması
            // İşlemler ters sırada (en yeni en üstte) olduğu için toplam bakiye:
            // Açılış Bakiyesi + Toplam Borç - Toplam Alacak
            decimal totalDebit = transactions.Sum(t => t.DebitAmount);
            decimal totalCredit = transactions.Sum(t => t.CreditAmount);
            decimal currentBalance = account.OpeningBalance + totalDebit - totalCredit;
            */
            /**/

            // Yürüyen bakiye
            var rows = new List<CariLedgerRowVm>();
            //decimal balance = account.OpeningBalance;
           // decimal runningBalance = currentBalance;

            /*foreach (var t in transactions)
            {
                balance += t.DebitAmount - t.CreditAmount;
                rows.Add(new CariLedgerRowVm
                {
                    Transaction = t,
                    RunningBalance = balance,
                    KasaCategoryId = t.LinkedTransactionId.HasValue
                                     && linkedCategories.TryGetValue(t.LinkedTransactionId.Value, out var catId)
                                     ? catId : null
                });
            }*/

            //eski tarih 
            /*
            var vm = new CariLedgerVm
            {
                Account = account,
                Rows = rows,
                TotalDebit = transactions.Sum(t => t.DebitAmount),
                TotalCredit = transactions.Sum(t => t.CreditAmount),
                CurrentBalance = balance
            };
            */
            /*
            // En yeni işlemden başlayarak geriye doğru bakiyeyi düşüyoruz
            foreach (var t in transactions)
            {
                rows.Add(new CariLedgerRowVm
                {
                    Transaction = t,
                    RunningBalance = runningBalance,
                    KasaCategoryId = t.LinkedTransactionId.HasValue
                                     && linkedCategories.TryGetValue(t.LinkedTransactionId.Value, out var catId)
                                     ? catId : null
                });

                // Bir sonraki (yani kronolojik olarak bir önceki) satırın bakiyesini hesaplıyoruz
                runningBalance -= (t.DebitAmount - t.CreditAmount);
            }

            //descing
            var vm = new CariLedgerVm
            {
                Account = account,
                Rows = rows,
                TotalDebit = totalDebit,
                TotalCredit = totalCredit,
                CurrentBalance = currentBalance
            };

            // Modal içindeki "İlgili Kasa" kategori listesi (aktif kategoriler)
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(vm);
            */

            //
            var vm = await BuildLedgerAsync(id, startDate, endDate);
            if (vm == null) return NotFound();

            // Kasaya bağlı hareketlerin kategori bilgisi (modalda ön seçim için)
            var linkedIds = vm.Rows
                .Where(r => r.Transaction.LinkedTransactionId.HasValue)
                .Select(r => r.Transaction.LinkedTransactionId!.Value)
                .ToList();
            var linkedCategories = linkedIds.Any()
                ? await _context.Transactions
                    .Where(x => linkedIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.CategoryId)
                : new Dictionary<int, int>();
            foreach (var r in vm.Rows)
            {
                if (r.Transaction.LinkedTransactionId.HasValue &&
                    linkedCategories.TryGetValue(r.Transaction.LinkedTransactionId.Value, out var catId))
                    r.KasaCategoryId = catId;
            }

            // Modal içindeki "İlgili Kasa" kategori listesi (aktif kategoriler)
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(vm);
        }

        /// <summary>
        /// Defter verisini hazırlar — ekran, Excel ve PDF için ORTAK kaynak
        /// (Reports'taki BuildReportAsync deseni). Tarih filtresi verilirse
        /// dönem başı bakiyesi = devir + aralıktan ÖNCEKİ tüm hareketler olur;
        /// böylece filtreli görünümde yürüyen bakiye doğru başlar.
        /// </summary>
        private async Task<CariLedgerVm?> BuildLedgerAsync(int id, DateTime? startDate, DateTime? endDate)
        {
            // Ters girilmişse takasla
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                (startDate, endDate) = (endDate, startDate);

            var account = await _context.CariAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
            if (account == null) return null;

            var baseQuery = _context.CariTransactions
                .AsNoTracking()
                .Where(t => t.CariAccountId == id);

            // Dönem başı bakiyesi: devir + başlangıçtan önceki hareketlerin neti
            decimal periodOpening = account.OpeningBalance;
            if (startDate.HasValue)
            {
                var before = baseQuery.Where(t => t.Date < startDate.Value.Date);
                periodOpening += (await before.SumAsync(t => (decimal?)t.DebitAmount) ?? 0)
                               - (await before.SumAsync(t => (decimal?)t.CreditAmount) ?? 0);
            }

            var filtered = baseQuery;
            if (startDate.HasValue) filtered = filtered.Where(t => t.Date >= startDate.Value.Date);
            if (endDate.HasValue) filtered = filtered.Where(t => t.Date <= endDate.Value.Date);

            var transactions = await filtered
                .OrderBy(t => t.Date).ThenBy(t => t.Id)
                .ToListAsync();

            var rows = new List<CariLedgerRowVm>();
            decimal balance = periodOpening;
            foreach (var t in transactions)
            {
                balance += t.DebitAmount - t.CreditAmount;
                rows.Add(new CariLedgerRowVm { Transaction = t, RunningBalance = balance });
            }

            return new CariLedgerVm
            {
                Account = account,
                Rows = rows,
                TotalDebit = transactions.Sum(t => t.DebitAmount),
                TotalCredit = transactions.Sum(t => t.CreditAmount),
                CurrentBalance = balance,
                PeriodOpeningBalance = periodOpening,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        /// <summary>Dosya adı için dönem/hesap metni: CariDefteri_Digo_20260101_20260731</summary>
        private static string BuildFileName(CariLedgerVm vm, string extension)
        {
            string safeName = string.Concat(vm.Account.Name.Split(Path.GetInvalidFileNameChars()))
                .Replace(" ", "");
            string start = vm.StartDate?.ToString("yyyyMMdd")
                ?? vm.Rows.FirstOrDefault()?.Transaction.Date.ToString("yyyyMMdd") ?? "Baslangic";
            string end = vm.EndDate?.ToString("yyyyMMdd")
                ?? vm.Rows.LastOrDefault()?.Transaction.Date.ToString("yyyyMMdd") ?? "Son";
            return $"CariDefteri_{safeName}_{start}_{end}.{extension}";
        }

        // ============ DIŞA AKTARMA ============

        // GET: /Cari/ExportExcel/5?startDate=...&endDate=...
        public async Task<IActionResult> ExportExcel(int id, DateTime? startDate, DateTime? endDate)
        {
            var vm = await BuildLedgerAsync(id, startDate, endDate);
            if (vm == null) return NotFound();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cari Defter");

            // Başlık bloğu
            ws.Cell(1, 1).Value = $"Cari Defter — {vm.Account.Name}";
            ws.Cell(1, 1).Style.Font.SetBold().Font.FontSize = 14;
            ws.Range(1, 1, 1, 7).Merge();

            string donem = (vm.StartDate.HasValue || vm.EndDate.HasValue)
                ? $"Dönem: {vm.StartDate?.ToString("dd.MM.yyyy") ?? "…"} – {vm.EndDate?.ToString("dd.MM.yyyy") ?? "…"}"
                : "Dönem: Tüm kayıtlar";
            ws.Cell(2, 1).Value = donem;
            ws.Range(2, 1, 2, 7).Merge();

            ws.Cell(3, 1).Value = vm.StartDate.HasValue ? "Dönem Başı Bakiyesi" : "Devir Bakiyesi";
            ws.Cell(3, 2).Value = vm.PeriodOpeningBalance;
            ws.Cell(3, 2).Style.NumberFormat.Format = "#,##0.00 ₺";
            ws.Cell(3, 2).Style.Font.SetBold();

            // Tablo başlıkları
            int headerRow = 5;
            string[] headers = { "Tarih", "Açıklama", "Miktar", "Birim Fiyat",
                                 "Borç (Giriş)", "Alacak (Ödeme)", "Bakiye" };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(headerRow, i + 1);
                c.Value = headers[i];
                c.Style.Font.SetBold().Font.FontColor = XLColor.White;
                c.Style.Fill.BackgroundColor = XLColor.FromHtml("#14231f");
            }

            // Satırlar
            int row = headerRow + 1;
            foreach (var r in vm.Rows)
            {
                var t = r.Transaction;
                ws.Cell(row, 1).Value = t.Date;
                ws.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";
                ws.Cell(row, 2).Value = t.Description;
                if (t.Quantity.HasValue) ws.Cell(row, 3).Value = t.Quantity.Value;
                if (t.UnitPrice.HasValue)
                {
                    ws.Cell(row, 4).Value = t.UnitPrice.Value;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                }
                if (t.DebitAmount > 0)
                {
                    ws.Cell(row, 5).Value = t.DebitAmount;
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#b3423a");
                }
                if (t.CreditAmount > 0)
                {
                    ws.Cell(row, 6).Value = t.CreditAmount;
                    ws.Cell(row, 6).Style.Font.FontColor = XLColor.FromHtml("#0e7a5f");
                }
                ws.Cell(row, 7).Value = r.RunningBalance;
                ws.Cell(row, 7).Style.Font.SetBold();
                ws.Range(row, 5, row, 7).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            // Toplam satırı
            ws.Cell(row, 2).Value = "TOPLAM";
            ws.Cell(row, 5).FormulaA1 = $"SUM(E{headerRow + 1}:E{row - 1})";
            ws.Cell(row, 6).FormulaA1 = $"SUM(F{headerRow + 1}:F{row - 1})";
            ws.Cell(row, 7).Value = vm.CurrentBalance;
            var totalRange = ws.Range(row, 1, row, 7);
            totalRange.Style.Font.SetBold();
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f7f6");
            ws.Range(row, 5, row, 7).Style.NumberFormat.Format = "#,##0.00";

            ws.SheetView.FreezeRows(headerRow);
            ws.Range(headerRow, 1, row - 1, 7).SetAutoFilter();
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                BuildFileName(vm, "xlsx"));
        }

        // GET: /Cari/ExportPdf/5?startDate=...&endDate=...
        public async Task<IActionResult> ExportPdf(int id, DateTime? startDate, DateTime? endDate)
        {
            var vm = await BuildLedgerAsync(id, startDate, endDate);
            if (vm == null) return NotFound();

            var document = new CariLedgerPdfDocument(vm);
            byte[] pdf = document.GeneratePdf();

            return File(pdf, "application/pdf", BuildFileName(vm, "pdf"));
        }


        // ============ HAREKET KAYDET (MODAL — YENİ + DÜZENLE) ============

        // POST: /Cari/SaveTransaction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTransaction(CariTransactionFormVm form)
        {
            var account = await _context.CariAccounts.FindAsync(form.CariAccountId);
            if (account == null) return NotFound();

            // --- Tutarı belirle ---
            decimal amount;
            if (form.EntryMode == "calc")
            {
                if (form.Quantity is null or <= 0 || form.UnitPrice is null or < 0)
                {
                    TempData["Error"] = "Miktar × Fiyat modunda miktar ve birim fiyat girilmelidir.";
                    return RedirectToAction(nameof(Details), new { id = form.CariAccountId });
                }
                amount = Math.Round(form.Quantity.Value * form.UnitPrice.Value, 2);
            }
            else
            {
                if (form.Amount is null or <= 0)
                {
                    TempData["Error"] = "Tutar sıfırdan büyük olmalıdır.";
                    return RedirectToAction(nameof(Details), new { id = form.CariAccountId });
                }
                amount = Math.Round(form.Amount.Value, 2);
            }

            if (string.IsNullOrWhiteSpace(form.Description))
            {
                TempData["Error"] = "Açıklama zorunludur.";
                return RedirectToAction(nameof(Details), new { id = form.CariAccountId });
            }

            bool isCredit = form.Direction == "alacak";

            // --- Kaydı oluştur / güncelle ---
            CariTransaction entity;
            if (form.Id > 0)
            {
                var found = await _context.CariTransactions.FindAsync(form.Id);
                if (found == null) return NotFound();
                entity = found;
            }
            else
            {
                entity = new CariTransaction
                {
                    CariAccountId = form.CariAccountId,
                    CreatedAt = DateTime.Now
                };
                _context.CariTransactions.Add(entity);
            }

            entity.Date = form.Date.Date;
            entity.Description = form.Description.Trim();
            entity.DocumentNo = string.IsNullOrWhiteSpace(form.DocumentNo) ? null : form.DocumentNo.Trim();
            entity.Notes = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
            entity.Quantity = form.EntryMode == "calc" ? (int?)Math.Round(form.Quantity!.Value) : null;
            entity.Unit = form.EntryMode == "calc" ? form.Unit : null;
            entity.UnitPrice = form.EntryMode == "calc" ? form.UnitPrice : null;
            entity.DebitAmount = isCredit ? 0 : amount;
            entity.CreditAmount = isCredit ? amount : 0;

            // --- Kasa (gelir-gider) bağlantısı ---
            // Dropdown'da kategori seçildiyse kasaya işlenir: Alacak → Gelir, Borç → Gider.
            // "Kasaya işleme" seçiliyse (boş) mevcut bağlantı varsa kaldırılır.
            if (form.KasaCategoryId.HasValue)
            {
                int typeId = isCredit
                    ? (int)TransactionTypeEnum.Gelir
                    : (int)TransactionTypeEnum.Gider;
                string kasaDesc = $"[Cari: {account.Name}] {entity.Description}";

                Transaction? linked = entity.LinkedTransactionId.HasValue
                    ? await _context.Transactions.FindAsync(entity.LinkedTransactionId.Value)
                    : null;

                if (linked != null)
                {
                    linked.Amount = amount;
                    linked.Date = entity.Date;
                    linked.Description = kasaDesc;
                    linked.CategoryId = form.KasaCategoryId.Value;
                    linked.TransactionTypeId = typeId;
                }
                else
                {
                    var kasa = new Transaction
                    {
                        Amount = amount,
                        Date = entity.Date,
                        Description = kasaDesc,
                        CategoryId = form.KasaCategoryId.Value,
                        TransactionTypeId = typeId,
                        CreatedAt = DateTime.Now
                    };
                    _context.Transactions.Add(kasa);
                    await _context.SaveChangesAsync(); // Id oluşsun
                    entity.LinkedTransactionId = kasa.Id;
                }
            }
            else if (entity.LinkedTransactionId.HasValue)
            {
                var linked = await _context.Transactions.FindAsync(entity.LinkedTransactionId.Value);
                if (linked != null) _context.Transactions.Remove(linked);
                entity.LinkedTransactionId = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = form.Id > 0 ? "Hareket güncellendi." : "Hareket eklendi.";
            return RedirectToAction(nameof(Details), new { id = form.CariAccountId });
        }

        // ============ TOPLU GİRİŞ ============

        // POST: /Cari/BulkSave — toplu giriş tablosundan gelen satırları tek seferde kaydeder.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkSave(int cariAccountId, List<CariBulkRowVm> rows)
        {
            var account = await _context.CariAccounts.FindAsync(cariAccountId);
            if (account == null) return NotFound();

            int added = 0, skipped = 0;
            DateTime lastDate = DateTime.Today;

            foreach (var row in rows ?? new List<CariBulkRowVm>())
            {
                if (string.IsNullOrWhiteSpace(row.Description))
                    continue; // tamamen boş satır

                // Tutar: elle girilen tutar öncelikli, yoksa miktar × birim fiyat
                decimal amount = 0;
                bool hasQtyPrice = row.Quantity is > 0 && row.UnitPrice is > 0;
                if (row.Amount is > 0)
                    amount = Math.Round(row.Amount.Value, 2);
                else if (hasQtyPrice)
                    amount = Math.Round(row.Quantity!.Value * row.UnitPrice!.Value, 2);

                if (amount <= 0) { skipped++; continue; }

                if (row.Date.HasValue) lastDate = row.Date.Value.Date;
                bool isCredit = row.Direction == "alacak";

                _context.CariTransactions.Add(new CariTransaction
                {
                    CariAccountId = cariAccountId,
                    Date = lastDate,
                    Description = row.Description.Trim(),
                    Quantity = hasQtyPrice ? (int)Math.Round(row.Quantity!.Value) : null,
                    Unit = hasQtyPrice ? "adet" : null,
                    UnitPrice = hasQtyPrice ? Math.Round(row.UnitPrice!.Value, 2) : null,
                    DebitAmount = isCredit ? 0 : amount,
                    CreditAmount = isCredit ? amount : 0,
                    CreatedAt = DateTime.Now
                });
                added++;
            }

            await _context.SaveChangesAsync();

            if (added == 0)
                TempData["Error"] = "Kaydedilecek geçerli satır bulunamadı. " +
                                    "Her satırda açıklama ve tutar (veya miktar + birim fiyat) olmalıdır.";
            else
                TempData["Success"] = $"{added} hareket kaydedildi." +
                                      (skipped > 0 ? $" {skipped} satır tutar eksik olduğu için atlandı." : "");

            return RedirectToAction(nameof(Details), new { id = cariAccountId });
        }

        // POST: /Cari/DeleteTransaction/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            var transaction = await _context.CariTransactions.FindAsync(id);
            if (transaction == null) return NotFound();

            int accountId = transaction.CariAccountId;

            // Kasaya bağlı kayıt varsa onu da sil
            if (transaction.LinkedTransactionId.HasValue)
            {
                var linked = await _context.Transactions.FindAsync(transaction.LinkedTransactionId.Value);
                if (linked != null) _context.Transactions.Remove(linked);
            }

            _context.CariTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Hareket silindi.";
            return RedirectToAction(nameof(Details), new { id = accountId });
        }
    }



    // ============ VIEW MODEL'LER ============

    public class CariAccountSummaryVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int TransactionCount { get; set; }
        public decimal Balance { get; set; }
    }

    public class CariIndexVm
    {
        public List<CariAccountSummaryVm> Accounts { get; set; } = new();
        /// <summary>Pozitif bakiyeli hesapların toplamı: müşterilerin size borcu.</summary>
        public decimal TotalReceivable { get; set; }
        /// <summary>Negatif bakiyeli hesapların toplamı (mutlak): sizin müşterilere borcunuz.</summary>
        public decimal TotalPayable { get; set; }
        public decimal Net => TotalReceivable - TotalPayable;
    }


    /// <summary>Toplu giriş tablosunun bir satırı.</summary>
    public class CariBulkRowVm
    {
        public DateTime? Date { get; set; }       // boşsa bir üst satırın tarihi kullanılır
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }      // elle tutar (miktar × fiyat yerine)
        public string? Direction { get; set; }    // "borc" | "alacak"
    }
    public class CariLedgerRowVm
    {
        public CariTransaction Transaction { get; set; } = null!;
        public decimal RunningBalance { get; set; }
        /// <summary>Kasaya bağlıysa bağlı kaydın kategori Id'si (modal ön seçimi için).</summary>
        public int? KasaCategoryId { get; set; }
    }

    public class CariLedgerVm
    {
        public CariAccount Account { get; set; } = null!;
        public List<CariLedgerRowVm> Rows { get; set; } = new();
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal CurrentBalance { get; set; }
        /// <summary>Devir + tarih filtresinden önceki hareketlerin neti (filtre yoksa = devir).</summary>
        public decimal PeriodOpeningBalance { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsFiltered => StartDate.HasValue || EndDate.HasValue;
    }

    /// <summary>Modal formundan gelen veri.</summary>
    public class CariTransactionFormVm
    {
        public int Id { get; set; }                    // 0 = yeni kayıt
        public int CariAccountId { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string? DocumentNo { get; set; }
        public string Description { get; set; } = string.Empty;
        public string EntryMode { get; set; } = "calc"; // "calc" | "direct"
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Amount { get; set; }            // direct modda tutar
        public string Direction { get; set; } = "borc"; // "borc" | "alacak"
        public int? KasaCategoryId { get; set; }        // null = kasaya işlenmez
        public string? Notes { get; set; }
    }
    public enum TransactionTypeEnum { Gelir = 1, Gider = 2 }
}
