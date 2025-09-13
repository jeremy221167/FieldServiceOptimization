using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class MapVisualizationService : IMapVisualizationService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ITrafficAwareRoutingService _trafficRoutingService;
        private readonly ILogger<MapVisualizationService> _logger;
        private readonly Dictionary<string, MapVisualization> _activeMap = new();

        public MapVisualizationService(
            IGoogleMapsService googleMapsService,
            ITrafficAwareRoutingService trafficRoutingService,
            ILogger<MapVisualizationService> logger)
        {
            _googleMapsService = googleMapsService;
            _trafficRoutingService = trafficRoutingService;
            _logger = logger;
        }

        public async Task<MapVisualization> CreateFleetMapAsync(List<Technician> technicians, List<JobRequest> jobs)
        {
            try
            {
                _logger.LogInformation("Creating fleet map visualization for {TechCount} technicians and {JobCount} jobs",
                    technicians.Count, jobs.Count);

                // Convert technicians to locations
                var techLocations = technicians.Select(t => new TechnicianLocation
                {
                    TechnicianId = t.TechnicianId,
                    Latitude = t.CurrentStatus?.CurrentLatitude ?? t.Latitude,
                    Longitude = t.CurrentStatus?.CurrentLongitude ?? t.Longitude,
                    SpeedKmh = t.CurrentStatus?.Status == "EnRoute" ? 35.0 : 0.0,
                    Heading = Random.Shared.Next(0, 360),
                    Timestamp = DateTime.UtcNow,
                    Accuracy = LocationAccuracy.High
                }).ToList();

                // Convert jobs to locations
                var jobLocations = jobs.Select(j => new JobLocation
                {
                    JobId = j.JobId,
                    Latitude = j.Latitude,
                    Longitude = j.Longitude,
                    Address = j.CustomerName, // Use CustomerName as address fallback
                    Priority = j.Priority,
                    ScheduledTime = DateTime.UtcNow.AddHours(1) // Default scheduled time
                }).ToList();

                // Generate routes for en-route technicians
                var routes = new List<TrafficAwareRoute>();
                foreach (var tech in technicians.Where(t => t.CurrentStatus?.Status == "EnRoute"))
                {
                    var assignedJob = jobs.FirstOrDefault(j => j.JobId == tech.CurrentJobId);
                    if (assignedJob != null)
                    {
                        var route = await _googleMapsService.GetDirectionsAsync(
                            tech.CurrentStatus.CurrentLatitude ?? tech.Latitude,
                            tech.CurrentStatus.CurrentLongitude ?? tech.Longitude,
                            assignedJob.Latitude,
                            assignedJob.Longitude
                        );
                        routes.Add(route);
                    }
                }

                var visualization = await _googleMapsService.GenerateMapVisualizationAsync(
                    techLocations, jobLocations, routes);

                // Cache the visualization
                _activeMap[visualization.MapId] = visualization;

                _logger.LogInformation("Created fleet map {MapId} with {TechMarkers} technician markers and {JobMarkers} job markers",
                    visualization.MapId, visualization.TechnicianMarkers.Count, visualization.JobMarkers.Count);

                return visualization;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fleet map visualization");
                throw;
            }
        }

        public async Task<string> GenerateTechnicianIconAsync(Technician technician, string status)
        {
            try
            {
                // Generate SVG van icon based on technician status and properties
                var baseColor = GetStatusColor(status);
                var skillLevel = technician.Skills?.Count ?? 0;
                var slaRate = 0.85; // Default SLA rate for demo purposes

                // Create enhanced van icon with status indicators
                var iconSvg = GenerateVanIconSvg(baseColor, skillLevel, slaRate);

                // Convert to data URI
                var iconUrl = $"data:image/svg+xml;charset=UTF-8,{Uri.EscapeDataString(iconSvg)}";

                _logger.LogDebug("Generated icon for technician {TechnicianId} with status {Status}",
                    technician.TechnicianId, status);

                await Task.CompletedTask;
                return iconUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating technician icon for {TechnicianId}", technician.TechnicianId);
                return GetDefaultVanIcon();
            }
        }

        public async Task UpdateTechnicianLocationAsync(string mapId, TechnicianLocation location)
        {
            try
            {
                if (!_activeMap.TryGetValue(mapId, out var map))
                {
                    _logger.LogWarning("Map {MapId} not found for location update", mapId);
                    return;
                }

                var marker = map.TechnicianMarkers.FirstOrDefault(m => m.TechnicianId == location.TechnicianId);
                if (marker != null)
                {
                    marker.Latitude = location.Latitude;
                    marker.Longitude = location.Longitude;
                    marker.Heading = location.Heading;

                    // Update status based on speed
                    marker.Status = location.SpeedKmh > 5 ? "Moving" :
                                   location.SpeedKmh > 0 ? "Slow" : "Stationary";

                    _logger.LogDebug("Updated location for technician {TechnicianId} on map {MapId}",
                        location.TechnicianId, mapId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating technician location for {TechnicianId} on map {MapId}",
                    location.TechnicianId, mapId);
            }
        }

        public async Task AddTrafficIncidentAsync(string mapId, TrafficIncident incident)
        {
            try
            {
                if (!_activeMap.TryGetValue(mapId, out var map))
                {
                    _logger.LogWarning("Map {MapId} not found for incident addition", mapId);
                    return;
                }

                var incidentMarker = new IncidentMarker
                {
                    IncidentId = incident.IncidentId,
                    Type = incident.Type,
                    Latitude = incident.Latitude,
                    Longitude = incident.Longitude,
                    Severity = incident.Severity,
                    Description = incident.Description,
                    StartTime = incident.StartTime,
                    IconUrl = GetIncidentIconUrl(incident.Type, incident.Severity)
                };

                map.TrafficIncidents.Add(incidentMarker);

                _logger.LogInformation("Added traffic incident {IncidentId} to map {MapId}",
                    incident.IncidentId, mapId);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding traffic incident {IncidentId} to map {MapId}",
                    incident.IncidentId, mapId);
            }
        }

        public async Task RemoveTrafficIncidentAsync(string mapId, string incidentId)
        {
            try
            {
                if (!_activeMap.TryGetValue(mapId, out var map))
                {
                    _logger.LogWarning("Map {MapId} not found for incident removal", mapId);
                    return;
                }

                var incident = map.TrafficIncidents.FirstOrDefault(i => i.IncidentId == incidentId);
                if (incident != null)
                {
                    map.TrafficIncidents.Remove(incident);
                    _logger.LogInformation("Removed traffic incident {IncidentId} from map {MapId}",
                        incidentId, mapId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing traffic incident {IncidentId} from map {MapId}",
                    incidentId, mapId);
            }
        }

        private string GetStatusColor(string status) => status switch
        {
            "Available" => "#28a745",
            "EnRoute" => "#ffc107",
            "OnSite" => "#17a2b8",
            "Busy" => "#dc3545",
            _ => "#6c757d"
        };

        private string GenerateVanIconSvg(string baseColor, int skillLevel, double slaRate)
        {
            // Create an enhanced SVG van icon with skill level and SLA indicators
            var skillStars = Math.Min(5, Math.Max(0, skillLevel / 2)); // 0-5 stars based on skill count
            var slaIndicator = slaRate > 0.9 ? "⭐" : slaRate > 0.8 ? "✓" : "";

            return $@"
                <svg width='40' height='40' viewBox='0 0 40 40' xmlns='http://www.w3.org/2000/svg'>
                    <circle cx='20' cy='20' r='18' fill='{baseColor}' stroke='white' stroke-width='2'/>
                    <text x='20' y='25' text-anchor='middle' fill='white' font-size='16' font-family='Arial'>🚐</text>
                    {(skillStars > 0 ? $"<text x='35' y='15' text-anchor='middle' fill='gold' font-size='10'>{new string('★', Math.Min(3, skillStars))}</text>" : "")}
                    {(!string.IsNullOrEmpty(slaIndicator) ? $"<text x='35' y='30' text-anchor='middle' fill='white' font-size='10'>{slaIndicator}</text>" : "")}
                </svg>";
        }

        private string GetDefaultVanIcon()
        {
            return $"data:image/svg+xml;charset=UTF-8,{Uri.EscapeDataString(@"
                <svg width='32' height='32' viewBox='0 0 32 32' xmlns='http://www.w3.org/2000/svg'>
                    <circle cx='16' cy='16' r='14' fill='#6c757d' stroke='white' stroke-width='2'/>
                    <text x='16' y='20' text-anchor='middle' fill='white' font-size='12' font-family='Arial'>🚐</text>
                </svg>
            ")}";
        }

        private string GetIncidentIconUrl(IncidentType type, IncidentSeverity severity)
        {
            var color = severity switch
            {
                IncidentSeverity.Low => "#28a745",
                IncidentSeverity.Medium => "#ffc107",
                IncidentSeverity.High => "#fd7e14",
                IncidentSeverity.Critical => "#dc3545",
                _ => "#6c757d"
            };

            var icon = type switch
            {
                IncidentType.Accident => "🚗",
                IncidentType.Construction => "🚧",
                IncidentType.RoadClosure => "🚫",
                IncidentType.Emergency => "🚨",
                _ => "⚠️"
            };

            var svg = $@"
                <svg width='24' height='24' viewBox='0 0 24 24' xmlns='http://www.w3.org/2000/svg'>
                    <circle cx='12' cy='12' r='10' fill='{color}' stroke='white' stroke-width='2'/>
                    <text x='12' y='16' text-anchor='middle' fill='white' font-size='10' font-family='Arial'>{icon}</text>
                </svg>";

            return $"data:image/svg+xml;charset=UTF-8,{Uri.EscapeDataString(svg)}";
        }
    }
}