using Microsoft.AspNetCore.Http;

namespace CliniKa.Application.DTOs.FormFile
{
    public sealed record UploadFileRequest(IFormFile File);
}
