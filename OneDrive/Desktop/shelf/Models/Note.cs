using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace shelf.Models
{
    public class Note
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст нотатки не може бути порожнім")]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Зв'язок з конкретною книгою (Foreign Key)
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }
    }
}