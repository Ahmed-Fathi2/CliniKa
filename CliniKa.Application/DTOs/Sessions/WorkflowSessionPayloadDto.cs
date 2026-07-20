using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Application.DTOs.Sessions
{
    public class WorkflowSessionPayloadDto
    {
        public int SessionId { get; set; }
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? AudioTranscript { get; set; }
        public string? NewMedications { get; set; }
        public List<string> CurrentMedications { get; set; } = new();
        public string? PossibleDiagnoses { get; set; }
    }
}
