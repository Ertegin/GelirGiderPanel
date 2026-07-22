using GelirGiderPanel.Data;
using GelirGiderPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GelirGiderPanel.Controllers
{
    /// <summary>
    /// Kategori CRUD işlemleri: Listeleme, Ekleme, Düzenleme, Silme.
    /// </summary>
    /// 
    //GuvenPolicy
    [Authorize(Policy = "GuvenPolicy")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Transactions)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: /Categories/Create
        public IActionResult Create() => View();

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsActive")] Category category)
        {
            // Aynı isimde kategori var mı kontrolü
            bool exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == category.Name.ToLower());

            if (exists)
                ModelState.AddModelError(nameof(Category.Name), "Bu isimde bir kategori zaten mevcut.");

            if (!ModelState.IsValid)
                return View(category);

            category.CreatedAt = DateTime.Now;
            _context.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{category.Name}\" kategorisi eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,IsActive,CreatedAt")] Category category)
        {
            if (id != category.Id) return NotFound();

            bool exists = await _context.Categories
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == category.Name.ToLower());

            if (exists)
                ModelState.AddModelError(nameof(Category.Name), "Bu isimde başka bir kategori zaten mevcut.");

            if (!ModelState.IsValid)
                return View(category);

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categories.AnyAsync(c => c.Id == id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = $"\"{category.Name}\" kategorisi güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // İşlem kaydı olan kategori silinemez (veri bütünlüğü)
            if (category.Transactions.Any())
            {
                TempData["Error"] = $"\"{category.Name}\" kategorisine ait {category.Transactions.Count} işlem var. Önce işlemleri silin veya kategoriyi pasife alın.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{category.Name}\" kategorisi silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
