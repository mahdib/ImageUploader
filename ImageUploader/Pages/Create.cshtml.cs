using ImageUploader.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImageUploader.Pages
{
    public class CreateModel : PageModel
    {
        private readonly IOptions<FtpServerSettings> _ftpServerSettings;
        private readonly IOptions<ApiSettings> _apiSettings;
        private readonly IOptions<ImageSettings> _imageSettings;

        public CreateModel(IOptions<FtpServerSettings> ftpServerSettings,
        IOptions<ImageSettings> imageSettings, IOptions<ApiSettings> apiSettings)
        {
            _ftpServerSettings = ftpServerSettings;
            _imageSettings = imageSettings;
            _apiSettings = apiSettings;
        }

        [TempData]
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public bool IsFtp { get; set; }

        public void OnGet()
        {
            ViewData["ServerAddress"] = _ftpServerSettings.Value?.Uri;
            ViewData["FolderName"] = _ftpServerSettings.Value?.FolderName;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            using var httpClient = new HttpClient();
            using var form = new MultipartFormDataContent();

            if (Request.Form.Files.Count == 0)
            {
                Message = "Please select an image to uplaod";
                return Page();
            }

            var file = Request.Form.Files[0];

            var apiUrl = string.Empty;

            if (IsFtp is true)
            {
                apiUrl = _apiSettings.Value?.Uri + "/api/image/ftp-upload";
            }
            else
            {
                apiUrl = _apiSettings.Value?.Uri + "/api/image/upload";
            }

            using var fileStream = file.OpenReadStream();
            form.Add(new StreamContent(fileStream), "file", file.FileName);

            using var response = await httpClient.PostAsync(apiUrl, form).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Message = response.Content.ReadAsStringAsync().Result;
                ViewData["ServerAddress"] = _ftpServerSettings.Value?.Uri;
                ViewData["FolderName"] = _ftpServerSettings.Value?.FolderName;
                return Page();
            }
            var apiResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return RedirectToPage("./Index");
        }
    }
}