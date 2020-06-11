using Microsoft.AspNetCore.Http;

namespace ImageUploader.Controllers.DTO
{
    public class UploadDto
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public IFormFile File { get; set; }
        public bool IsFtp { get; set; }
    }
}