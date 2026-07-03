using CliniKa.Application.DTOs;

namespace CliniKa.Application.Abstraction
{
    public interface IFileService
    {
        Task<GeneralResult<string>> UploadFileAsync(UploadFileRequest uploadFile, string baseUrl, string? schema, string? host);
        bool DeleteFile(string? fileUrl, string baseUrl);
    }
}
