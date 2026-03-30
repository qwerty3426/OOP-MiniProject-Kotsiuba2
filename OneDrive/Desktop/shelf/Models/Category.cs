using System.ComponentModel.DataAnnotations;

namespace shelf.Models
{
    public partial class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва жанру обов'язкова")]
        public string Name { get; set; }

        [Range(1, 100, ErrorMessage = "Порядок відображення має бути від 1 до 100")]
        public int DisplayOrder { get; set; } = 1;
    }
}