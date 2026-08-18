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


            return new DashboardSummaryDto
            {
                TopDiseases = topDiseases,
                TotalAiSessions = totalAiSessions,
            };
        }
    }
}
