using CliniKa.Application;
using CliniKa.Application.Abstraction;
using CliniKa.Application.DTOs;
using CliniKa.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace CliniKa.Infrastructure.Services
{
    public class PatientService : IPatientService
    {
        private readonly IUnitOfWork _unitOfWork;
        public PatientService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<GeneralResult<PatientReadDTO>> GetPatientAsync(string query)
        {
            var patient = await _unitOfWork.Patient.GetAsync(
                    filter: p => p.NationalId == query || p.FullName.Contains(query),
                    trackChanges: false
                );

            if (patient == null)
            {
                GeneralResult<PatientReadDTO>.NotFound("Patient not found.");
            }

            var patientReadDTO = new PatientReadDTO
            {
                Id = patient.Id,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                BloodType = patient.BloodType,
                NationalId = patient.NationalId,
                Phone = patient.Phone
            };

            return GeneralResult<PatientReadDTO>.Success(patientReadDTO, "Patient retrieved successfully.");
        }
    }
}
