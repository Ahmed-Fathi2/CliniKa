using System.IO;
using System.Threading.Tasks;
using CliniKa.Application.DTOs;

namespace CliniKa.Application.Abstraction
{
    public interface ISessionService
    {
        Task<GeneralResult<TranscribeSessionResponse>> TranscribeSessionAudioAsync(int sessionId, Stream audioStream, string fileName);
    }
}
