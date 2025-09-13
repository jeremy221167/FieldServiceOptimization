using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;
using System.Text.Json;

namespace ML.Services.Implementation
{
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleMapsService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleMapsService(
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _apiKey = _configuration.GetValue<string>("GoogleMaps:ApiKey") ?? string.Empty;
        }

        public async Task<TrafficAwareRoute> GetDirectionsAsync(double originLat, double originLng,
            double destLat, double destLng, RoutePreferences? preferences = null)
        {
            try
            {
                _logger.LogInformation("Getting directions from {OriginLat},{OriginLng} to {DestLat},{DestLng}",
                    originLat, originLng, destLat, destLng);

                // In production, this would call Google Directions API:
                /*
                var url = $"https://maps.googleapis.com/maps/api/directions/json?" +
                         $"origin={originLat},{originLng}&" +
                         $"destination={destLat},{destLng}&" +
                         $"departure_time=now&" +
                         $"traffic_model=best_guess&" +
                         $"key={_apiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var directionsResult = JsonSerializer.Deserialize<GoogleDirectionsResponse>(response);
                */

                // For demo purposes, simulate Google Directions API response
                var route = await SimulateGoogleDirectionsAsync(originLat, originLng, destLat, destLng, preferences);

                _logger.LogInformation("Generated route: {Distance}m, {Duration}s (traffic: {TrafficDuration}s)",
                    route.DistanceMeters, route.DurationSeconds, route.DurationInTrafficSeconds);

                return route;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directions from Google Maps");
                throw;
            }
        }

        public async Task<List<RouteWaypoint>> GeocodeAddressAsync(string address)
        {
            try
            {
                _logger.LogInformation("Geocoding address: {Address}", address);

                // In production, this would call Google Geocoding API:
                /*
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?" +
                         $"address={Uri.EscapeDataString(address)}&" +
                         $"key={_apiKey}";

                var response = await _httpClient.GetStringAsync(url);
                var geocodeResult = JsonSerializer.Deserialize<GoogleGeocodeResponse>(response);
                */

                // For demo purposes, return simulated NYC coordinates
                await Task.Delay(100);
                return new List<RouteWaypoint>
                {
                    new RouteWaypoint
                    {
                        Latitude = 40.7128 + (Random.Shared.NextDouble() - 0.5) * 0.1,
                        Longitude = -74.0060 + (Random.Shared.NextDouble() - 0.5) * 0.1,
                        Address = address,
                        Type = WaypointType.Destination
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address: {Address}", address);
                throw;
            }
        }

        public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                // In production, this would call Google Reverse Geocoding API
                await Task.Delay(50);
                return $"Address near {latitude:F4}, {longitude:F4}, New York, NY";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverse geocoding {Lat},{Lng}", latitude, longitude);
                return $"{latitude:F4}, {longitude:F4}";
            }
        }

