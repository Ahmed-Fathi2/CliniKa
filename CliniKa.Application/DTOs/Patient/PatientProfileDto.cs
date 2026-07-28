using System;

namespace CliniKa.Application.DTOs.Patient
{
    public class PatientProfileDto
    {
        public int PatientId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? NationalId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsProfileCompleted => CompletedAt.HasValue;
    }
}
