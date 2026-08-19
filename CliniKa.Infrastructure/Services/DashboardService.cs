using CliniKa.Application.Abstractions.Services;
using CliniKa.Domain.Repositories;

namespace CliniKa.Infrastructure.Services
{
    public class DashboardService (IUnitOfWork unitOfWork) : IDashboardService
    {
        public async Task<DashboardSummaryDto> GetDashboardDataAsync()
        {
            var topDiseases = await unitOfWork.Dashboard.GetTopDiagnosedDiseasesAsync(5);
            var totalAiSessions = await unitOfWork.Dashboard.GetTotalAiSessionsAsync();

            var dummyTopDiseases = new List<DiseaseStatisticDto>
        {
            new DiseaseStatisticDto { DiseaseName = "Panadol (Paracetamol)", Count = 150 },
            new DiseaseStatisticDto { DiseaseName = "Amoxil (Amoxicillin)", Count = 120 },
            new DiseaseStatisticDto { DiseaseName = "Cataflam (Diclofenac)", Count = 95 },
            new DiseaseStatisticDto { DiseaseName = "Concor (Bisoprolol)", Count = 60 },
            new DiseaseStatisticDto { DiseaseName = "Augmentin", Count = 45 }
        };


            return new DashboardSummaryDto
            {
                //TopDiseases = topDiseases == null || topDiseases.Count == 0 ? dummyTopDiseases : topDiseases,
                TopDiseases = dummyTopDiseases,
                TotalAiSessions = totalAiSessions,
            };
        }
    }
}
