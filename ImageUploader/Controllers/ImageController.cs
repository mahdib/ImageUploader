using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ImageUploader.Controllers.DTO;
using ImageUploader.Data;
using ImageUploader.Helpers;
using ImageUploader.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ImageUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ImageController : ControllerBase
    {
        private readonly IOptions<ImageSettings> _imageSettings;
        private readonly IOptions<FtpServerSettings> _ftpServerSettings;
        private readonly IOptions<FoldersSettings> _foldersSettings;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;

        public ImageController(IOptions<ImageSettings> imageSettings, IOptions<FtpServerSettings> ftpServerSettings,
        ApplicationDbContext dbContext, IWebHostEnvironment environment, IOptions<FoldersSettings> foldersSettings)
        {
            _imageSettings = imageSettings;
            _ftpServerSettings = ftpServerSettings;
            _dbContext = dbContext;
            _environment = environment;
            _foldersSettings = foldersSettings;
        }

        [HttpGet("{filter?}")]
        public async Task<IActionResult> GetAsync(string filter = null)
        {
            List<Photo> images;

            if (string.IsNullOrEmpty(filter))
            {
                images = await _dbContext.Photos
                                        .ToListAsync()
                                        .ConfigureAwait(false);
            }
            else
            {
                images = await _dbContext.Photos
                                        .Where(p => p.Url.EndsWith(filter))
                                        .ToListAsync()
                                        .ConfigureAwait(false);
            }

            return Ok(images);
        }

        [HttpGet("{id:int}", Name = "GetImage")]
        public async Task<IActionResult> GetAsync(int id)
        {
            if (id == 0) return BadRequest();

            var entity = await _dbContext.Photos
            .FirstOrDefaultAsync(p => p.Id.Equals(id))
            .ConfigureAwait(false);

            if (entity is null) return NotFound();

            var returnDto = new ReturnDto
            {
                Title = entity.Title,
                Url = entity.Url
            };

            return Ok(returnDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostAsync([FromForm] UploadDto model)
        {
            if (!ModelState.IsValid) return BadRequest();

            if (model.File is null) return BadRequest("Image file cannot be empty.");

            var file = model.File;

            if (!CheckFileSize(file.Length))
                return BadRequest($"The file is more than {_imageSettings.Value.MaxSize} byte or is less than {_imageSettings.Value.MinSize} byte.");

            var entity = new Photo();
            var extension = Path.GetExtension(file.FileName);
            var fileName = new Guid() + extension;

            try
            {
                if (model.IsFtp)
                {
                    var uri = _ftpServerSettings.Value?.Uri;
                    var username = _ftpServerSettings.Value?.UserName;
                    var password = _ftpServerSettings.Value?.Password;

                    var request = (FtpWebRequest)WebRequest.Create(uri + fileName);
                    request.Credentials = new NetworkCredential(username, password);
                    request.Method = WebRequestMethods.Ftp.UploadFile;

                    using var stream = request.GetRequestStream();
                    await file.CopyToAsync(stream).ConfigureAwait(false);

                    entity.Title = model.Title;
                    entity.Url = uri + fileName;
                }
                else
                {
                    var folderName = _foldersSettings.Value.ImageFolderName;
                    var path = Path.Combine(_environment.WebRootPath, folderName);
                    var fullPath = Path.Combine(path, fileName);
                    var fileUrl = Path.Combine(folderName, fileName);

                    if (!Directory.Exists(path)) Directory.CreateDirectory(folderName);

                    await using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream).ConfigureAwait(false);
                    entity.Title = model.Title;
                    entity.Url = fileUrl;
                }

                await _dbContext.Photos.AddAsync(entity).ConfigureAwait(false);

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                return CreatedAtRoute("GetImage", new { id = entity.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private bool CheckFileSize(long length) => length < _imageSettings.Value?.MaxSize && length > _imageSettings.Value?.MinSize;
    }
}
