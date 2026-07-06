using System.Collections.Generic;

namespace CliniKa.Application.DTOs.Sessions
{
    public class TranscribeSessionResponse
    {
        public int SessionId { get; set; }
        public string AudioTranscript { get; set; } = string.Empty;
        public List<string> ExtractedSymptoms { get; set; } = new();
        public List<string> MentionedMedications { get; set; } = new();
        public List<string> MentionedHistory { get; set; } = new();
    }
}
