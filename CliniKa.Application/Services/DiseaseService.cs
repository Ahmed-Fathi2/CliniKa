using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Disease;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using FluentValidation;

namespace CliniKa.Application.Services
{
    public class DiseaseService : IDiseaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<CreateDiseaseRequest> _createValidator;
        private readonly IValidator<UpdateDiseaseRequest> _updateValidator;

        public DiseaseService(
            IUnitOfWork unitOfWork,
            IValidator<CreateDiseaseRequest> createValidator,
            IValidator<UpdateDiseaseRequest> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<GeneralResult<IEnumerable<DiseaseResponseDto>>> GetAllDiseasesAsync(string? search = null)
        {
            IEnumerable<Disease> diseases;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                diseases = await _unitOfWork.Diseases.GetAllAsync(
                    filter: d => d.DiseaseName.ToLower().Contains(searchLower) || d.DiseaseCode.ToLower().Contains(searchLower),
                    trackChanges: false
                );
            }
            else
            {
                diseases = await _unitOfWork.Diseases.GetAllAsync(trackChanges: false);
            }

            var dtos = diseases.Select(d => new DiseaseResponseDto
            {
                Id = d.Id,
                DiseaseCode = d.DiseaseCode,
                DiseaseName = d.DiseaseName,
                Description = d.Description
            });

            return GeneralResult<IEnumerable<DiseaseResponseDto>>.Success(dtos, "Diseases retrieved successfully.");
        }

        public async Task<GeneralResult<DiseaseResponseDto>> GetDiseaseByIdAsync(int id)
        {
            var disease = await _unitOfWork.Diseases.GetByIdAsync(id);
            if (disease == null)
            {
                return GeneralResult<DiseaseResponseDto>.NotFound($"Disease with ID {id} was not found.");
            }

            var dto = new DiseaseResponseDto
            {
                Id = disease.Id,
                DiseaseCode = disease.DiseaseCode,
                DiseaseName = disease.DiseaseName,
                Description = disease.Description
            };

            return GeneralResult<DiseaseResponseDto>.Success(dto, "Disease retrieved successfully.");
        }

        public async Task<GeneralResult<DiseaseResponseDto>> CreateDiseaseAsync(CreateDiseaseRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<DiseaseResponseDto>.ValidationFailure(errors);
            }

            var disease = new Disease
            {
                DiseaseCode = request.DiseaseCode.Trim(),
                DiseaseName = request.DiseaseName.Trim(),
                Description = request.Description?.Trim()
            };

            _unitOfWork.Diseases.Add(disease);
            await _unitOfWork.SaveChangesAsync();

            var dto = new DiseaseResponseDto
            {
                Id = disease.Id,
                DiseaseCode = disease.DiseaseCode,
                DiseaseName = disease.DiseaseName,
                Description = disease.Description
            };

            return GeneralResult<DiseaseResponseDto>.Success(dto, "Disease created successfully.");
        }

        public async Task<GeneralResult<DiseaseResponseDto>> UpdateDiseaseAsync(int id, UpdateDiseaseRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<DiseaseResponseDto>.ValidationFailure(errors);
            }

            var disease = await _unitOfWork.Diseases.GetByIdAsync(id);
            if (disease == null)
            {
                return GeneralResult<DiseaseResponseDto>.NotFound($"Disease with ID {id} was not found.");
            }

            disease.DiseaseCode = request.DiseaseCode.Trim();
            disease.DiseaseName = request.DiseaseName.Trim();
            disease.Description = request.Description?.Trim();

            await _unitOfWork.SaveChangesAsync();

            var dto = new DiseaseResponseDto
            {
                Id = disease.Id,
                DiseaseCode = disease.DiseaseCode,
                DiseaseName = disease.DiseaseName,
                Description = disease.Description
            };

            return GeneralResult<DiseaseResponseDto>.Success(dto, "Disease updated successfully.");
        }

        public async Task<GeneralResult> DeleteDiseaseAsync(int id)
        {
            var disease = await _unitOfWork.Diseases.GetByIdAsync(id);
            if (disease == null)
            {
                return GeneralResult.NotFound($"Disease with ID {id} was not found.");
            }

            _unitOfWork.Diseases.Delete(disease);
            await _unitOfWork.SaveChangesAsync();

            return GeneralResult.Success("Disease deleted successfully.");
        }
    }
}
