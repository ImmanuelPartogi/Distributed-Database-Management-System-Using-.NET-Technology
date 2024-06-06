using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ModelClasses
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [Range(1, 999, ErrorMessage = "Range 1 to 999.99 only")]
        [RegularExpression(@"^[0-9]+(\.[0-9]{1,2})$", ErrorMessage = "Please insert two digits after decimal. Example: 0.00")]
        public double Price { get; set; }

        [Required]
        [MaxLength(2000, ErrorMessage = "Length can not exist more than 30 characters")]
        public string Description { get; set; }

        public ICollection<PImages>? ImgUrls { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? category { get; set; }

        public string? HomeImgUrl { get; set; }
    }
}
