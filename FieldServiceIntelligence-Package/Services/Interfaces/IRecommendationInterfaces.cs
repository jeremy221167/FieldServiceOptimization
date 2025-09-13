using ML.Services.Models;

namespace ML.Services.Interfaces
{
    public interface IMLNetPredictionService
    {
        Task<float> PredictScoreAsync(string tenantId, MLNetInput input);
        Task<bool> LoadTenantModelAsync(string tenantId);
        Task<string> GetModelVersionAsync(string tenantId);
        Task<bool> IsModelLoadedAsync(string tenantId);
    }

    public interface ILlmExplanationService
    {
        Task<string> GenerateExplanationAsync(
            JobRequest job,
            Technician technician,
            TechnicianRecommendation recommendation);

        Task<Dictionary<string, string>> GenerateBatchExplanationsAsync(
            JobRequest job,
            List<TechnicianRecommendation> recommendations);

        Task<bool> IsServiceAvailableAsync();
    }

    public interface ITenantRecommendationService
    {
        Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request);
        Task<List<TechnicianRecommendation>> ScoreTechniciansAsync(
            string tenantId,
            JobRequest job,
            List<Technician> technicians);
        Task<bool> ValidateTenantAccessAsync(string tenantId);
    }

    public interface ITenantModelManager
    {
        Task<string> GetModelPathAsync(string tenantId);
        Task<bool> ModelExistsAsync(string tenantId);
        Task<DateTime> GetModelLastUpdatedAsync(string tenantId);
        Task<string> GetModelVersionAsync(string tenantId);
        Task UpdateModelAsync(string tenantId, byte[] modelData);
    }

    public interface IRecommendationScoring
    {
        float CalculateSkillsScore(Dictionary<string, string> requiredSkills, Dictionary<string, int> technicianSkills);
        float CalculateDistanceScore(double jobLat, double jobLng, double techLat, double techLng);
        float CalculateAvailabilityScore(Technician technician, DateTime jobScheduledDate);
        float CalculateSlaScore(double historicalSuccessRate);
        float CalculateWorkloadScore(int currentWorkload);
        float CalculateGeographicScore(JobRequest job, Technician technician);
        GeographicMatch CalculateGeographicMatch(JobRequest job, Technician technician);
        double CalculateDistance(double lat1, double lng1, double lat2, double lng2);
        double EstimateTravelTime(double distanceKm);
    }

    public interface IEmergencyDiversionService
    {
        Task<EmergencyDiversionResponse> FindBestDiversionAsync(EmergencyDiversionRequest request);
        Task<List<TechnicianRecommendation>> GetInterruptibleTechniciansAsync(
            JobRequest emergencyJob, List<Technician> technicians);
        Task<double> CalculateInterruptionCostAsync(Technician technician, JobRequest emergencyJob);
    }

    public interface ITechnicianTrackingService
    {
        Task UpdateTechnicianLocationAsync(TrackingUpdate update);
        Task<TechnicianStatus> GetTechnicianStatusAsync(string technicianId);
        Task<double> GetEstimatedArrivalTimeAsync(string technicianId, double destLat, double destLng);
        Task StartTrackingAsync(string technicianId, string jobId);
        Task StopTrackingAsync(string technicianId);
    }

    public interface INotificationService
    {
        Task SendSmsAsync(string phoneNumber, string message);
        Task SendEmailAsync(string email, string subject, string message);
        Task<bool> SendETAUpdateAsync(string customerPhone, string technicianName, double etaMinutes);
        Task<bool> SendEmergencyDiversionNotificationAsync(NotificationAction notification);
    }

    public interface ITrafficAwareRoutingService
    {
        Task<TrafficAwareETAResponse> CalculateTrafficAwareETAAsync(RouteOptimizationRequest request);
        Task<TrafficAwareRoute> GetOptimalRouteAsync(RouteOptimizationRequest request);
        Task<List<TrafficIncident>> GetTrafficIncidentsAsync(double centerLat, double centerLng, double radiusKm);
        Task<bool> IsRouteAffectedByTrafficAsync(string routeId);
        Task<AlternativeRoute?> FindFasterAlternativeAsync(TrafficAwareRoute currentRoute);
    }

    public interface IGoogleMapsService
    {
        Task<TrafficAwareRoute> GetDirectionsAsync(double originLat, double originLng,
            double destLat, double destLng, RoutePreferences? preferences = null);
        Task<List<RouteWaypoint>> GeocodeAddressAsync(string address);
        Task<string> ReverseGeocodeAsync(double latitude, double longitude);
        Task<List<TrafficIncident>> GetRoadClosuresAsync(double centerLat, double centerLng, double radiusKm);
        Task<MapVisualization> GenerateMapVisualizationAsync(List<TechnicianLocation> technicians,
            List<JobLocation> jobs, List<TrafficAwareRoute> routes);
    }

    public interface IRouteOptimizationService
    {
        Task<List<TrafficAwareRoute>> OptimizeMultipleRoutesAsync(List<RouteOptimizationRequest> requests);
        Task<TrafficAwareRoute> OptimizeEmergencyRouteAsync(RouteOptimizationRequest emergencyRequest);
        Task<bool> UpdateRouteForTrafficAsync(string routeId, List<TrafficIncident> newIncidents);
        Task<List<TechnicianLocation>> GetOptimalTechnicianPositionsAsync(List<JobLocation> upcomingJobs);
    }

    public interface IMapVisualizationService
    {
        Task<MapVisualization> CreateFleetMapAsync(List<Technician> technicians, List<JobRequest> jobs);
        Task<string> GenerateTechnicianIconAsync(Technician technician, string status);
        Task UpdateTechnicianLocationAsync(string mapId, TechnicianLocation location);
        Task AddTrafficIncidentAsync(string mapId, TrafficIncident incident);
        Task RemoveTrafficIncidentAsync(string mapId, string incidentId);
    }
}