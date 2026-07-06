using System.IO;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.Sessions;

namespace CliniKa.Application.Abstractions.Services
{
    public interface ISessionService
    {
        Task<GeneralResult<TranscribeSessionResponse>> TranscribeSessionAudioAsync(int sessionId, Stream audioStream, string fileName);
    }
}
