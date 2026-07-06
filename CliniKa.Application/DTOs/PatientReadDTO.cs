using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Application.DTOs
{
    public class PatientReadDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? BloodType { get; set; }
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
    }
}
