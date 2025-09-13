using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class RouteOptimizationService : IRouteOptimizationService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ITrafficAwareRoutingService _trafficRoutingService;
        private readonly ILogger<RouteOptimizationService> _logger;

        public RouteOptimizationService(
            IGoogleMapsService googleMapsService,
            ITrafficAwareRoutingService trafficRoutingService,
            ILogger<RouteOptimizationService> logger)
        {
            _googleMapsService = googleMapsService;
            _trafficRoutingService = trafficRoutingService;
            _logger = logger;
        }

        public async Task<List<TrafficAwareRoute>> OptimizeMultipleRoutesAsync(List<RouteOptimizationRequest> requests)
        {
            try
            {
                _logger.LogInformation("Optimizing {Count} routes", requests.Count);

                var routes = new List<TrafficAwareRoute>();
                var tasks = requests.Select(request => _trafficRoutingService.GetOptimalRouteAsync(request));

                var results = await Task.WhenAll(tasks);
                routes.AddRange(results);

                // Sort by emergency priority, then by travel time
                routes = routes.OrderBy(r => !requests.First(req => req.TechnicianId == GetTechnicianIdFromRoute(r)).IsEmergency)
                              .ThenBy(r => r.DurationInTrafficSeconds)
                              .ToList();

                _logger.LogInformation("Optimized {Count} routes successfully", routes.Count);
                return routes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing multiple routes");
                throw;
            }
        }

        public async Task<TrafficAwareRoute> OptimizeEmergencyRouteAsync(RouteOptimizationRequest emergencyRequest)
        {
            try
            {
                _logger.LogInformation("Optimizing emergency route for technician {TechnicianId}", emergencyRequest.TechnicianId);

                // Mark as emergency for special handling
                emergencyRequest.IsEmergency = true;
                emergencyRequest.Preferences = GetEmergencyRoutePreferences();

                var route = await _trafficRoutingService.GetOptimalRouteAsync(emergencyRequest);

                _logger.LogInformation("Emergency route optimized: {Duration} minutes", route.DurationInTrafficSeconds / 60.0);
                return route;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing emergency route for technician {TechnicianId}", emergencyRequest.TechnicianId);
                throw;
            }
        }

        public async Task<bool> UpdateRouteForTrafficAsync(string routeId, List<TrafficIncident> newIncidents)
        {
            try
            {
                _logger.LogInformation("Updating route {RouteId} for {Count} new traffic incidents", routeId, newIncidents.Count);

                // Check if any incidents significantly affect the route
                bool routeAffected = await _trafficRoutingService.IsRouteAffectedByTrafficAsync(routeId);

                if (routeAffected)
                {
                    _logger.LogWarning("Route {RouteId} is significantly affected by traffic incidents", routeId);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating route {RouteId} for traffic", routeId);
                return false;
            }
        }

        public async Task<List<TechnicianLocation>> GetOptimalTechnicianPositionsAsync(List<JobLocation> upcomingJobs)
        {
            try
            {
                _logger.LogInformation("Calculating optimal technician positions for {Count} upcoming jobs", upcomingJobs.Count);

                // For demo purposes, simulate optimal positioning
                // In production, this would use complex algorithms considering:
                // - Job locations and priorities
                // - Historical demand patterns
                // - Traffic patterns
                // - Technician skills and availability

                var optimalPositions = new List<TechnicianLocation>();

                foreach (var job in upcomingJobs.Take(5)) // Process top 5 priority jobs
                {
                    // Simulate optimal positioning near high-priority jobs
                    var position = new TechnicianLocation
                    {
                        TechnicianId = $"OPT-{Random.Shared.Next(1000, 9999)}",
                        Latitude = job.Latitude + (Random.Shared.NextDouble() - 0.5) * 0.01,
                        Longitude = job.Longitude + (Random.Shared.NextDouble() - 0.5) * 0.01,
                        Heading = Random.Shared.Next(0, 360),
                        SpeedKmh = 0, // Stationary at optimal position
                        Timestamp = DateTime.UtcNow,
                        Accuracy = LocationAccuracy.High
                    };

                    optimalPositions.Add(position);
                }

                _logger.LogInformation("Calculated {Count} optimal technician positions", optimalPositions.Count);
                return optimalPositions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating optimal technician positions");
                return new List<TechnicianLocation>();
            }
        }

        private RoutePreferences GetEmergencyRoutePreferences()
        {
            return new RoutePreferences
            {
                AvoidTolls = false,           // Use all roads for emergency
                AvoidHighways = false,       // Highways are often fastest
                AvoidFerries = true,         // Ferries cause delays
                PreferFastestRoute = true,   // Prioritize speed over distance
                EmergencyVehicleMode = true, // Special emergency routing
                MaxDetourMinutes = 2.0       // Minimize detours for emergencies
            };
        }

        private string GetTechnicianIdFromRoute(TrafficAwareRoute route)
        {
            // Extract technician ID from route metadata or return default
            // In production, this would be stored in the route object
            return route.RouteId.Split('_').FirstOrDefault() ?? "Unknown";
        }
    }
}