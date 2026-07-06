using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CliniKa.Application.DTOs
{
    public class LangFlowResponse
    {
        [JsonPropertyName("outputs")]
        public List<LangFlowOutputContainer>? Outputs { get; set; }
    }

    public class LangFlowOutputContainer
    {
        [JsonPropertyName("outputs")]
        public List<LangFlowOutput>? Outputs { get; set; }
    }

    public class LangFlowOutput
    {
        [JsonPropertyName("results")]
        public LangFlowResult? Results { get; set; }
    }

    public class LangFlowResult
    {
        [JsonPropertyName("message")]
        public LangFlowMessage? Message { get; set; }
    }

    public class LangFlowMessage
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class LangFlowAudioTranscriptResult
    {
        [JsonPropertyName("AudioTranscript")]
        public string AudioTranscript { get; set; } = string.Empty;

        [JsonPropertyName("Symptoms")]
        public List<string> Symptoms { get; set; } = new();

        [JsonPropertyName("MentionedMedications")]
        public List<string> MentionedMedications { get; set; } = new();

        [JsonPropertyName("MentionedHistory")]
        public List<string> MentionedHistory { get; set; } = new();
    }
}
