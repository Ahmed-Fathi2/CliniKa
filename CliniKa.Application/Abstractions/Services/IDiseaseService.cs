using System.Collections.Generic;
using System.Threading.Tasks;
using CliniKa.Application.DTOs.Disease;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IDiseaseService
    {
        Task<GeneralResult<IEnumerable<DiseaseResponseDto>>> GetAllDiseasesAsync(string? search = null);
        Task<GeneralResult<DiseaseResponseDto>> GetDiseaseByIdAsync(int id);
        Task<GeneralResult<DiseaseResponseDto>> CreateDiseaseAsync(CreateDiseaseRequest request);
        Task<GeneralResult<DiseaseResponseDto>> UpdateDiseaseAsync(int id, UpdateDiseaseRequest request);
        Task<GeneralResult> DeleteDiseaseAsync(int id);
    }
}
