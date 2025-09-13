using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Interfaces
{
    public interface IRecommendationService
    {
        Task<RecommendationResponse> GetTechnicianRecommendationsAsync(RecommendationRequest request);
    }

    public interface IRecommendationScoring
    {
        double CalculateSkillsScore(Dictionary<string, string> requiredSkills, Dictionary<string, int> technicianSkills);
        double CalculateDistanceScore(double distance, double maxDistance = 200.0);
        double CalculateAvailabilityScore(Technician technician, DateTime scheduledDate);
        double CalculateSlaScore(double historicalSlaRate);
        double CalculateGeographicScore(GeographicMatch match);
        double CalculateOverallScore(double skillsScore, double distanceScore, double availabilityScore, double slaScore, double geographicScore, string priority);
    }

    public interface IEmergencyDiversionService
    {
        Task<EmergencyDiversionResponse> HandleEmergencyDiversionAsync(EmergencyDiversionRequest request);
        Task<List<NotificationAction>> GenerateNotificationsAsync(EmergencyDiversionResponse diversion);
    }

    public interface ITrafficAwareRoutingService
    {
        Task<RouteOptimization> OptimizeRouteAsync(double startLat, double startLng, double endLat, double endLng);
        Task<List<TrafficIncident>> GetActiveIncidentsAsync(double lat, double lng, double radiusKm);
        Task<double> CalculateTrafficAwareTravelTimeAsync(double distance, List<TrafficIncident> incidents);
    }

    public interface IGeographicMatchingService
    {
        GeographicMatch CalculateGeographicMatch(JobRequest job, Technician technician);
        bool IsWithinServiceRadius(double jobLat, double jobLng, double techLat, double techLng, double serviceRadiusKm);
        bool IsInPrimaryCity(string jobLocation, List<string> primaryCities);
        bool IsInSecondaryCity(string jobLocation, List<string> secondaryCities);
        bool IsInPostalCode(string jobLocation, List<string> postalCodes);
        List<string> GetMatchingRegions(double jobLat, double jobLng, List<GeographicRegion> regions);
        double CalculateDistance(double lat1, double lng1, double lat2, double lng2);
    }

    public interface ITechnicianTrackingService
    {
        Task<bool> UpdateTechnicianLocationAsync(TrackingUpdate update);
        Task<TechnicianStatus> GetTechnicianStatusAsync(string technicianId);
        Task<List<TrackingUpdate>> GetRecentTrackingHistoryAsync(string technicianId, int hours = 24);
    }

    public interface IMLNetScoringService
    {
        Task<float> PredictTechnicianScoreAsync(MLNetInput input);
        Task InitializeModelAsync(string modelPath);
        bool IsModelLoaded { get; }
    }
}