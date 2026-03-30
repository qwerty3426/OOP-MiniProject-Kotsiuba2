using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using shelf.Models;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace shelf.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BooksController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ОНОВЛЕНО: додано параметр для пошуку
        public IActionResult Index(string searchString, int? categoryId)
        {
            // МАГІЯ: Якщо жанрів в базі 0, створюємо стандартні автоматично!
            if (!_db.Categories.Any())
            {
                _db.Categories.AddRange(
                    new Category { Name = "Фентезі", DisplayOrder = 1 },
                    new Category { Name = "Бізнес та Саморозвиток", DisplayOrder = 2 },
                    new Category { Name = "Детектив / Трилер", DisplayOrder = 3 },
                    new Category { Name = "IT та Програмування", DisplayOrder = 4 },
                    new Category { Name = "Класика", DisplayOrder = 5 }
                );
                _db.SaveChanges();
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var books = _db.Books.Include(u => u.Category)
                                 .Where(u => u.ApplicationUserId == userId)
                                 .ToList();

            // 1. Фільтр по ПОШУКУ
            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(s => s.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase) 
                                      || s.Author.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 2. Фільтр по ЖАНРУ (Категорії)
            if (categoryId != null && categoryId > 0)
            {
                books = books.Where(b => b.CategoryId == categoryId).ToList();
            }

            var sortedBooks = books.OrderBy(b => b.Status == "Читаю" ? 1 : b.Status == "Хочу прочитати" ? 2 : 3)
                                   .ThenByDescending(b => b.Id).ToList();

            // Передаємо список всіх жанрів у View для кнопок-фільтрів
            ViewBag.CategoryList = _db.Categories.OrderBy(c => c.DisplayOrder).ToList();
            ViewBag.CurrentCategoryId = categoryId; // Щоб підсвітити активну кнопку

            ViewBag.Total = books.Count;
            ViewBag.Reading = books.Count(b => b.Status == "Читаю");
            ViewBag.Finished = books.Count(b => b.Status == "Завершено");

            return View(sortedBooks);
        }

        public IActionResult Details(int? id)
        {
            if (id == null || id == 0) return NotFound();

            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Завантажуємо книгу + Категорію + Нотатки (сортуємо новіші зверху)
            var bookFromDb = _db.Books
                .Include(u => u.Category)
                .Include(u => u.Notes.OrderByDescending(n => n.CreatedAt))
                .FirstOrDefault(u => u.Id == id && u.ApplicationUserId == userId);
            
            if (bookFromDb == null) return NotFound();

            return View(bookFromDb);
        }

        [HttpPost]
        public IActionResult AddNote(int bookId, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var note = new Note 
                { 
                    BookId = bookId, 
                    Text = text,
                    CreatedAt = DateTime.Now
                };
                _db.Notes.Add(note);
                _db.SaveChanges();
                TempData["success"] = "Нотатку успішно додано!";
            }
            return RedirectToAction("Details", new { id = bookId });
        }

        [HttpPost]
        public IActionResult QuickUpdate(int id, int pagesReadToday)
        {
            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Знаходимо книгу, перевіряючи власника
            var bookFromDb = _db.Books.FirstOrDefault(u => u.Id == id && u.ApplicationUserId == userId);
            if (bookFromDb == null) return NotFound();

            // Оновлюємо логіку трекінгу
            bookFromDb.ReadPages += pagesReadToday;

            // Якщо ми почали читати, змінюємо статус з "Хочу" на "Читаю"
            if (bookFromDb.Status == "Хочу прочитати" && pagesReadToday > 0)
            {
                bookFromDb.Status = "Читаю";
            }

            // Якщо прочитали все, ставимо статус "Завершено" і дату
            if (bookFromDb.ReadPages >= bookFromDb.TotalPages)
            {
                bookFromDb.ReadPages = bookFromDb.TotalPages; // щоб не було більше 100%
                bookFromDb.Status = "Завершено";
                bookFromDb.FinishDate = DateTime.Now;
            }

            // Зберігаємо зміни
            _db.Books.Update(bookFromDb);
            _db.SaveChanges();

            TempData["success"] = $"Прогрес оновлено! +{pagesReadToday} стор.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string newStatus)
        {
            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var bookFromDb = _db.Books.FirstOrDefault(u => u.Id == id && u.ApplicationUserId == userId);
            if (bookFromDb == null) return NotFound();

            bookFromDb.Status = newStatus;

            // Якщо маркаємо як завершену, то виставляємо 100% прогрес
            if (newStatus == "Завершено")
            {
                bookFromDb.ReadPages = bookFromDb.TotalPages;
                bookFromDb.FinishDate = DateTime.Now;
            }

            _db.Books.Update(bookFromDb);
            _db.SaveChanges();

            TempData["success"] = $"Статус змінено на: {newStatus}";
            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            ViewBag.CategoryList = new SelectList(_db.Categories.ToList(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult Create(Book obj, IFormFile? file)
        {
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                // --- ЗАПИСУЄМО ID ЮЗЕРА ---
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                obj.ApplicationUserId = userId;

                string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\books");

                    if (!Directory.Exists(productPath))
                        Directory.CreateDirectory(productPath);

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.ImageUrl = @"\images\books\" + fileName;
                }

                _db.Books.Add(obj);
                _db.SaveChanges();
                TempData["success"] = "Книгу додано з обкладинкою!";
                return RedirectToAction("Index");
            }
            ViewBag.CategoryList = new SelectList(_db.Categories.ToList(), "Id", "Name");
            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var bookFromDb = _db.Books.FirstOrDefault(u => u.Id == id && u.ApplicationUserId == userId);
            if (bookFromDb == null) return NotFound();
            
            // Передаємо список жанрів у форму
            ViewBag.CategoryList = new SelectList(_db.Categories.ToList(), "Id", "Name");
            return View(bookFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Book obj, IFormFile? file)
        {
            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Перевіряємо, чи книга належить цьому юзеру
            var bookFromDb = _db.Books.FirstOrDefault(u => u.Id == obj.Id && u.ApplicationUserId == userId);
            if (bookFromDb == null) return NotFound();

            // ДОДАЙ ЦІ ДВА РЯДКИ:
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\books");

                    // Видаляємо стару картинку, якщо вона була
                    if (!string.IsNullOrEmpty(obj.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, obj.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.ImageUrl = @"\images\books\" + fileName;
                }

                _db.Books.Update(obj);
                _db.SaveChanges();
                TempData["success"] = "Книгу успішно оновлено!";
                return RedirectToAction("Index");
            }
            ViewBag.CategoryList = new SelectList(_db.Categories.ToList(), "Id", "Name");
            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var bookFromDb = _db.Books.FirstOrDefault(u => u.Id == id && u.ApplicationUserId == userId);
            if (bookFromDb == null) return NotFound();
            return View(bookFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            // Дістаємо ID поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            var obj = _db.Books.FirstOrDefault(u => u.Id == id && u.ApplicationUserId == userId);
            if (obj == null) return NotFound();

            // Видаляємо файл картинки, якщо він існує
            if (!string.IsNullOrEmpty(obj.ImageUrl))
            {
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", obj.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            _db.Books.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Книгу та обкладинку видалено!";
            return RedirectToAction("Index");
        }
    }
}