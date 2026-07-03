using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;
using CliniKa.Domain.Repositories;

namespace CliniKa.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _env;
        private readonly IUnitOfWork _unitOfWork;

        public FileController(IFileService fileService, IWebHostEnvironment env, IUnitOfWork unitOfWork)
        {
            _fileService = fileService;
            _env = env;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request)
        {
            var schema = Request.Scheme;
            var host = Request.Host.Value;
            var baseUrl = _env.WebRootPath;

            var result = await _fileService.UploadFileAsync(request, baseUrl, schema, host);

            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
