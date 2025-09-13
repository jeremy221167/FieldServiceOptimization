using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class GeographicMatchingService : IGeographicMatchingService
    {
        private readonly ILogger<GeographicMatchingService> _logger;

        public GeographicMatchingService(ILogger<GeographicMatchingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public GeographicMatch CalculateGeographicMatch(JobRequest job, Technician technician)
        {
            try
            {
                if (job == null || technician?.CoverageArea == null)
                    return new GeographicMatch { CoverageType = "OutOfRange" };

                var coverage = technician.CoverageArea;
                var match = new GeographicMatch();

                var distance = CalculateDistance(job.Latitude, job.Longitude,
                    technician.Latitude, technician.Longitude);
                match.DistanceFromServiceCenter = distance;

                match.IsWithinServiceRadius = IsWithinServiceRadius(
                    job.Latitude, job.Longitude,
                    technician.Latitude, technician.Longitude,
                    coverage.ServiceRadiusKm);

                match.IsInPrimaryCity = IsInPrimaryCity(job.Location ?? "", coverage.PrimaryCities);
                match.IsInSecondaryCity = IsInSecondaryCity(job.Location ?? "", coverage.SecondaryCities);

                var jobPostal = ExtractPostalCode(job.Location ?? "");
                match.IsInPostalCode = IsInPostalCode(jobPostal, coverage.PostalCodes);

                match.MatchingRegions = GetMatchingRegions(job.Latitude, job.Longitude, coverage.PreferredRegions);

                match.CoverageType = DetermineCoverageType(match, coverage, distance);

                return match;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating geographic match for job {JobId}", job?.JobId);
                return new GeographicMatch { CoverageType = "OutOfRange" };
            }
        }

        public bool IsWithinServiceRadius(double jobLat, double jobLng, double techLat, double techLng, double serviceRadiusKm)
        {
            try
            {
                var distance = CalculateDistance(jobLat, jobLng, techLat, techLng);
                return distance <= serviceRadiusKm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service radius");
                return false;
            }
        }

        public bool IsInPrimaryCity(string jobLocation, List<string> primaryCities)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobLocation) || primaryCities?.Any() != true)
                    return false;

                var jobLocationLower = jobLocation.ToLowerInvariant();
                return primaryCities.Any(city =>
                    !string.IsNullOrWhiteSpace(city) &&
                    jobLocationLower.Contains(city.ToLowerInvariant()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking primary city match");
                return false;
            }
        }

        public bool IsInSecondaryCity(string jobLocation, List<string> secondaryCities)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobLocation) || secondaryCities?.Any() != true)
                    return false;

                var jobLocationLower = jobLocation.ToLowerInvariant();
                return secondaryCities.Any(city =>
                    !string.IsNullOrWhiteSpace(city) &&
                    jobLocationLower.Contains(city.ToLowerInvariant()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking secondary city match");
                return false;
            }
        }

        public bool IsInPostalCode(string jobLocation, List<string> postalCodes)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jobLocation) || postalCodes?.Any() != true)
                    return false;

                var jobPostal = ExtractPostalCode(jobLocation);
                return !string.IsNullOrEmpty(jobPostal) && postalCodes.Contains(jobPostal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking postal code match");
                return false;
            }
        }

        public List<string> GetMatchingRegions(double jobLat, double jobLng, List<GeographicRegion> regions)
        {
            var matchingRegions = new List<string>();

            try
            {
                if (regions?.Any() != true)
                    return matchingRegions;

                foreach (var region in regions.Where(r => r != null))
                {
                    var regionDistance = CalculateDistance(jobLat, jobLng,
                        region.CenterLatitude, region.CenterLongitude);

                    if (regionDistance <= region.RadiusKm)
                    {
                        matchingRegions.Add(region.RegionName ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding matching regions");
            }

            return matchingRegions;
        }

        public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance");
                return double.MaxValue;
            }
        }

        private static string DetermineCoverageType(GeographicMatch match, GeographicCoverage coverage, double distance)
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
            try
            {
                if (string.IsNullOrWhiteSpace(location))
                    return string.Empty;

                var parts = location.Split(new[] { ' ', ',', '-' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.Length >= 5 && part.All(char.IsDigit))
                        return part;
                    if (part.Length == 6 && part.Take(3).All(char.IsLetter) &&
                        part.Skip(3).All(char.IsDigit))
                        return part.ToUpperInvariant();
                }
            }
            catch
            {
                // Silent fail for postal code extraction
            }

            return string.Empty;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}