using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва книги обов'язкова")]
        [DisplayName("Назва книги")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Автор обов'язковий")]
        [DisplayName("Автор")]
        public string Author { get; set; }

        [Range(1000, 2026, ErrorMessage = "Рік має бути від 1000 до 2026")]
        [DisplayName("Рік видання")]
        public int Year { get; set; }

        // --- НОВІ ПОЛЯ ДЛЯ ТРЕКІНГУ ---

        [DisplayName("Всього сторінок")]
        [Range(1, 5000, ErrorMessage = "Сторінок має бути від 1 до 5000")]
        public int TotalPages { get; set; } = 100; // Дефолтне значення

        [DisplayName("Прочитано сторінок")]
        public int ReadPages { get; set; } = 0;

        // Статус: "Читаю", "Завершено", "Хочу прочитати"
        [DisplayName("Статус")]
        public string Status { get; set; } = "Хочу прочитати";

        [DisplayName("Дата завершення")]
        public DateTime? FinishDate { get; set; } // Може бути порожнім

        // Зв'язок з категорією (залишаємо як було)
        [DisplayName("Жанр")]
        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        
        // --- ЧИТАНА ВЛАСТИВІСТЬ (PROGRESS) ---
        [NotMapped] // Це поле не створюється в БД
        public int ProgressPercent
        {
            get 
            {
                if (TotalPages == 0) return 0;
                // Рахуємо відсоток і заокруглюємо
                var percent = (double)ReadPages / TotalPages * 100;
                return (int)Math.Min(100, percent); // Не більше 100%
            }
        }

        public string? ImageUrl { get; set; }

        [DisplayName("Анотація/Опис")]
        public string? Description { get; set; } // Опис книги (необов'язковий)

        [System.ComponentModel.DataAnnotations.Range(0, 5, ErrorMessage = "Оцінка має бути від 1 до 5")]
        [System.ComponentModel.DisplayName("Оцінка (1-5)")]
        public int Rating { get; set; } = 0; // 0 означає, що ще не оцінено

        // --- ПРИВ'ЯЗКА ДО КОРИСТУВАЧА ---
        public string? ApplicationUserId { get; set; }

        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("ApplicationUserId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? ApplicationUser { get; set; }

        // --- ЗВ'ЯЗОК З НОТАТКАМИ ---
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public List<Note> Notes { get; set; } = new List<Note>();
    }
}