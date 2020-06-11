using Microsoft.AspNetCore.Http;

namespace ImageUploader.Controllers.DTO
{
    public class ReturnDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}