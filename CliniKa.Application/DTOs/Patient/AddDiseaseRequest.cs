using System;

namespace CliniKa.Application.DTOs.Patient
{
    public class AddDiseaseRequest
    {
        /// <summary>
        /// Reference to an existing disease in the catalog. Nullable if using RawDiseaseName.
        /// </summary>
        public int? DiseaseId { get; set; }

        /// <summary>
        /// Free-text disease name typed by the patient. Nullable if using DiseaseId.
        /// </summary>
        public string? RawDiseaseName { get; set; }

        /// <summary>
        /// When was this disease diagnosed. Defaults to today if not provided.
        /// </summary>
        public DateTime? DiagnosedDate { get; set; }
    }
}
