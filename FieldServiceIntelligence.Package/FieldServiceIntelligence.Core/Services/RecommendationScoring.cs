using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class RecommendationScoring : IRecommendationScoring
    {
        private readonly ILogger<RecommendationScoring> _logger;

        public RecommendationScoring(ILogger<RecommendationScoring> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public double CalculateSkillsScore(Dictionary<string, string> requiredSkills, Dictionary<string, int> technicianSkills)
        {
            try
            {
                if (requiredSkills == null || !requiredSkills.Any())
                    return 1.0;

                if (technicianSkills == null || !technicianSkills.Any())
                    return 0.0;

                var totalRequiredSkills = requiredSkills.Count;
                var matchedSkills = 0.0;

                foreach (var required in requiredSkills)
                {
                    if (technicianSkills.TryGetValue(required.Key, out var level))
                    {
                        if (int.TryParse(required.Value, out var requiredLevel))
                        {
                            if (level >= requiredLevel)
                                matchedSkills += 1.0;
                            else
                                matchedSkills += Math.Max(0.0, (double)level / requiredLevel * 0.7);
                        }
                        else
                        {
                            matchedSkills += 1.0;
                        }
                    }
                }

                return Math.Min(1.0, matchedSkills / totalRequiredSkills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating skills score");
                return 0.0;
            }
        }

        public double CalculateDistanceScore(double distance, double maxDistance = 200.0)
        {
            try
            {
                return distance switch
                {
                    <= 5 => 1.0,
                    <= 10 => 0.9,
                    <= 20 => 0.7,
                    <= 50 => 0.5,
                    <= 100 => 0.3,
                    _ => Math.Max(0.1, 1.0 - (distance / maxDistance))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance score");
                return 0.0;
            }
        }

        public double CalculateAvailabilityScore(Technician technician, DateTime scheduledDate)
        {
            try
            {
                if (technician == null || !technician.IsAvailable)
                    return 0.0;

                if (scheduledDate < technician.AvailableFrom || scheduledDate > technician.AvailableUntil)
                    return 0.1;

                var timeUntilJob = (scheduledDate - DateTime.UtcNow).TotalHours;

                var score = timeUntilJob switch
                {
                    >= 24 => 1.0,
                    >= 12 => 0.9,
                    >= 6 => 0.8,
                    >= 2 => 0.6,
                    >= 1 => 0.4,
                    _ => 0.2
                };

                var workloadPenalty = technician.CurrentWorkload switch
                {
                    0 => 0.0,
                    1 => 0.1,
                    2 => 0.3,
                    3 => 0.5,
                    _ => 0.7
                };

                return Math.Max(0.0, score - workloadPenalty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating availability score");
                return 0.0;
            }
        }

        public double CalculateSlaScore(double historicalSlaRate)
        {
            try
            {
                return Math.Max(0.0, Math.Min(1.0, historicalSlaRate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating SLA score");
                return 0.5;
            }
        }

        public double CalculateGeographicScore(GeographicMatch match)
        {
            try
            {
                if (match == null)
                    return 0.0;

                var score = 0.0;

                if (match.IsInPrimaryCity) score += 1.0;
                else if (match.IsInSecondaryCity) score += 0.8;
                else if (match.IsInPostalCode) score += 0.7;
                else if (match.IsWithinServiceRadius) score += 0.6;

                score += match.MatchingRegions.Count * 0.1;

                var coverageMultiplier = match.CoverageType switch
                {
                    "Primary" => 1.0,
                    "Secondary" => 0.8,
                    "Extended" => 0.6,
                    _ => 0.2
                };

                return Math.Max(0.0, Math.Min(1.0, score * coverageMultiplier));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating geographic score");
                return 0.5;
            }
        }

        public double CalculateOverallScore(double skillsScore, double distanceScore, double availabilityScore,
            double slaScore, double geographicScore, string priority)
        {
            try
            {
                var weights = GetScoreWeights(priority);

                var overallScore =
                    (skillsScore * weights.Skills) +
                    (distanceScore * weights.Distance) +
                    (availabilityScore * weights.Availability) +
                    (slaScore * weights.Sla) +
                    (geographicScore * weights.Geographic);

                var priorityMultiplier = priority?.ToLowerInvariant() switch
                {
                    "emergency" => 1.2,
                    "high" => 1.1,
                    "normal" => 1.0,
                    "low" => 0.9,
                    _ => 1.0
                };

                return Math.Max(0.0, Math.Min(1.0, overallScore * priorityMultiplier));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating overall score");
                return 0.0;
            }
        }

        private static (double Skills, double Distance, double Availability, double Sla, double Geographic) GetScoreWeights(string priority)
        {
            return priority?.ToLowerInvariant() switch
            {
                "emergency" => (0.4, 0.3, 0.2, 0.05, 0.05),
                "high" => (0.3, 0.25, 0.25, 0.1, 0.1),
                "normal" => (0.25, 0.2, 0.2, 0.175, 0.175),
                "low" => (0.2, 0.15, 0.15, 0.25, 0.25),
                _ => (0.25, 0.2, 0.2, 0.175, 0.175)
            };
        }
    }
}