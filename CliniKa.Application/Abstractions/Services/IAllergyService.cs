using System.Collections.Generic;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.Allergy;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IAllergyService
    {
        Task<GeneralResult<IEnumerable<AllergyResponseDto>>> GetAllAllergiesAsync(string? search = null);
        Task<GeneralResult<AllergyResponseDto>> GetAllergyByIdAsync(int id);
        Task<GeneralResult<AllergyResponseDto>> CreateAllergyAsync(CreateAllergyRequest request);
        Task<GeneralResult<AllergyResponseDto>> UpdateAllergyAsync(int id, UpdateAllergyRequest request);
        Task<GeneralResult> DeleteAllergyAsync(int id);
    }
}