        public async Task<List<TrafficIncident>> GetRoadClosuresAsync(double centerLat, double centerLng, double radiusKm)
        {
            try
            {
                _logger.LogInformation("Getting road closures near {Lat},{Lng} within {Radius}km",
                    centerLat, centerLng, radiusKm);

                // In production, this would integrate with:
                // - Google Traffic API
                // - Government road closure APIs
                // - Real-time traffic incident feeds

                await Task.Delay(200);
                return GenerateSimulatedTrafficIncidents(centerLat, centerLng, radiusKm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting road closures");
                return new List<TrafficIncident>();
            }
        }

        public async Task<MapVisualization> GenerateMapVisualizationAsync(
            List<TechnicianLocation> technicians,
            List<JobLocation> jobs,
            List<TrafficAwareRoute> routes)
        {
            try
            {
                var visualization = new MapVisualization
                {
                    MapId = Guid.NewGuid().ToString(),
                    Center = CalculateMapCenter(technicians, jobs),
                    ZoomLevel = CalculateOptimalZoom(technicians, jobs),
                    ShowTrafficLayer = true
                };

                // Convert technicians to markers
                foreach (var tech in technicians)
                {
                    visualization.TechnicianMarkers.Add(new TechnicianMarker
                    {
                        TechnicianId = tech.TechnicianId,
                        Name = $"Technician {tech.TechnicianId}",
                        Latitude = tech.Latitude,
                        Longitude = tech.Longitude,
                        Status = DetermineStatusFromSpeed(tech.SpeedKmh),
                        Heading = tech.Heading,
                        IconUrl = await GenerateTechnicianIconUrlAsync(tech)
                    });
                }

                // Convert jobs to markers
                foreach (var job in jobs)
                {
                    visualization.JobMarkers.Add(new JobMarker
                    {
                        JobId = job.JobId,
                        Latitude = job.Latitude,
                        Longitude = job.Longitude,
                        Priority = job.Priority,
                        ServiceType = "Service Call",
                        IsEmergency = job.Priority == "Urgent"
                    });
                }

                // Convert routes to polylines
                foreach (var route in routes)
                {
                    visualization.Routes.Add(new RoutePolyline
                    {
                        RouteId = route.RouteId,
                        EncodedPolyline = route.PolylineEncoded,
                        Color = GetRouteColor(route.TrafficCondition),
                        Weight = route.TrafficCondition == TrafficCondition.Severe ? 5 : 3,
                        IsActive = true
                    });
                }

                return visualization;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating map visualization");
                throw;
            }
        }

        private async Task<TrafficAwareRoute> SimulateGoogleDirectionsAsync(
            double originLat, double originLng, double destLat, double destLng, RoutePreferences? preferences)
        {
            await Task.Delay(100); // Simulate API call

            var distance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
            var baseDuration = (distance / 45.0) * 3600; // 45 km/h average speed

            // Simulate traffic impact
            var trafficMultiplier = Random.Shared.NextDouble() * 0.5 + 1.0; // 1.0 to 1.5x
            var trafficCondition = DetermineTrafficCondition(trafficMultiplier);

            var route = new TrafficAwareRoute
            {
                RouteId = Guid.NewGuid().ToString(),
                DistanceMeters = distance * 1000,
                DurationSeconds = baseDuration,
                DurationInTrafficSeconds = baseDuration * trafficMultiplier,
                TrafficCondition = trafficCondition,
                PolylineEncoded = GenerateSimulatedPolyline(originLat, originLng, destLat, destLng),
                TrafficIncidents = GenerateRouteIncidents(originLat, originLng, destLat, destLng),
                LastUpdated = DateTime.UtcNow
            };

            route.Waypoints.Add(new RouteWaypoint
            {
                Latitude = originLat,
                Longitude = originLng,
                Type = WaypointType.Origin
            });

            route.Waypoints.Add(new RouteWaypoint
            {
                Latitude = destLat,
                Longitude = destLng,
                Type = WaypointType.Destination
            });

            return route;
        }

        private List<TrafficIncident> GenerateSimulatedTrafficIncidents(double centerLat, double centerLng, double radiusKm)
        {
            var incidents = new List<TrafficIncident>();
            var incidentCount = Random.Shared.Next(0, 5);

            for (int i = 0; i < incidentCount; i++)
            {
                var angle = Random.Shared.NextDouble() * 2 * Math.PI;
                var distance = Random.Shared.NextDouble() * radiusKm;

                var lat = centerLat + (distance / 111.0) * Math.Cos(angle);
                var lng = centerLng + (distance / (111.0 * Math.Cos(centerLat * Math.PI / 180))) * Math.Sin(angle);

                var incidentTypes = Enum.GetValues<IncidentType>();
                var severities = Enum.GetValues<IncidentSeverity>();

                incidents.Add(new TrafficIncident
                {
                    IncidentId = Guid.NewGuid().ToString(),
                    Type = incidentTypes[Random.Shared.Next(incidentTypes.Length)],
                    Latitude = lat,
                    Longitude = lng,
                    Severity = severities[Random.Shared.Next(severities.Length)],
                    Description = GenerateIncidentDescription(),
                    StartTime = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 120)),
                    EstimatedEndTime = DateTime.UtcNow.AddMinutes(Random.Shared.Next(30, 240)),
                    ImpactRadiusMeters = Random.Shared.Next(100, 1000)
                });
            }

