using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ILogger<RecommendationService> _logger;
        private readonly IRecommendationScoring _scoringService;
        private readonly IGeographicMatchingService _geoMatchingService;

        public RecommendationService(
            ILogger<RecommendationService> logger,
            IRecommendationScoring scoringService,
            IGeographicMatchingService geoMatchingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
            _geoMatchingService = geoMatchingService ?? throw new ArgumentNullException(nameof(geoMatchingService));
        }

        public async Task<RecommendationResponse> GetTechnicianRecommendationsAsync(RecommendationRequest request)
        {
            try
            {
                if (request?.Job == null || request.AvailableTechnicians?.Any() != true)
                {
                    return new RecommendationResponse
                    {
                        JobId = request?.Job?.JobId ?? "",
                        TenantId = request?.TenantId ?? "",
                        Recommendations = new List<TechnicianRecommendation>()
                    };
                }

                var recommendations = new List<TechnicianRecommendation>();

                foreach (var technician in request.AvailableTechnicians.Where(t => t != null))
                {
                    var recommendation = await CalculateTechnicianRecommendationAsync(request.Job, technician);
                    if (recommendation.Score > 0)
                    {
                        recommendations.Add(recommendation);
                    }
                }

                var topRecommendations = recommendations
                    .OrderByDescending(r => r.Score)
                    .Take(request.MaxRecommendations)
                    .ToList();

                if (request.IncludeLlmExplanation)
                {
                    foreach (var rec in topRecommendations)
                    {
                        rec.Explanation = GenerateExplanation(rec, request.Job);
                    }
                }

                return new RecommendationResponse
                {
                    JobId = request.Job.JobId,
                    TenantId = request.TenantId,
                    Recommendations = topRecommendations,
                    ModelVersion = "v1.0.0"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating technician recommendations for job {JobId}", request?.Job?.JobId);
                return new RecommendationResponse
                {
                    JobId = request?.Job?.JobId ?? "",
                    TenantId = request?.TenantId ?? "",
                    Recommendations = new List<TechnicianRecommendation>()
                };
            }
        }

        private async Task<TechnicianRecommendation> CalculateTechnicianRecommendationAsync(JobRequest job, Technician technician)
        {
            try
            {
                var geoMatch = _geoMatchingService.CalculateGeographicMatch(job, technician);
                var distance = _geoMatchingService.CalculateDistance(
                    job.Latitude, job.Longitude,
                    technician.Latitude, technician.Longitude);

                var skillsScore = _scoringService.CalculateSkillsScore(job.RequiredSkills, technician.Skills);
                var distanceScore = _scoringService.CalculateDistanceScore(distance);
                var availabilityScore = _scoringService.CalculateAvailabilityScore(technician, job.ScheduledDate);
                var slaScore = _scoringService.CalculateSlaScore(technician.HistoricalSlaSuccessRate);
                var geographicScore = _scoringService.CalculateGeographicScore(geoMatch);

                var overallScore = _scoringService.CalculateOverallScore(
                    skillsScore, distanceScore, availabilityScore, slaScore, geographicScore, job.Priority);

                var estimatedTravelTime = CalculateEstimatedTravelTime(distance);

                return new TechnicianRecommendation
                {
                    TechnicianId = technician.TechnicianId,
                    Name = technician.Name,
                    Score = overallScore,
                    SkillsScore = skillsScore,
                    DistanceScore = distanceScore,
                    AvailabilityScore = availabilityScore,
                    SlaScore = slaScore,
                    GeographicScore = geographicScore,
                    EstimatedTravelTime = estimatedTravelTime,
                    Distance = distance,
                    GeographicMatch = geoMatch
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating recommendation for technician {TechnicianId}", technician?.TechnicianId);
                return new TechnicianRecommendation
                {
                    TechnicianId = technician?.TechnicianId ?? "",
                    Name = technician?.Name ?? "",
                    Score = 0.0
                };
            }
        }

        private static double CalculateEstimatedTravelTime(double distanceKm)
        {
            const double averageSpeedKmh = 45.0;
            return Math.Max(5.0, (distanceKm / averageSpeedKmh) * 60.0);
        }

        private static string GenerateExplanation(TechnicianRecommendation recommendation, JobRequest job)
        {
            var parts = new List<string>();

            if (recommendation.SkillsScore >= 0.8)
                parts.Add("Strong skill match");
            else if (recommendation.SkillsScore >= 0.6)
                parts.Add("Good skill match");
            else if (recommendation.SkillsScore >= 0.4)
                parts.Add("Partial skill match");
            else
                parts.Add("Limited skill match");

            if (recommendation.Distance <= 10)
                parts.Add("very close location");
            else if (recommendation.Distance <= 30)
                parts.Add("nearby location");
            else if (recommendation.Distance <= 60)
                parts.Add("moderate distance");
            else
                parts.Add("distant location");

            if (recommendation.AvailabilityScore >= 0.8)
                parts.Add("excellent availability");
            else if (recommendation.AvailabilityScore >= 0.6)
                parts.Add("good availability");
            else if (recommendation.AvailabilityScore >= 0.4)
                parts.Add("limited availability");
            else
                parts.Add("poor availability");

            if (recommendation.SlaScore >= 0.9)
                parts.Add("outstanding track record");
            else if (recommendation.SlaScore >= 0.8)
                parts.Add("strong track record");
            else if (recommendation.SlaScore >= 0.7)
                parts.Add("good track record");
            else
                parts.Add("average track record");

            var explanation = $"Recommended due to {string.Join(", ", parts)}. ";
            explanation += $"Estimated travel time: {Math.Round(recommendation.EstimatedTravelTime)} minutes. ";

            if (recommendation.GeographicMatch.IsInPrimaryCity)
                explanation += "Within primary service area.";
            else if (recommendation.GeographicMatch.IsWithinServiceRadius)
                explanation += "Within service radius.";
            else
                explanation += "Extended service area.";

            return explanation;
        }
    }
}