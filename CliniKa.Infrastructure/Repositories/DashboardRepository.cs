using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace CliniKa.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DiseaseStatisticDto>> GetTopDiagnosedDiseasesAsync(int topCount)
        {
            return await _context.PatientDiseases
            .Include(pd => pd.Disease) 
            .GroupBy(pd => pd.Disease.DiseaseName)
            .Select(g => new DiseaseStatisticDto
            {
                DiseaseName = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(topCount)
            .ToListAsync();
        }

        public async Task<int> GetTotalAiSessionsAsync()
        {
            return await _context.SessionAIReports.CountAsync();
        }
    }
}
