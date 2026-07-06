using System.IO;
using System.Threading.Tasks;
using CliniKa.Application.DTOs;

namespace CliniKa.Application.Abstraction
{
    public interface ILangFlowService
    {
        Task<GeneralResult<LangFlowAudioTranscriptResult>> ProcessAudioAsync(Stream audioStream, string fileName);
    }
}
