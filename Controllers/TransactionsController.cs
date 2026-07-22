using GelirGiderPanel.Data;
using GelirGiderPanel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Controllers
{
    /// <summary>
    /// Gelir / Gider işlemleri CRUD.
    /// </summary>
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;

        public TransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Transactions
        // typeId parametresi ile filtreleme: 1 = sadece Gelir, 2 = sadece Gider
        public async Task<IActionResult> Index(int? typeId)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.TransactionType)
                .AsQueryable();

            if (typeId.HasValue)
                query = query.Where(t => t.TransactionTypeId == typeId.Value);

            ViewBag.SelectedTypeId = typeId;

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            return View(transactions);
        }

        // GET: /Transactions/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Transaction { Date = DateTime.Today });
        }

        // POST: /Transactions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Amount,Description,Date,CategoryId,TransactionTypeId")] Transaction transaction)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(transaction);
            }

            transaction.CreatedAt = DateTime.Now;
            _context.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "İşlem başarıyla kaydedildi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();

            await LoadDropdownsAsync();
            return View(transaction);
        }

        // POST: /Transactions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Amount,Description,Date,CategoryId,TransactionTypeId,CreatedAt")] Transaction transaction)
        {
            if (id != transaction.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(transaction);
            }

            try
            {
                _context.Update(transaction);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Transactions.AnyAsync(t => t.Id == id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = "İşlem güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Transactions/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            TempData["Success"] = "İşlem silindi.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Create/Edit formlarındaki dropdown'lar için verileri hazırlar.
        /// </summary>
        private async Task LoadDropdownsAsync()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                nameof(Category.Id),
                nameof(Category.Name));

            ViewBag.TransactionTypes = new SelectList(
                await _context.TransactionTypes.ToListAsync(),
                nameof(TransactionType.Id),
                nameof(TransactionType.Name));
        }
    }
}
