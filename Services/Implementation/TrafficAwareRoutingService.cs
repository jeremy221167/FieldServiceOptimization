using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;
using System.Collections.Concurrent;

namespace ML.Services.Implementation
{
    public class TrafficAwareRoutingService : ITrafficAwareRoutingService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ILogger<TrafficAwareRoutingService> _logger;
        private readonly ConcurrentDictionary<string, TrafficAwareRoute> _routeCache;
        private readonly ConcurrentDictionary<string, List<TrafficIncident>> _incidentCache;

        public TrafficAwareRoutingService(
            IGoogleMapsService googleMapsService,
            ILogger<TrafficAwareRoutingService> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
            _routeCache = new ConcurrentDictionary<string, TrafficAwareRoute>();
            _incidentCache = new ConcurrentDictionary<string, List<TrafficIncident>>();
        }

        public async Task<TrafficAwareETAResponse> CalculateTrafficAwareETAAsync(RouteOptimizationRequest request)
        {
            try
            {
                _logger.LogInformation("Calculating traffic-aware ETA for technician {TechnicianId} to job {JobId}",
                    request.TechnicianId, request.Destination.JobId);

                var preferences = request.Preferences;
                if (request.IsEmergency)
                {
                    preferences = GetEmergencyRoutePreferences(request.Preferences);
                }

                var route = await _googleMapsService.GetDirectionsAsync(
                    request.CurrentLocation.Latitude,
                    request.CurrentLocation.Longitude,
                    request.Destination.Latitude,
                    request.Destination.Longitude,
                    preferences);

                // Check for traffic incidents affecting the route
                var affectingIncidents = await GetIncidentsAffectingRouteAsync(route);

                // Calculate alternative routes if incidents are severe
                var fasterAlternative = await CheckForFasterAlternativeAsync(route, affectingIncidents);

                var response = new TrafficAwareETAResponse
                {
                    RouteId = route.RouteId,
                    EstimatedTravelTimeMinutes = route.DurationInTrafficSeconds / 60.0,
                    DistanceKm = route.DistanceMeters / 1000.0,
                    TrafficCondition = route.TrafficCondition,
                    AffectingIncidents = affectingIncidents,
                    CalculatedAt = DateTime.UtcNow,
                    FasterAlternative = fasterAlternative
                };

                // Add warnings based on traffic conditions
                response.Warnings = GenerateTrafficWarnings(route, affectingIncidents, request.IsEmergency);

                // Cache the route for future reference
                _routeCache.TryAdd(route.RouteId, route);

                _logger.LogInformation("Traffic-aware ETA calculated: {ETA} minutes, condition: {Condition}",
                    response.EstimatedTravelTimeMinutes, response.TrafficCondition);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating traffic-aware ETA for technician {TechnicianId}",
                    request.TechnicianId);
                throw;
            }
        }

        public async Task<TrafficAwareRoute> GetOptimalRouteAsync(RouteOptimizationRequest request)
        {
            try
            {
                var cacheKey = GenerateRouteCacheKey(request);

                // Check cache first (valid for 5 minutes)
                if (_routeCache.TryGetValue(cacheKey, out var cachedRoute) &&
                    DateTime.UtcNow - cachedRoute.LastUpdated < TimeSpan.FromMinutes(5))
                {
                    _logger.LogDebug("Returning cached route for {TechnicianId}", request.TechnicianId);
                    return cachedRoute;
                }

                var preferences = request.IsEmergency
                    ? GetEmergencyRoutePreferences(request.Preferences)
                    : request.Preferences;

                var route = await _googleMapsService.GetDirectionsAsync(
                    request.CurrentLocation.Latitude,
                    request.CurrentLocation.Longitude,
                    request.Destination.Latitude,
                    request.Destination.Longitude,
                    preferences);

                // Apply emergency optimizations
                if (request.IsEmergency)
                {
                    route = await OptimizeForEmergencyAsync(route);
                }

                // Update cache
                _routeCache.AddOrUpdate(cacheKey, route, (key, oldValue) => route);

                return route;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimal route for technician {TechnicianId}",
                    request.TechnicianId);
                throw;
            }
        }

        public async Task<List<TrafficIncident>> GetTrafficIncidentsAsync(double centerLat, double centerLng, double radiusKm)
        {
            try
            {
                var cacheKey = $"incidents_{centerLat:F4}_{centerLng:F4}_{radiusKm}";

                // Check cache (valid for 2 minutes)
                if (_incidentCache.TryGetValue(cacheKey, out var cachedIncidents))
                {
                    var cacheAge = DateTime.UtcNow - cachedIncidents.FirstOrDefault()?.StartTime;
                    if (cacheAge < TimeSpan.FromMinutes(2))
                    {
                        return cachedIncidents;
                    }
                }

                var incidents = await _googleMapsService.GetRoadClosuresAsync(centerLat, centerLng, radiusKm);

                // Filter and prioritize incidents
                incidents = incidents
                    .Where(i => IsIncidentRelevant(i))
                    .OrderByDescending(i => GetIncidentPriority(i))
                    .ToList();

                // Update cache
                _incidentCache.AddOrUpdate(cacheKey, incidents, (key, oldValue) => incidents);

                _logger.LogInformation("Found {Count} traffic incidents within {Radius}km of {Lat},{Lng}",
                    incidents.Count, radiusKm, centerLat, centerLng);

                return incidents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting traffic incidents");
                return new List<TrafficIncident>();
            }
        }

        public async Task<bool> IsRouteAffectedByTrafficAsync(string routeId)
        {
            try
            {
                if (!_routeCache.TryGetValue(routeId, out var route))
                {
                    _logger.LogWarning("Route {RouteId} not found in cache", routeId);
                    return false;
                }

                var trafficFactor = route.DurationInTrafficSeconds / route.DurationSeconds;
                return trafficFactor > 1.15; // 15% or more delay indicates traffic impact
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking traffic impact for route {RouteId}", routeId);
                return false;
            }
        }

        public async Task<AlternativeRoute?> FindFasterAlternativeAsync(TrafficAwareRoute currentRoute)
        {
            try
            {
                if (currentRoute.TrafficCondition == TrafficCondition.Clear ||
                    currentRoute.TrafficCondition == TrafficCondition.Light)
                {
                    return null; // No need for alternative if traffic is light
                }

                // In production, this would:
                // 1. Request alternative routes from Google Directions API
                // 2. Compare travel times with current traffic
                // 3. Calculate time savings

                // Simulate finding a faster alternative
                await Task.Delay(100);

                var timeSaved = Random.Shared.Next(5, 20);
                if (timeSaved > 5)
                {
                    return new AlternativeRoute
                    {
                        EstimatedTravelTimeMinutes = (currentRoute.DurationInTrafficSeconds / 60.0) - timeSaved,
                        DistanceKm = (currentRoute.DistanceMeters / 1000.0) + Random.Shared.NextDouble() * 2,
                        Reason = "Avoiding heavy traffic on main route",
                        TimeSavedMinutes = timeSaved
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding faster alternative for route {RouteId}", currentRoute.RouteId);
                return null;
            }
        }

        private async Task<List<TrafficIncident>> GetIncidentsAffectingRouteAsync(TrafficAwareRoute route)
        {
            var affectingIncidents = new List<TrafficIncident>();

            foreach (var incident in route.TrafficIncidents)
            {
                // Check if incident is within impact radius of route
                var isAffecting = await IsIncidentAffectingRouteAsync(route, incident);
                if (isAffecting)
                {
                    affectingIncidents.Add(incident);
                }
            }

            return affectingIncidents;
        }

        private async Task<bool> IsIncidentAffectingRouteAsync(TrafficAwareRoute route, TrafficIncident incident)
        {
            // Simplified check - in production would use route geometry
            foreach (var waypoint in route.Waypoints)
            {
                var distance = CalculateDistance(
                    waypoint.Latitude, waypoint.Longitude,
                    incident.Latitude, incident.Longitude) * 1000; // Convert to meters

                if (distance <= incident.ImpactRadiusMeters)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<AlternativeRoute?> CheckForFasterAlternativeAsync(
            TrafficAwareRoute route, List<TrafficIncident> affectingIncidents)
        {
            var severeIncidents = affectingIncidents
                .Where(i => i.Severity == IncidentSeverity.High || i.Severity == IncidentSeverity.Critical)
                .ToList();

            if (severeIncidents.Any())
            {
                return await FindFasterAlternativeAsync(route);
            }

            return null;
        }

        private RoutePreferences GetEmergencyRoutePreferences(RoutePreferences basePreferences)
        {
            return new RoutePreferences
            {
                AvoidTolls = false,           // Use all roads for emergency
                AvoidHighways = false,       // Highways are often fastest
                AvoidFerries = true,         // Ferries cause delays
                PreferFastestRoute = true,   // Prioritize speed
                EmergencyVehicleMode = true, // Special emergency routing
                MaxDetourMinutes = 5.0       // Minimize detours
            };
        }

        private async Task<TrafficAwareRoute> OptimizeForEmergencyAsync(TrafficAwareRoute route)
        {
            // Emergency optimizations:
            // 1. Apply emergency vehicle speed assumptions
            // 2. Consider emergency lane usage
            // 3. Factor in traffic light preemption

            var emergencySpeedBoost = 1.3; // 30% faster due to emergency status
            route.DurationInTrafficSeconds /= emergencySpeedBoost;

            // Mark as emergency route
            route.TrafficCondition = TrafficCondition.Clear; // Emergency vehicles can bypass most traffic

            _logger.LogInformation("Applied emergency optimizations to route {RouteId}", route.RouteId);

            return route;
        }

        private List<string> GenerateTrafficWarnings(TrafficAwareRoute route,
            List<TrafficIncident> incidents, bool isEmergency)
        {
            var warnings = new List<string>();

            if (route.TrafficCondition >= TrafficCondition.Heavy)
            {
                warnings.Add($"Heavy traffic detected - expect delays");
            }

            foreach (var incident in incidents)
            {
                if (incident.Severity >= IncidentSeverity.High)
                {
                    warnings.Add($"{incident.Type}: {incident.Description}");
                }
            }

            if (isEmergency && warnings.Any())
            {
                warnings.Insert(0, "🚨 EMERGENCY ROUTE - Alternative paths recommended");
            }

            return warnings;
        }

        private bool IsIncidentRelevant(TrafficIncident incident)
        {
            // Filter out old or low-impact incidents
            var age = DateTime.UtcNow - incident.StartTime;
            return age < TimeSpan.FromHours(4) &&
                   incident.Severity != IncidentSeverity.Low &&
                   incident.ImpactRadiusMeters > 50;
        }

        private int GetIncidentPriority(TrafficIncident incident)
        {
            var basePriority = incident.Severity switch
            {
                IncidentSeverity.Critical => 100,
                IncidentSeverity.High => 75,
                IncidentSeverity.Medium => 50,
                IncidentSeverity.Low => 25,
                _ => 10
            };

            var typePriority = incident.Type switch
            {
                IncidentType.RoadClosure => 50,
                IncidentType.Accident => 40,
                IncidentType.Construction => 30,
                IncidentType.Emergency => 45,
                _ => 20
            };

            return basePriority + typePriority;
        }

        private string GenerateRouteCacheKey(RouteOptimizationRequest request)
        {
            return $"route_{request.TechnicianId}_{request.CurrentLocation.Latitude:F4}_{request.CurrentLocation.Longitude:F4}_" +
                   $"{request.Destination.Latitude:F4}_{request.Destination.Longitude:F4}_{request.IsEmergency}";
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
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
    }
}