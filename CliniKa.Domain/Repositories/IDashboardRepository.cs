namespace CliniKa.Domain.Repositories
{
    public interface IDashboardRepository
    {
        Task<List<DiseaseStatisticDto>> GetTopDiagnosedDiseasesAsync(int topCount);
        Task<int> GetTotalAiSessionsAsync();
    }
    public class DiseaseStatisticDto
    {
        public string DiseaseName { get; set; }
        public int Count { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalUsers { get; set; }
        public int TotalAiSessions { get; set; }
        public List<DiseaseStatisticDto> TopDiseases { get; set; }
    }
}
