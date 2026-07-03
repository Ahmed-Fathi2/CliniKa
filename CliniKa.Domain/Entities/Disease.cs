using System;
using System.Collections.Generic;

namespace CliniKa.Domain.Entities
{
    public class Disease
    {
        public int Id { get; set; }
        public string DiseaseCode { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<PatientDisease> PatientDiseases { get; set; } = new List<PatientDisease>();
    }
}
