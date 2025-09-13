using System.ComponentModel.DataAnnotations;
using Microsoft.ML.Data;

namespace ML.Services.Models
{
    public class JobRequest
    {
        public string JobId { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string Equipment { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int SlaHours { get; set; }
        public string Priority { get; set; } = "Normal";
        public Dictionary<string, string> RequiredSkills { get; set; } = new();
        public bool IsEmergency { get; set; } = false;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }

    public class Technician
    {
        public string TechnicianId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Dictionary<string, int> Skills { get; set; } = new();
        public bool IsAvailable { get; set; }
        public int CurrentWorkload { get; set; }
        public double HistoricalSlaSuccessRate { get; set; }
        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableUntil { get; set; }
        public GeographicCoverage CoverageArea { get; set; } = new();
        public TechnicianStatus CurrentStatus { get; set; } = new();
        public string CurrentJobId { get; set; } = string.Empty;
        public bool CanBeInterrupted { get; set; } = true;
        public string Phone { get; set; } = string.Empty;
    }

    public class TechnicianStatus
    {
        public string Status { get; set; } = "Available"; // Available, EnRoute, OnSite, Busy
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime LastLocationUpdate { get; set; } = DateTime.UtcNow;
        public double EstimatedArrivalMinutes { get; set; }
        public string CurrentJobLocation { get; set; } = string.Empty;
        public bool IsTracking { get; set; } = false;
    }

    public class GeographicCoverage
    {
        public double ServiceRadiusKm { get; set; } = 50.0;
        public List<string> PrimaryCities { get; set; } = new();
        public List<string> SecondaryCities { get; set; } = new();
        public List<string> PostalCodes { get; set; } = new();
        public List<GeographicRegion> PreferredRegions { get; set; } = new();
        public bool IsWillingToTravel { get; set; } = true;
        public double MaxTravelDistanceKm { get; set; } = 200.0;
    }

    public class GeographicRegion
    {
        public string RegionName { get; set; } = string.Empty;
        public string RegionType { get; set; } = string.Empty; // "City", "County", "State", "PostalCode", "Custom"
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public int Priority { get; set; } = 1; // 1 = highest priority
    }

    public class TechnicianRecommendation
    {
        public string TechnicianId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Score { get; set; }
        public double SkillsScore { get; set; }
        public double DistanceScore { get; set; }
        public double AvailabilityScore { get; set; }
        public double SlaScore { get; set; }
        public double GeographicScore { get; set; }
        public string? Explanation { get; set; }
        public double EstimatedTravelTime { get; set; }
        public double Distance { get; set; }
        public GeographicMatch GeographicMatch { get; set; } = new();
    }

    public class GeographicMatch
    {
        public bool IsWithinServiceRadius { get; set; }
        public bool IsInPrimaryCity { get; set; }
        public bool IsInSecondaryCity { get; set; }
        public bool IsInPostalCode { get; set; }
        public List<string> MatchingRegions { get; set; } = new();
        public double DistanceFromServiceCenter { get; set; }
        public string CoverageType { get; set; } = string.Empty; // "Primary", "Secondary", "Extended", "OutOfRange"
    }

    public class RecommendationRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public JobRequest Job { get; set; } = new();
        public List<Technician> AvailableTechnicians { get; set; } = new();
        public int MaxRecommendations { get; set; } = 5;
        public bool IncludeLlmExplanation { get; set; } = true;
    }

    public class RecommendationResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public List<TechnicianRecommendation> Recommendations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string ModelVersion { get; set; } = string.Empty;
    }

    public class MLNetInput
    {
        [LoadColumn(0)] public float SkillsMatch { get; set; }
        [LoadColumn(1)] public float Distance { get; set; }
        [LoadColumn(2)] public float Workload { get; set; }
        [LoadColumn(3)] public float SlaHistory { get; set; }
        [LoadColumn(4)] public float TravelTime { get; set; }
        [LoadColumn(5)] public float Priority { get; set; }
    }

    public class MLNetOutput
    {
        [ColumnName("Score")] public float Score { get; set; }
    }

    public class EmergencyDiversionRequest
    {
        public JobRequest EmergencyJob { get; set; } = new();
        public List<Technician> CurrentlyAssignedTechnicians { get; set; } = new();
        public List<Technician> AllAvailableTechnicians { get; set; } = new();
        public bool AllowInterruption { get; set; } = true;
        public int MaxDiversionRadius { get; set; } = 100;
    }

    public class EmergencyDiversionResponse
    {
        public TechnicianRecommendation RecommendedTechnician { get; set; } = new();
        public string DiversionType { get; set; } = string.Empty; // "Available", "Interrupted", "Rerouted"
        public string PreviousJobId { get; set; } = string.Empty;
        public double EstimatedDelayMinutes { get; set; }
        public List<NotificationAction> RequiredNotifications { get; set; } = new();
    }

    public class NotificationAction
    {
        public string Type { get; set; } = string.Empty; // "SMS", "Call", "Email"
        public string Recipient { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsUrgent { get; set; } = false;
    }

    public class TrackingUpdate
    {
        public string TechnicianId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double SpeedKmh { get; set; }
        public string Status { get; set; } = string.Empty;
        public double EstimatedArrivalMinutes { get; set; }
    }
}