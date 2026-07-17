using System.Collections.Generic;
using System.Threading.Tasks;

namespace CliniKa.Application.Abstractions.Services
{
    /// <summary>
    /// Service Interface responsible for communicating with Activepieces webhook/flow.
    /// </summary>
    public interface ILangFlowService
    {
        /// <summary>
        /// Sends the audio and medical documents to the diagnosis flow trigger endpoint asynchronously.
        /// </summary>
        Task<GeneralResult<string>> TriggerDiagnosisFlowAsync(
            int sessionId,
            string audioFilePath,
            List<string> documentFilePaths);
    }
}
