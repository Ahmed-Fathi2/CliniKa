using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.DTOs.Allergy;
using CliniKa.Domain.Entities;
using CliniKa.Domain.Repositories;
using FluentValidation;

namespace CliniKa.Application.Services
{
    public class AllergyService : IAllergyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidator<CreateAllergyRequest> _createValidator;
        private readonly IValidator<UpdateAllergyRequest> _updateValidator;

        public AllergyService(
            IUnitOfWork unitOfWork,
            IValidator<CreateAllergyRequest> createValidator,
            IValidator<UpdateAllergyRequest> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<GeneralResult<IEnumerable<AllergyResponseDto>>> GetAllAllergiesAsync(string? search = null)
        {
            IEnumerable<Allergy> allergies;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                allergies = await _unitOfWork.Allergies.GetAllAsync(
                    filter: a => a.AllergenName.ToLower().Contains(searchLower) || (a.AllergyType != null && a.AllergyType.ToLower().Contains(searchLower)),
                    trackChanges: false
                );
            }
            else
            {
                allergies = await _unitOfWork.Allergies.GetAllAsync(trackChanges: false);
            }

            var dtos = allergies.Select(a => new AllergyResponseDto
            {
                Id = a.Id,
                AllergenName = a.AllergenName,
                AllergyType = a.AllergyType
            });

            return GeneralResult<IEnumerable<AllergyResponseDto>>.Success(dtos, "Allergies retrieved successfully.");
        }

        public async Task<GeneralResult<AllergyResponseDto>> GetAllergyByIdAsync(int id)
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy == null)
            {
                return GeneralResult<AllergyResponseDto>.NotFound($"Allergy with ID {id} was not found.");
            }

            var dto = new AllergyResponseDto
            {
                Id = allergy.Id,
                AllergenName = allergy.AllergenName,
                AllergyType = allergy.AllergyType
            };

            return GeneralResult<AllergyResponseDto>.Success(dto, "Allergy retrieved successfully.");
        }

        public async Task<GeneralResult<AllergyResponseDto>> CreateAllergyAsync(CreateAllergyRequest request)
        {
            var validationResult = await _createValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<AllergyResponseDto>.ValidationFailure(errors);
            }

            var allergy = new Allergy
            {
                AllergenName = request.AllergenName.Trim(),
                AllergyType = request.AllergyType?.Trim()
            };

            _unitOfWork.Allergies.Add(allergy);
            await _unitOfWork.SaveChangesAsync();

            var dto = new AllergyResponseDto
            {
                Id = allergy.Id,
                AllergenName = allergy.AllergenName,
                AllergyType = allergy.AllergyType
            };

            return GeneralResult<AllergyResponseDto>.Success(dto, "Allergy created successfully.");
        }

        public async Task<GeneralResult<AllergyResponseDto>> UpdateAllergyAsync(int id, UpdateAllergyRequest request)
        {
            var validationResult = await _updateValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                return GeneralResult<AllergyResponseDto>.ValidationFailure(errors);
            }

            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy == null)
            {
                return GeneralResult<AllergyResponseDto>.NotFound($"Allergy with ID {id} was not found.");
            }

            allergy.AllergenName = request.AllergenName.Trim();
            allergy.AllergyType = request.AllergyType?.Trim();

            await _unitOfWork.SaveChangesAsync();

            var dto = new AllergyResponseDto
            {
                Id = allergy.Id,
                AllergenName = allergy.AllergenName,
                AllergyType = allergy.AllergyType
            };

            return GeneralResult<AllergyResponseDto>.Success(dto, "Allergy updated successfully.");
        }

        public async Task<GeneralResult> DeleteAllergyAsync(int id)
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy == null)
            {
                return GeneralResult.NotFound($"Allergy with ID {id} was not found.");
            }

            _unitOfWork.Allergies.Delete(allergy);
            await _unitOfWork.SaveChangesAsync();

            return GeneralResult.Success("Allergy deleted successfully.");
        }
    }
}
