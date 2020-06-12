using ImageUploader.Controllers.DTO;
using ImageUploader.Data;
using ImageUploader.Helpers;
using ImageUploader.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.FileProviders;

namespace ImageUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Produces("application/json")]
    public class ImageController : ControllerBase
    {
        private readonly IOptions<ImageSettings> _imageSettings;
        private readonly IOptions<FtpServerSettings> _ftpServerSettings;
        private readonly IOptions<FoldersSettings> _foldersSettings;
        private readonly IFileProvider _fileProvider;
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
            _fileProvider = environment.WebRootFileProvider;
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
                Id = entity.Id,
                Url = entity.Url
            };

            return Ok(returnDto);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is null) return new UnsupportedMediaTypeResult();

            if (!CheckFileSize(file.Length))
                return BadRequest($"The file is more than {_imageSettings.Value.MaxSize} byte or is less than {_imageSettings.Value.MinSize} byte.");

            try
            {
                var folderName = _foldersSettings.Value.ImageFolderName;
                var extension = Path.GetExtension(file.FileName);
                var fileName = Guid.NewGuid() + extension;
                var path = Path.Combine(_environment.WebRootPath, folderName);
                var fullPath = Path.Combine(path, fileName);

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream).ConfigureAwait(false);

                var fileUrl = Path.Combine(folderName, fileName);
                var entity = new Photo { Url = fileUrl };

                await _dbContext.Photos.AddAsync(entity).ConfigureAwait(false);

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                GenerateThumbnail(fullPath);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("ftp-upload")]
        public async Task<IActionResult> FtpUpload(IFormFile file)
        {
            if (file is null) return new UnsupportedMediaTypeResult();

            if (!CheckFileSize(file.Length))
                return BadRequest($"The file is more than {_imageSettings.Value.MaxSize / 1000} Kb or is less than {_imageSettings.Value.MinSize / 1000} Kb.");

            var uri = _ftpServerSettings.Value?.Uri;
            var username = _ftpServerSettings.Value?.UserName;
            var password = _ftpServerSettings.Value?.Password;
            var folderName = _ftpServerSettings.Value?.FolderName;

            var extension = Path.GetExtension(file.FileName);
            var fileName = Guid.NewGuid() + extension;

            var ftpPath = Path.Combine(uri, folderName, fileName);

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.Credentials = new NetworkCredential(username, password);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                using var stream = request.GetRequestStream();
                await file.CopyToAsync(stream).ConfigureAwait(false);

                var entity = new Photo
                {
                    Url = uri + fileName
                };

                await _dbContext.Photos.AddAsync(entity).ConfigureAwait(false);

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private void GenerateThumbnail(string fullPath)
        {
            var width = _imageSettings.Value?.ThumbWidth;
            var height = _imageSettings.Value?.ThumbHeight;

            var imagePath = PathString.FromUriComponent(fullPath);
            var fileInfo = _fileProvider.GetFileInfo(imagePath);

            var outputStream = new MemoryStream();
            using var inputStream = fileInfo.CreateReadStream();
            using var image = Image.Load(inputStream);
            var size = new Size(width.Value, height.Value);
            image.Mutate(i => i.Resize(size));
            if (Path.GetExtension(fileInfo.Name).Equals("jpg"))
                image.SaveAsJpeg(outputStream);
            else
                image.SaveAsPng(outputStream);

            outputStream.Seek(0, SeekOrigin.Begin);
        }

        private bool CheckFileSize(long length) => length < _imageSettings.Value?.MaxSize && length > _imageSettings.Value?.MinSize;
    }
}
