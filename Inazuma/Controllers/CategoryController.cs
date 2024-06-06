using Microsoft.AspNetCore.Mvc;
using DatabaseAccess;
using ModelClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Inazuma.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var items = _context.categories.ToList();
            return View(items);
        }
        [Authorize]
        public IActionResult Upsert(int? id)
        {
            if (id == 0)
            {
                Category category = new Category();
                return View(category);

            }
            else
            {
                var items = _context.categories.FirstOrDefault(u => u.Id == id);
                return View(items);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(int? id, Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (id == null)
                    {
                        var foundItem = await _context.categories.FirstOrDefaultAsync(u => u.Name == category.Name);
                        if (foundItem != null)
                        {
                            TempData["AlertMessage"] = category.Name + " already exists in the list. It was not added.";
                            return RedirectToAction("Index");
                        }

                        await _context.categories.AddAsync(category);
                        TempData["AlertMessage"] = category.Name + " has been added to the category list.";
                    }
                    else
                    {
                        var existingItem = await _context.categories.FindAsync(id);
                        if (existingItem == null)
                        {
                            return NotFound();
                        }

                        existingItem.Name = category.Name;
                        TempData["AlertMessage"] = category.Name + " has been updated in the category list.";
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "An error occurred while processing your request. Please try again later.";
                    // Log the exception here if necessary
                    return RedirectToAction("Index");
                }
            }

            return View(category); // Return the view with validation errors
        }

        public IActionResult Delete(int id)
        {
                var items = _context.categories.FirstOrDefault(u => u.Id == id);
                return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Category category)
        {
            var itemToDelete = await _context.categories.FindAsync(category.Id);
            if (itemToDelete == null)
            {
                return NotFound();
            }

            _context.categories.Remove(itemToDelete);
            await _context.SaveChangesAsync();

            TempData["AlertMessage"] = itemToDelete.Name + " has been deleted from the category list";
            return RedirectToAction(nameof(Index));
        }


    }
}
