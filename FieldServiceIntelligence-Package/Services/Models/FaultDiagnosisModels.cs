using Microsoft.ML.Data;

namespace ML.Services.Models
{
    public class Equipment
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime InstallationDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Dictionary<string, string> Specifications { get; set; } = new();
        public int MaintenanceCycleMonths { get; set; }
        public DateTime LastMaintenanceDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Decommissioned
    }

    public class FaultType
    {
        public string FaultId { get; set; } = string.Empty;
        public string FaultCode { get; set; } = string.Empty;
        public string FaultName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Electrical, Mechanical, Software, etc.
        public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
        public int TypicalResolutionTimeMinutes { get; set; }
        public List<string> CommonCauses { get; set; } = new();
        public List<string> RequiredSkills { get; set; } = new();
    }

    public class Part
    {
        public string PartId { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public int StockLevel { get; set; }
        public int ReorderLevel { get; set; }
        public string WarrantyPeriod { get; set; } = string.Empty;
        public List<string> CompatibleEquipmentTypes { get; set; } = new();
    }

    public class HistoricalFault
    {
        public string FaultHistoryId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string FaultId { get; set; } = string.Empty;
        public string TechnicianId { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
        public DateTime ResolvedDate { get; set; }
        public int ResolutionTimeMinutes { get; set; }
        public string InitialDiagnosis { get; set; } = string.Empty;
        public string FinalDiagnosis { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public List<string> PartsUsed { get; set; } = new();
        public bool SlaMetric { get; set; }
        public decimal TotalCost { get; set; }
        public int CustomerSatisfactionScore { get; set; } // 1-10 scale
        public string TechnicianNotes { get; set; } = string.Empty;
    }

    public class TechnicianKPI
    {
        public string TechnicianId { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalJobsCompleted { get; set; }
        public int SlaJobsCompleted { get; set; }
        public double SlaComplianceRate { get; set; }
        public double AverageResolutionTimeMinutes { get; set; }
        public double FirstCallResolutionRate { get; set; }
        public double AverageCustomerSatisfaction { get; set; }
        public int TotalRevenue { get; set; }
        public int CallbackJobs { get; set; }
        public double CallbackRate { get; set; }
        public Dictionary<string, int> FaultTypesResolved { get; set; } = new();
        public Dictionary<string, double> FaultTypeSuccessRates { get; set; } = new();
        public List<string> TopPerformingEquipmentTypes { get; set; } = new();
        public double UtilizationRate { get; set; }
        public int CertificationsEarned { get; set; }
    }

    public class FaultDiagnosisRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string ReportedSymptoms { get; set; } = string.Empty;
        public List<string> ObservedBehaviors { get; set; } = new();
        public DateTime FaultReportedDate { get; set; }
        public string Priority { get; set; } = "Medium";
        public string ReportedBy { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentalConditions { get; set; } = new();
        public bool IncludePartsRecommendations { get; set; } = true;
        public bool IncludeTechnicianRecommendations { get; set; } = true;
    }

    public class FaultDiagnosisResponse
    {
        public string DiagnosisId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public List<FaultPrediction> PossibleFaults { get; set; } = new();
        public List<PartRecommendation> RecommendedParts { get; set; } = new();
        public List<TechnicianRecommendation> RecommendedTechnicians { get; set; } = new();
        public int EstimatedResolutionTimeMinutes { get; set; }
        public decimal EstimatedCost { get; set; }
        public string RecommendedActions { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string ModelVersion { get; set; } = string.Empty;
    }

    public class FaultPrediction
    {
        public string FaultId { get; set; } = string.Empty;
        public string FaultCode { get; set; } = string.Empty;
        public string FaultName { get; set; } = string.Empty;
        public double ProbabilityScore { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int EstimatedResolutionTime { get; set; }
        public string PredictedCause { get; set; } = string.Empty;
        public string DiagnosisExplanation { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public double HistoricalSuccessRate { get; set; }
    }

    public class PartRecommendation
    {
        public string PartId { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public double RecommendationScore { get; set; }
        public string ReasonForRecommendation { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public bool InStock { get; set; }
        public int StockLevel { get; set; }
        public string EstimatedDelivery { get; set; } = string.Empty;
        public double HistoricalUsageRate { get; set; }
    }

    public class FaultDiagnosisMLInput
    {
        [LoadColumn(0)] public float EquipmentAge { get; set; }
        [LoadColumn(1)] public float TimeSinceLastMaintenance { get; set; }
        [LoadColumn(2)] public float SymptomSeverity { get; set; }
        [LoadColumn(3)] public float EnvironmentalFactors { get; set; }
        [LoadColumn(4)] public float UsageIntensity { get; set; }
        [LoadColumn(5)] public float EquipmentType { get; set; }
        [LoadColumn(6)] public float FaultFrequency { get; set; }
    }

    public class FaultDiagnosisMLOutput
    {
        [ColumnName("Score")] public float ProbabilityScore { get; set; }
        [ColumnName("PredictedLabel")] public string PredictedFaultType { get; set; } = string.Empty;
    }

    public class TechnicianPerformanceMetrics
    {
        public string TechnicianId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SpecializationArea { get; set; } = string.Empty;
        public double FaultDiagnosisAccuracy { get; set; }
        public double AverageRepairTime { get; set; }
        public double SpecificEquipmentExpertise { get; set; }
        public double SpecificFaultTypeExpertise { get; set; }
        public int RecentJobsCount { get; set; }
        public double CustomerFeedbackScore { get; set; }
        public bool AvailableForEmergency { get; set; }
        public DateTime NextAvailableTime { get; set; }
        public List<string> CertifiedEquipmentTypes { get; set; } = new();
        public List<string> ExpertFaultTypes { get; set; } = new();
    }
}