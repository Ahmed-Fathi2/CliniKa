using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CliniKa.Application.DTOs.Sessions
{
    public class WorkflowSessionPayloadDto
    {
        [JsonPropertyName("sessionId")]
        public int SessionId { get; set; }

        [JsonPropertyName("patientId")]
        public int PatientId { get; set; }

        [JsonPropertyName("patientName")]
        public string? PatientName { get; set; }

        [JsonPropertyName("audioTranscript")]
        public string? AudioTranscript { get; set; }

        [JsonPropertyName("newMedications")]
        public List<string> NewMedications { get; set; } = new();

        [JsonPropertyName("currentMedications")]
        public List<string> CurrentMedications { get; set; } = new();

        [JsonPropertyName("possibleDiagnoses")]
        public string? PossibleDiagnoses { get; set; }
    }
}
