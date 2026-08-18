using CliniKa.Domain.Repositories;

namespace CliniKa.Application.Abstractions.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardDataAsync();
    }
}
