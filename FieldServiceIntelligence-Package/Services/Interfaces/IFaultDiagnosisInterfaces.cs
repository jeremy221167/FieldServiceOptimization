using ML.Services.Models;

namespace ML.Services.Interfaces
{
    public interface IFaultDiagnosisService
    {
        Task<FaultDiagnosisResponse> DiagnoseFaultAsync(FaultDiagnosisRequest request);
        Task<List<FaultPrediction>> PredictPossibleFaultsAsync(string tenantId, string equipmentId, string symptoms);
        Task<List<PartRecommendation>> RecommendPartsAsync(string tenantId, List<FaultPrediction> faultPredictions);
        Task<List<TechnicianRecommendation>> RecommendTechniciansForFaultAsync(string tenantId, List<FaultPrediction> faultPredictions);
        Task<double> CalculateFaultProbabilityAsync(string tenantId, string equipmentId, string faultId, string symptoms);
    }

    public interface IEquipmentHistoryService
    {
        Task<List<HistoricalFault>> GetEquipmentFaultHistoryAsync(string tenantId, string equipmentId, int daysBack = 365);
        Task<List<HistoricalFault>> GetSimilarFaultHistoryAsync(string tenantId, string faultId, int maxResults = 50);
        Task<Dictionary<string, double>> GetFaultFrequencyByEquipmentTypeAsync(string tenantId, string equipmentType);
        Task<Dictionary<string, List<string>>> GetCommonPartsForFaultAsync(string tenantId, string faultId);
        Task<List<TechnicianPerformanceMetrics>> GetTechnicianFaultExpertiseAsync(string tenantId, string faultId);
    }

    public interface ITechnicianKPIService
    {
        Task<TechnicianKPI> GetTechnicianKPIAsync(string tenantId, string technicianId, DateTime periodStart, DateTime periodEnd);
        Task<List<TechnicianKPI>> GetAllTechnicianKPIsAsync(string tenantId, DateTime periodStart, DateTime periodEnd);
        Task<Dictionary<string, double>> GetTechnicianFaultSpecializationAsync(string tenantId, string technicianId);
        Task<List<TechnicianPerformanceMetrics>> RankTechniciansForFaultTypeAsync(string tenantId, string faultId);
        Task<double> CalculateTechnicianSuccessRateAsync(string tenantId, string technicianId, string faultId);
        Task UpdateTechnicianKPIAsync(string tenantId, string technicianId, HistoricalFault completedJob);
    }

    public interface IPartsRecommendationService
    {
        Task<List<PartRecommendation>> GetPartsForFaultAsync(string tenantId, string faultId, string equipmentId);
        Task<List<PartRecommendation>> PredictPartsNeededAsync(string tenantId, List<FaultPrediction> faultPredictions);
        Task<Dictionary<string, double>> GetPartUsageFrequencyAsync(string tenantId, string faultId);
        Task<List<Part>> GetLowStockPartsAsync(string tenantId);
        Task<decimal> EstimateRepairCostAsync(string tenantId, List<PartRecommendation> parts);
    }

    public interface IFaultMLService
    {
        Task<FaultDiagnosisMLOutput> PredictFaultTypeAsync(string tenantId, FaultDiagnosisMLInput input);
        Task<bool> LoadFaultModelAsync(string tenantId, string equipmentType);
        Task<float> CalculateFaultProbabilityScoreAsync(string tenantId, string equipmentId, string symptoms);
        Task<List<FaultPrediction>> GetTopFaultPredictionsAsync(string tenantId, FaultDiagnosisMLInput input, int maxResults = 5);
    }

    public interface IEquipmentService
    {
        Task<Equipment?> GetEquipmentAsync(string tenantId, string equipmentId);
        Task<List<Equipment>> GetEquipmentByTypeAsync(string tenantId, string equipmentType);
        Task<List<Equipment>> GetEquipmentByLocationAsync(string tenantId, double latitude, double longitude, double radiusKm);
        Task<Dictionary<string, int>> GetEquipmentMaintenanceStatusAsync(string tenantId);
        Task<List<Equipment>> GetDueForMaintenanceAsync(string tenantId);
    }
}