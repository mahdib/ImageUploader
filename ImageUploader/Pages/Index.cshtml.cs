using System;
using ImageUploader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ImageUploader.Helpers;
using Microsoft.Extensions.Options;

namespace ImageUploader.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IOptions<ApiSettings> _apiSettings;

        public IndexModel(IOptions<ApiSettings> apiSettings)
        {
            _apiSettings = apiSettings;
        }

        [TempData] public string Message { get; set; } = string.Empty;

        [BindProperty] public IList<Photo> PhotoList { get; set; }

        public string BaseUrl { get; set; } = "/api/Image";

        public async Task OnGetAsync(string filter = null)
        {
            PhotoList = new List<Photo>();
            using var httpClient = new HttpClient();
            using var responseMessage = await httpClient.GetAsync(_apiSettings.Value?.Uri + BaseUrl).ConfigureAwait(false);
            if (responseMessage.IsSuccessStatusCode)
            {
                var response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                PhotoList = JsonConvert.DeserializeObject<List<Photo>>(response);
            }
            else
            {
                Message = responseMessage.RequestMessage.ToString();
            }
        }
    }
}