using System.ComponentModel.DataAnnotations;

namespace ImageUploader.Models
{
    public class Photo
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [MaxLength(2048, ErrorMessage = "{0} is more than 2048 characters.")]
        public string Url { get; set; }
    }
}