            return incidents;
        }

        private List<TrafficIncident> GenerateRouteIncidents(double originLat, double originLng, double destLat, double destLng)
        {
            var incidents = new List<TrafficIncident>();

            // 30% chance of having incidents on route
            if (Random.Shared.NextDouble() < 0.3)
            {
                var incidentCount = Random.Shared.Next(1, 3);
                for (int i = 0; i < incidentCount; i++)
                {
                    var progress = Random.Shared.NextDouble();
                    var lat = originLat + (destLat - originLat) * progress;
                    var lng = originLng + (destLng - originLng) * progress;

                    incidents.Add(new TrafficIncident
                    {
                        IncidentId = Guid.NewGuid().ToString(),
                        Type = IncidentType.Congestion,
                        Latitude = lat,
                        Longitude = lng,
                        Severity = IncidentSeverity.Medium,
                        Description = "Heavy traffic congestion",
                        StartTime = DateTime.UtcNow.AddMinutes(-30),
                        ImpactRadiusMeters = 500
                    });
                }
            }

            return incidents;
        }

        private string GenerateIncidentDescription()
        {
            var descriptions = new[]
            {
                "Road construction causing delays",
                "Vehicle accident blocking lane",
                "Heavy traffic congestion",
                "Road closure for maintenance",
                "Weather-related delays",
                "Emergency vehicle activity"
            };

            return descriptions[Random.Shared.Next(descriptions.Length)];
        }

        private async Task<string> GenerateTechnicianIconUrlAsync(TechnicianLocation location)
        {
            // In production, this would generate custom van icons based on:
            // - Technician status (available, busy, en route)
            // - Vehicle type
            // - Company branding

            await Task.CompletedTask;

            var status = DetermineStatusFromSpeed(location.SpeedKmh);
            return status switch
            {
                "Moving" => "/images/van-moving.png",
                "Stationary" => "/images/van-stationary.png",
                _ => "/images/van-available.png"
            };
        }

        private string DetermineStatusFromSpeed(double speedKmh) => speedKmh switch
        {
            > 5 => "Moving",
            <= 5 and > 0 => "Slow",
            _ => "Stationary"
        };

        private TrafficCondition DetermineTrafficCondition(double multiplier) => multiplier switch
        {
            < 1.1 => TrafficCondition.Clear,
            < 1.2 => TrafficCondition.Light,
            < 1.35 => TrafficCondition.Moderate,
            < 1.5 => TrafficCondition.Heavy,
            _ => TrafficCondition.Severe
        };

        private string GetRouteColor(TrafficCondition condition) => condition switch
        {
            TrafficCondition.Clear => "#4CAF50",      // Green
            TrafficCondition.Light => "#8BC34A",     // Light Green
            TrafficCondition.Moderate => "#FF9800",  // Orange
            TrafficCondition.Heavy => "#FF5722",     // Red Orange
            TrafficCondition.Severe => "#F44336",    // Red
            _ => "#2196F3"                            // Blue (default)
        };

        private MapCenter CalculateMapCenter(List<TechnicianLocation> technicians, List<JobLocation> jobs)
        {
            var allLatitudes = technicians.Select(t => t.Latitude).Concat(jobs.Select(j => j.Latitude)).ToList();
            var allLongitudes = technicians.Select(t => t.Longitude).Concat(jobs.Select(j => j.Longitude)).ToList();

            if (!allLatitudes.Any())
                return new MapCenter { Latitude = 40.7128, Longitude = -74.0060 }; // Default to NYC

            return new MapCenter
            {
                Latitude = allLatitudes.Average(),
                Longitude = allLongitudes.Average()
            };
        }

        private int CalculateOptimalZoom(List<TechnicianLocation> technicians, List<JobLocation> jobs)
        {
            var allLatitudes = technicians.Select(t => t.Latitude).Concat(jobs.Select(j => j.Latitude)).ToList();
            var allLongitudes = technicians.Select(t => t.Longitude).Concat(jobs.Select(j => j.Longitude)).ToList();

            if (!allLatitudes.Any()) return 10;

            var latRange = allLatitudes.Max() - allLatitudes.Min();
            var lngRange = allLongitudes.Max() - allLongitudes.Min();
            var maxRange = Math.Max(latRange, lngRange);

            return maxRange switch
            {
                < 0.01 => 15,    // Very close
                < 0.05 => 13,    // Close
                < 0.1 => 11,     // Medium
                < 0.5 => 9,      // Wide
                _ => 7           // Very wide
            };
        }

        private double CalculateHaversineDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double earthRadiusKm = 6371.0;

            var lat1Rad = lat1 * Math.PI / 180;
            var lng1Rad = lng1 * Math.PI / 180;
            var lat2Rad = lat2 * Math.PI / 180;
            var lng2Rad = lng2 * Math.PI / 180;

            var deltaLat = lat2Rad - lat1Rad;
            var deltaLng = lng2Rad - lng1Rad;

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private string GenerateSimulatedPolyline(double originLat, double originLng, double destLat, double destLng)
        {
            // In production, this would be the actual encoded polyline from Google Directions API
            // For demo purposes, return a simple encoded polyline representation
            return $"polyline_{originLat:F4}_{originLng:F4}_to_{destLat:F4}_{destLng:F4}";
        }
    }
}