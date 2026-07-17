using System.Collections.Generic;

namespace CliniKa.Application.DTOs.Sessions
{
    public class DiagnoseSessionResponseDto
    {
        public int SessionId { get; set; }
        public string AudioTranscript { get; set; } = string.Empty;
        public List<string> ExtractedSymptoms { get; set; } = new();
        public List<PossibleDiagnosisDto> PossibleDiagnoses { get; set; } = new();
    }

    public class PossibleDiagnosisDto
    {
        public string DiagnosisName { get; set; } = string.Empty;
        public string ConfidenceScore { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
    }
}
