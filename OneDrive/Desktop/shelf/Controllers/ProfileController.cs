using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using shelf.Models; // ТУТ МАЄ БУТИ ТВІЙ NAMESPACE БАЗИ (може бути інший, перевір як у BooksController)
using System.Linq;

namespace shelf.Controllers // Заміни ShelfApp на назву свого проекту
{
    [Authorize] // Тільки для залогінених
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProfileController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // Дістаємо поточного юзера
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Беремо всі ЙОГО книги
            var myBooks = _db.Books.Where(b => b.ApplicationUserId == userId).ToList();

            // РАХУЄМО ЖОРСТКУ СТАТИСТИКУ
            ViewBag.TotalBooks = myBooks.Count;
            ViewBag.FinishedBooks = myBooks.Count(b => b.Status == "Завершено");

            // Сума ВСІХ прочитаних сторінок
            ViewBag.TotalPagesRead = myBooks.Sum(b => b.ReadPages);

            // Вираховуємо середню оцінку (тільки для тих, де оцінка > 0)
            var ratedBooks = myBooks.Where(b => b.Rating > 0).ToList();
            if (ratedBooks.Any())
            {
                ViewBag.AvgRating = Math.Round(ratedBooks.Average(b => b.Rating), 1);
            }
            else
            {
                ViewBag.AvgRating = 0;
            }

            return View();
        }
    }
}