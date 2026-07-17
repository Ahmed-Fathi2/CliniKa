using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CliniKa.Application.DTOs.Langflow
{
    public class LangFlowDiagnosisResult
    {
        [JsonPropertyName("status")]
        public int? Status { get; set; }

        [JsonPropertyName("body")]
        public LangFlowDiagnosisResultBody? Body { get; set; }
    }

    public class LangFlowDiagnosisResultBody
    {
        [JsonPropertyName("transcript")]
        public string? Transcript { get; set; }

        [JsonPropertyName("analysis")]
        public string? Analysis { get; set; }

        [JsonPropertyName("Chief Complaint")]
        public string? ChiefComplaint { get; set; }

        [JsonPropertyName("History of Present Illness")]
        public string? HistoryOfPresentIllness { get; set; }

        [JsonPropertyName("Complete Findings From All Medical Images")]
        public string? CompleteFindingsFromAllMedicalImages { get; set; }

        [JsonPropertyName("Possible Diagnosis")]
        public string? PossibleDiagnosis { get; set; }

        [JsonPropertyName("Top 5 Differential Diagnoses")]
        public List<string>? Top5DifferentialDiagnoses { get; set; }

        [JsonPropertyName("Recommended Investigations")]
        public List<string>? RecommendedInvestigations { get; set; }

        [JsonPropertyName("Immediate Management")]
        public string? ImmediateManagement { get; set; }

        [JsonPropertyName("images_received")]
        public int? ImagesReceived { get; set; }
    }
}
