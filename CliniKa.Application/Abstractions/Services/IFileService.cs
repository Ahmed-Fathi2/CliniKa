using CliniKa.Application.DTOs.FormFile;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IFileService
    {
        Task<GeneralResult<string>> UploadFileAsync(UploadFileRequest uploadFile, string baseUrl, string? schema, string? host);
        bool DeleteFile(string? fileUrl, string baseUrl);
    }
}
