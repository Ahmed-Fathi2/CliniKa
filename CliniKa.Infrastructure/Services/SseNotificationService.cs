using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CliniKa.Application;
using CliniKa.Application.Abstractions.Services;

namespace CliniKa.Infrastructure.Services
{
    /// <summary>
    /// Singleton service that coordinates SSE notifications between the diagnosis callback
    /// and waiting frontend clients using System.Threading.Channels.
    /// 
    /// How it works:
    /// - When a client connects to the SSE endpoint, WaitForNotificationAsync creates
    ///   a bounded Channel for that sessionId and awaits a read.
    /// - When the callback arrives and saves successfully, NotifyDiagnosisComplete writes
    ///   to that channel, unblocking the waiting SSE endpoint.
    /// - The channel is then removed from the dictionary to free memory.
    /// 
    /// Thread Safety:
    /// - ConcurrentDictionary ensures safe concurrent access from multiple threads.
    /// - Channel<bool> is inherently thread-safe for producer-consumer scenarios.
    /// </summary>
    public class SseNotificationService : IDiagnosisNotificationService
    {
        // Each session gets its own channel. The channel acts as a "signal" —
        // the SSE endpoint reads from it, the callback writes to it.
        private readonly ConcurrentDictionary<int, Channel<bool>> _channels = new();

        /// <inheritdoc />
        public async Task<GeneralResult> WaitForNotificationAsync(int sessionId, CancellationToken cancellationToken)
        {
            // GetOrAdd is atomic — if two SSE clients connect for the same session,
            // they share the same channel (both will be notified).
            var channel = _channels.GetOrAdd(sessionId, _ =>
                Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
                {
                    // If the channel is full (notification already sent), drop the oldest.
                    // This handles the edge case where the callback arrives BEFORE the SSE connects.
                    FullMode = BoundedChannelFullMode.DropOldest
                }));

            // Check if the client has already disconnected before we even start waiting
            if (cancellationToken.IsCancellationRequested)
            {
                _channels.TryRemove(sessionId, out _);
                return GeneralResult.Failure("Client disconnected before waiting started.");
            }

            // Use WaitToReadAsync which returns false when the channel is completed
            // and the cancellation token overload returns false on cancellation
            // — no exceptions needed for flow control.
            var canRead = await channel.Reader.WaitToReadAsync(cancellationToken)
                .ConfigureAwait(false);

            // Clean up: remove the channel from the dictionary to prevent memory leaks.
            // TryRemove is safe even if another thread already removed it.
            _channels.TryRemove(sessionId, out _);

            if (!canRead)
            {
                return GeneralResult.Failure("Client disconnected before diagnosis completed.");
            }

            // Consume the item from the channel (non-blocking since WaitToReadAsync confirmed data is available)
            channel.Reader.TryRead(out _);

            return GeneralResult.Success("Diagnosis callback received successfully.");
        }

        /// <inheritdoc />
        public void NotifyDiagnosisComplete(int sessionId)
        {
            if (_channels.TryGetValue(sessionId, out var channel))
            {
                // TryWrite returns false if the channel is full — that means
                // a previous notification was already sent (shouldn't happen, but safe).
                channel.Writer.TryWrite(true);

                // Mark the channel as complete so no more writes can happen.
                channel.Writer.TryComplete();
            }
            // If no channel exists for this sessionId, it means no SSE client
            // is listening — the notification is simply discarded. That's OK because
            // the data is already saved in the database; the frontend can fetch it anytime.
        }
    }
}
