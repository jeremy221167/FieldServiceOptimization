using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class RecommendationScoring : IRecommendationScoring
    {
        private readonly ILogger<RecommendationScoring> _logger;

        public RecommendationScoring(ILogger<RecommendationScoring> logger)
        {
            _logger = logger;
        }

        public float CalculateSkillsScore(Dictionary<string, string> requiredSkills, Dictionary<string, int> technicianSkills)
        {
            try
            {
                if (requiredSkills == null || !requiredSkills.Any())
                    return 1.0f;

                if (technicianSkills == null || !technicianSkills.Any())
                    return 0.0f;

                var totalRequiredSkills = requiredSkills.Count;
                var matchedSkills = 0f;

                foreach (var required in requiredSkills)
                {
                    if (technicianSkills.TryGetValue(required.Key, out var level))
                    {
                        if (int.TryParse(required.Value, out var requiredLevel))
                        {
                            if (level >= requiredLevel)
                                matchedSkills += 1.0f;
                            else
                                matchedSkills += Math.Max(0f, (float)level / requiredLevel * 0.7f);
                        }
                        else
                        {
                            matchedSkills += 1.0f;
                        }
                    }
                }

                return Math.Min(1.0f, matchedSkills / totalRequiredSkills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating skills score");
                return 0.0f;
            }
        }

        public float CalculateDistanceScore(double jobLat, double jobLng, double techLat, double techLng)
        {
            try
            {
                var distance = CalculateDistance(jobLat, jobLng, techLat, techLng);

                return distance switch
                {
                    <= 5 => 1.0f,
                    <= 10 => 0.9f,
                    <= 20 => 0.7f,
                    <= 50 => 0.5f,
                    <= 100 => 0.3f,
                    _ => 0.1f
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance score");
                return 0.0f;
            }
        }

        public float CalculateAvailabilityScore(Technician technician, DateTime jobScheduledDate)
        {
            try
            {
                if (!technician.IsAvailable)
                    return 0.0f;

                if (jobScheduledDate < technician.AvailableFrom || jobScheduledDate > technician.AvailableUntil)
                    return 0.1f;

                var timeUntilJob = (jobScheduledDate - DateTime.UtcNow).TotalHours;

                return timeUntilJob switch
                {
                    >= 24 => 1.0f,
                    >= 12 => 0.9f,
                    >= 6 => 0.8f,
                    >= 2 => 0.6f,
                    >= 1 => 0.4f,
                    _ => 0.2f
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating availability score");
                return 0.0f;
            }
        }

        public float CalculateSlaScore(double historicalSuccessRate)
        {
            try
            {
                return Math.Max(0.0f, Math.Min(1.0f, (float)historicalSuccessRate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating SLA score");
                return 0.5f;
            }
        }

        public float CalculateWorkloadScore(int currentWorkload)
        {
            try
            {
                return currentWorkload switch
                {
                    0 => 1.0f,
                    1 => 0.9f,
                    2 => 0.7f,
                    3 => 0.5f,
                    4 => 0.3f,
                    _ => 0.1f
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating workload score");
                return 0.5f;
            }
        }

        public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371.0;

            var lat1Rad = DegreesToRadians(lat1);
            var lng1Rad = DegreesToRadians(lng1);
            var lat2Rad = DegreesToRadians(lat2);
            var lng2Rad = DegreesToRadians(lng2);

            var deltaLat = lat2Rad - lat1Rad;
            var deltaLng = lng2Rad - lng1Rad;

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        public float CalculateGeographicScore(JobRequest job, Technician technician)
        {
            try
            {
                var geoMatch = CalculateGeographicMatch(job, technician);
                var coverage = technician.CoverageArea;

                if (!coverage.IsWillingToTravel)
                {
                    return geoMatch.IsWithinServiceRadius ? 1.0f : 0.0f;
                }

                var score = 0.0f;

                if (geoMatch.IsInPrimaryCity) score += 1.0f;
                else if (geoMatch.IsInSecondaryCity) score += 0.8f;
                else if (geoMatch.IsInPostalCode) score += 0.7f;
                else if (geoMatch.IsWithinServiceRadius) score += 0.6f;

                foreach (var region in geoMatch.MatchingRegions)
                {
                    var matchingRegion = coverage.PreferredRegions
                        .FirstOrDefault(r => r.RegionName.Equals(region, StringComparison.OrdinalIgnoreCase));
                    if (matchingRegion != null)
                    {
                        score += (6f - matchingRegion.Priority) / 10f;
                    }
                }

                var distance = geoMatch.DistanceFromServiceCenter;
                if (distance <= coverage.ServiceRadiusKm)
                {
                    score += 0.5f * (1f - (float)(distance / coverage.ServiceRadiusKm));
                }
                else if (distance <= coverage.MaxTravelDistanceKm)
                {
                    score += 0.2f * (1f - (float)((distance - coverage.ServiceRadiusKm) /
                        (coverage.MaxTravelDistanceKm - coverage.ServiceRadiusKm)));
                }

                return Math.Max(0.0f, Math.Min(1.0f, score));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating geographic score");
                return 0.5f;
            }
        }

        public GeographicMatch CalculateGeographicMatch(JobRequest job, Technician technician)
        {
            try
            {
                var coverage = technician.CoverageArea;
                var match = new GeographicMatch();

                var distance = CalculateDistance(job.Latitude, job.Longitude,
                    technician.Latitude, technician.Longitude);
                match.DistanceFromServiceCenter = distance;

                match.IsWithinServiceRadius = distance <= coverage.ServiceRadiusKm;

                var jobLocation = job.Location?.ToLowerInvariant() ?? "";
                match.IsInPrimaryCity = coverage.PrimaryCities.Any(city =>
                    jobLocation.Contains(city.ToLowerInvariant()));
                match.IsInSecondaryCity = coverage.SecondaryCities.Any(city =>
                    jobLocation.Contains(city.ToLowerInvariant()));

                var jobPostal = ExtractPostalCode(jobLocation);
                match.IsInPostalCode = !string.IsNullOrEmpty(jobPostal) &&
                    coverage.PostalCodes.Contains(jobPostal);

                foreach (var region in coverage.PreferredRegions)
                {
                    var regionDistance = CalculateDistance(job.Latitude, job.Longitude,
                        region.CenterLatitude, region.CenterLongitude);

                    if (regionDistance <= region.RadiusKm)
                    {
                        match.MatchingRegions.Add(region.RegionName);
                    }
                }

                match.CoverageType = DetermineCoverageType(match, coverage, distance);

                return match;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating geographic match");
                return new GeographicMatch { CoverageType = "OutOfRange" };
            }
        }

        public double EstimateTravelTime(double distanceKm)
        {
            const double averageSpeedKmh = 45.0;
            return (distanceKm / averageSpeedKmh) * 60.0;
        }

        private static string DetermineCoverageType(GeographicMatch match,
            GeographicCoverage coverage, double distance)
        {
            if (match.IsInPrimaryCity || match.IsInPostalCode)
                return "Primary";

            if (match.IsInSecondaryCity || match.MatchingRegions.Any())
                return "Secondary";

            if (distance <= coverage.ServiceRadiusKm)
                return "Extended";

            return distance <= coverage.MaxTravelDistanceKm ? "Extended" : "OutOfRange";
        }

        private static string ExtractPostalCode(string location)
        {
            var parts = location.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Length >= 5 && part.All(char.IsDigit))
                    return part;
                if (part.Length == 6 && part.Take(3).All(char.IsLetter) &&
                    part.Skip(3).All(char.IsDigit))
                    return part.ToUpperInvariant();
            }
            return string.Empty;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}