using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class TenantRecommendationService : ITenantRecommendationService
    {
        private readonly IMLNetPredictionService _predictionService;
        private readonly ILlmExplanationService _llmService;
        private readonly IRecommendationScoring _scoringService;
        private readonly ILogger<TenantRecommendationService> _logger;

        public TenantRecommendationService(
            IMLNetPredictionService predictionService,
            ILlmExplanationService llmService,
            IRecommendationScoring scoringService,
            ILogger<TenantRecommendationService> logger)
        {
            _predictionService = predictionService;
            _llmService = llmService;
            _scoringService = scoringService;
            _logger = logger;
        }

        public async Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request));

                if (string.IsNullOrEmpty(request.TenantId))
                    throw new ArgumentException("TenantId is required", nameof(request));

                if (!await ValidateTenantAccessAsync(request.TenantId))
                    throw new UnauthorizedAccessException($"Invalid tenant access: {request.TenantId}");

                _logger.LogInformation("Generating recommendations for job {JobId}, tenant {TenantId}",
                    request.Job.JobId, request.TenantId);

                var recommendations = await ScoreTechniciansAsync(
                    request.TenantId,
                    request.Job,
                    request.AvailableTechnicians);

                var topRecommendations = recommendations
                    .OrderByDescending(r => r.Score)
                    .Take(request.MaxRecommendations)
                    .ToList();

                if (request.IncludeLlmExplanation && topRecommendations.Any())
                {
                    var explanations = await _llmService.GenerateBatchExplanationsAsync(
                        request.Job, topRecommendations);

                    foreach (var rec in topRecommendations)
                    {
                        if (explanations.TryGetValue(rec.TechnicianId, out var explanation))
                        {
                            rec.Explanation = explanation;
                        }
                    }
                }

                var modelVersion = await _predictionService.GetModelVersionAsync(request.TenantId);

                var response = new RecommendationResponse
                {
                    JobId = request.Job.JobId,
                    TenantId = request.TenantId,
                    Recommendations = topRecommendations,
                    GeneratedAt = DateTime.UtcNow,
                    ModelVersion = modelVersion
                };

                _logger.LogInformation("Generated {Count} recommendations for job {JobId}",
                    topRecommendations.Count, request.Job.JobId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations for job {JobId}, tenant {TenantId}",
                    request?.Job?.JobId, request?.TenantId);
                throw;
            }
        }

        public async Task<List<TechnicianRecommendation>> ScoreTechniciansAsync(
            string tenantId,
            JobRequest job,
            List<Technician> technicians)
        {
            try
            {
                if (technicians == null || !technicians.Any())
                {
                    _logger.LogWarning("No technicians provided for scoring");
                    return new List<TechnicianRecommendation>();
                }

                var recommendations = new List<TechnicianRecommendation>();

                foreach (var technician in technicians)
                {
                    try
                    {
                        var skillsScore = _scoringService.CalculateSkillsScore(
                            job.RequiredSkills, technician.Skills);

                        var distanceScore = _scoringService.CalculateDistanceScore(
                            job.Latitude, job.Longitude,
                            technician.Latitude, technician.Longitude);

                        var availabilityScore = _scoringService.CalculateAvailabilityScore(
                            technician, job.ScheduledDate);

                        var slaScore = _scoringService.CalculateSlaScore(
                            technician.HistoricalSlaSuccessRate);

                        var workloadScore = _scoringService.CalculateWorkloadScore(
                            technician.CurrentWorkload);

                        var geographicScore = _scoringService.CalculateGeographicScore(
                            job, technician);

                        var geographicMatch = _scoringService.CalculateGeographicMatch(
                            job, technician);

                        var distance = _scoringService.CalculateDistance(
                            job.Latitude, job.Longitude,
                            technician.Latitude, technician.Longitude);

                        var travelTime = _scoringService.EstimateTravelTime(distance);

                        var priorityScore = job.Priority?.ToLower() switch
                        {
                            "urgent" => 1.0f,
                            "high" => 0.8f,
                            "normal" => 0.6f,
                            "low" => 0.4f,
                            _ => 0.6f
                        };

                        var mlInput = new MLNetInput
                        {
                            SkillsMatch = skillsScore,
                            Distance = (float)distance,
                            Workload = technician.CurrentWorkload,
                            SlaHistory = (float)technician.HistoricalSlaSuccessRate,
                            TravelTime = (float)travelTime,
                            Priority = priorityScore
                        };

                        var mlScore = await _predictionService.PredictScoreAsync(tenantId, mlInput);

                        var finalScore = CalculateFinalScore(
                            mlScore, skillsScore, distanceScore,
                            availabilityScore, slaScore, workloadScore, geographicScore);

                        var recommendation = new TechnicianRecommendation
                        {
                            TechnicianId = technician.TechnicianId,
                            Name = technician.Name,
                            Score = finalScore,
                            SkillsScore = skillsScore,
                            DistanceScore = distanceScore,
                            AvailabilityScore = availabilityScore,
                            SlaScore = slaScore,
                            GeographicScore = geographicScore,
                            EstimatedTravelTime = travelTime,
                            Distance = distance,
                            GeographicMatch = geographicMatch
                        };

                        recommendations.Add(recommendation);

                        _logger.LogDebug("Scored technician {TechnicianId}: {Score:F3}",
                            technician.TechnicianId, finalScore);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scoring technician {TechnicianId}",
                            technician.TechnicianId);
                    }
                }

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scoring technicians for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<bool> ValidateTenantAccessAsync(string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(tenantId))
                    return false;

                if (tenantId == "demo-tenant")
                    return true;

                return await _predictionService.IsModelLoadedAsync(tenantId) ||
                       await _predictionService.LoadTenantModelAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating tenant access for {TenantId}", tenantId);
                return false;
            }
        }

        private static double CalculateFinalScore(
            float mlScore,
            float skillsScore,
            float distanceScore,
            float availabilityScore,
            float slaScore,
            float workloadScore,
            float geographicScore)
        {
            const double mlWeight = 0.35;
            const double skillsWeight = 0.25;
            const double distanceWeight = 0.1;
            const double geographicWeight = 0.15;
            const double availabilityWeight = 0.1;
            const double slaWeight = 0.03;
            const double workloadWeight = 0.02;

            var weightedScore =
                (mlScore * mlWeight) +
                (skillsScore * skillsWeight) +
                (distanceScore * distanceWeight) +
                (geographicScore * geographicWeight) +
                (availabilityScore * availabilityWeight) +
                (slaScore * slaWeight) +
                (workloadScore * workloadWeight);

            return Math.Max(0.0, Math.Min(1.0, weightedScore));
        }
    }
}