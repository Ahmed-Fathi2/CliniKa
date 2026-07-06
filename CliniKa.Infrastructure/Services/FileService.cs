using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.FormFile;

namespace CliniKa.Infrastructure.Services
{
    public class FileService : IFileService
    {
        public async Task<GeneralResult<string>> UploadFileAsync(UploadFileRequest uploadFile, string baseUrl, string? schema, string? host)
        {
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(schema))
                return GeneralResult<string>.Failure("Schema or Host not found.");

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var file = uploadFile.File;
            var extension = Path.GetExtension(file.FileName).ToLower();
            var fileName = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_").ToLower();
            var actualFileName = $"{fileName}-{Guid.NewGuid()}{extension}";

            var directory = Path.Combine(baseUrl, "uploads");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, actualFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"{schema}://{host}/uploads/{actualFileName}";

            return GeneralResult<string>.Success(url, "File uploaded successfully.");
        }

        public bool DeleteFile(string? fileUrl, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return false;

            try
            {
                var uri = new Uri(fileUrl);
                var fileName = Path.GetFileName(uri.LocalPath);
                var filePath = Path.Combine(baseUrl, "uploads", fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch
            {
                // Log error or handle if needed
            }

            return false;
        }
    }
}
