using System.Threading;
using System.Threading.Tasks;

namespace CliniKa.Application.Abstractions.Services
{
    /// <summary>
    /// Provides a mechanism for real-time notification when a diagnosis callback is received.
    /// Used by SSE (Server-Sent Events) to push updates to frontend clients.
    /// </summary>
    public interface IDiagnosisNotificationService
    {
        /// <summary>
        /// Waits asynchronously until a diagnosis-complete notification is published
        /// for the specified session, or until the cancellation token is triggered
        /// (e.g. the client disconnects).
        /// </summary>
        /// <param name="sessionId">The clinical session ID to listen for.</param>
        /// <param name="cancellationToken">
        /// Token that fires when the client disconnects or the request is aborted.
        /// </param>
        /// <returns>
        /// GeneralResult.Success if the notification was received.
        /// GeneralResult.Failure if the client disconnected before the callback arrived.
        /// </returns>
        Task<GeneralResult> WaitForNotificationAsync(int sessionId, CancellationToken cancellationToken);

        /// <summary>
        /// Signals that the diagnosis callback has been received and processed
        /// for the specified session. All waiting SSE listeners will be notified.
        /// </summary>
        /// <param name="sessionId">The clinical session ID whose diagnosis is complete.</param>
        void NotifyDiagnosisComplete(int sessionId);
    }
}
