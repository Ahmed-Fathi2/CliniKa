using Microsoft.AspNetCore.Http;

namespace CliniKa.Application.DTOs
{
    public sealed record UploadFileRequest(IFormFile File);
}
