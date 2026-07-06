using System.IO;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.Langflow;

namespace CliniKa.Application.Abstractions.Services
{
    public interface ILangFlowService
    {
        Task<GeneralResult<LangFlowAudioTranscriptResult>> ProcessAudioAsync(Stream audioStream, string fileName);
    }
}
