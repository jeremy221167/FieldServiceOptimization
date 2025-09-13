using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class TrafficAwareRoutingService : ITrafficAwareRoutingService
    {
        private readonly ILogger<TrafficAwareRoutingService> _logger;

        public TrafficAwareRoutingService(ILogger<TrafficAwareRoutingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RouteOptimization> OptimizeRouteAsync(double startLat, double startLng, double endLat, double endLng)
        {
            try
            {
                var route = new RouteOptimization
                {
                    RouteId = Guid.NewGuid().ToString(),
                    Points = new List<RoutePoint>
                    {
                        new RoutePoint
                        {
                            Latitude = startLat,
                            Longitude = startLng,
                            Order = 0,
                            EstimatedArrival = DateTime.UtcNow
                        },
                        new RoutePoint
                        {
                            Latitude = endLat,
                            Longitude = endLng,
                            Order = 1
                        }
                    }
                };

                route.TotalDistanceKm = CalculateDistance(startLat, startLng, endLat, endLng);

                var incidents = await GetActiveIncidentsAsync((startLat + endLat) / 2, (startLng + endLng) / 2, route.TotalDistanceKm + 10);
                route.TrafficIncidents = incidents;

                var baseTravelTime = (route.TotalDistanceKm / 60.0) * 60.0; // 60 km/h base speed
                route.EstimatedTravelTimeMinutes = await CalculateTrafficAwareTravelTimeAsync(route.TotalDistanceKm, incidents);

                route.Points[1].EstimatedArrival = DateTime.UtcNow.AddMinutes(route.EstimatedTravelTimeMinutes);
                route.IsOptimized = true;

                return route;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing route from ({StartLat},{StartLng}) to ({EndLat},{EndLng})",
                    startLat, startLng, endLat, endLng);

                return new RouteOptimization
                {
                    RouteId = Guid.NewGuid().ToString(),
                    Points = new List<RoutePoint>(),
                    TotalDistanceKm = 0,
                    EstimatedTravelTimeMinutes = 0,
                    IsOptimized = false,
                    TrafficIncidents = new List<TrafficIncident>()
                };
            }
        }

        public async Task<List<TrafficIncident>> GetActiveIncidentsAsync(double lat, double lng, double radiusKm)
        {
            try
            {
                await Task.Delay(100); // Simulate async call

                var incidents = new List<TrafficIncident>();
                var random = new Random();

                // Simulate some traffic incidents based on location
                if (random.NextDouble() < 0.3) // 30% chance of incidents
                {
                    var incidentCount = random.Next(1, 4);
                    for (int i = 0; i < incidentCount; i++)
                    {
                        incidents.Add(new TrafficIncident
                        {
                            IncidentId = Guid.NewGuid().ToString(),
                            Type = (IncidentType)random.Next(1, 7),
                            Severity = (IncidentSeverity)random.Next(1, 5),
                            Description = GenerateIncidentDescription(),
                            Latitude = lat + (random.NextDouble() - 0.5) * (radiusKm / 111.0), // Rough lat conversion
                            Longitude = lng + (random.NextDouble() - 0.5) * (radiusKm / 111.0),
                            ReportedAt = DateTime.UtcNow.AddMinutes(-random.Next(0, 120)),
                            ExpectedDurationMinutes = random.Next(15, 180),
                            IsActive = true
                        });
                    }
                }

                return incidents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active incidents near ({Lat},{Lng})", lat, lng);
                return new List<TrafficIncident>();
            }
        }

        public async Task<double> CalculateTrafficAwareTravelTimeAsync(double distance, List<TrafficIncident> incidents)
        {
            try
            {
                await Task.Delay(50); // Simulate async processing

                var baseTimeMinutes = (distance / 60.0) * 60.0; // 60 km/h base speed
                var trafficMultiplier = 1.0;

                if (incidents?.Any() == true)
                {
                    foreach (var incident in incidents)
                    {
                        var impactMultiplier = incident.Severity switch
                        {
                            IncidentSeverity.Low => 1.1,
                            IncidentSeverity.Medium => 1.25,
                            IncidentSeverity.High => 1.5,
                            IncidentSeverity.Critical => 2.0,
                            _ => 1.0
                        };

                        var typeMultiplier = incident.Type switch
                        {
                            IncidentType.Accident => 1.3,
                            IncidentType.RoadClosure => 1.8,
                            IncidentType.Construction => 1.2,
                            IncidentType.Weather => 1.4,
                            IncidentType.Event => 1.1,
                            _ => 1.0
                        };

                        trafficMultiplier += (impactMultiplier * typeMultiplier - 1.0) * 0.3; // Reduce impact
                    }
                }

                // Add some variability for realistic traffic patterns
                var timeOfDayFactor = GetTimeOfDayTrafficFactor();
                trafficMultiplier *= timeOfDayFactor;

                return Math.Max(baseTimeMinutes * 0.8, baseTimeMinutes * trafficMultiplier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating traffic-aware travel time");
                return (distance / 60.0) * 60.0; // Fallback to base calculation
            }
        }

        private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
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

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        private static string GenerateIncidentDescription()
        {
            var descriptions = new[]
            {
                "Heavy traffic due to accident",
                "Road construction causing delays",
                "Weather-related slow traffic",
                "Special event affecting traffic flow",
                "Lane closure for maintenance",
                "Traffic signal malfunction",
                "Emergency vehicle activity"
            };

            var random = new Random();
            return descriptions[random.Next(descriptions.Length)];
        }

        private static double GetTimeOfDayTrafficFactor()
        {
            var hour = DateTime.Now.Hour;

            return hour switch
            {
                >= 7 and <= 9 => 1.4,    // Morning rush
                >= 17 and <= 19 => 1.5,  // Evening rush
                >= 12 and <= 14 => 1.2,  // Lunch time
                >= 22 or <= 5 => 0.8,    // Late night/early morning
                _ => 1.1                  // Normal traffic
            };
        }
    }
}