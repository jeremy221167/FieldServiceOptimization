namespace ML.Services.Models
{
    public class GoogleMapsConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string MapId { get; set; } = string.Empty;
        public bool EnableTraffic { get; set; } = true;
        public bool EnableRoadClosures { get; set; } = true;
        public int TrafficUpdateIntervalSeconds { get; set; } = 30;
    }

    public class TrafficAwareRoute
    {
        public string RouteId { get; set; } = string.Empty;
        public List<RouteWaypoint> Waypoints { get; set; } = new();
        public double DistanceMeters { get; set; }
        public double DurationSeconds { get; set; }
        public double DurationInTrafficSeconds { get; set; }
        public TrafficCondition TrafficCondition { get; set; } = TrafficCondition.Unknown;
        public List<TrafficIncident> TrafficIncidents { get; set; } = new();
        public string PolylineEncoded { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class RouteWaypoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public WaypointType Type { get; set; } = WaypointType.Via;
    }

    public enum WaypointType
    {
        Origin,
        Destination,
        Via
    }

    public enum TrafficCondition
    {
        Unknown,
        Clear,
        Light,
        Moderate,
        Heavy,
        Severe
    }

    public class TrafficIncident
    {
        public string IncidentId { get; set; } = string.Empty;
        public IncidentType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public IncidentSeverity Severity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EstimatedEndTime { get; set; }
        public double ImpactRadiusMeters { get; set; }
        public string AlternateRoute { get; set; } = string.Empty;
    }

    public enum IncidentType
    {
        Accident,
        Construction,
        RoadClosure,
        LaneRestriction,
        Weather,
        Event,
        Congestion,
        Emergency
    }

    public enum IncidentSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class RouteOptimizationRequest
    {
        public string TechnicianId { get; set; } = string.Empty;
        public TechnicianLocation CurrentLocation { get; set; } = new();
        public JobLocation Destination { get; set; } = new();
        public bool IsEmergency { get; set; } = false;
        public RoutePreferences Preferences { get; set; } = new();
        public DateTime DepartureTime { get; set; } = DateTime.UtcNow;
    }

    public class TechnicianLocation
    {
        public string TechnicianId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Heading { get; set; }
        public double SpeedKmh { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LocationAccuracy Accuracy { get; set; } = LocationAccuracy.High;
    }

    public class JobLocation
    {
        public string JobId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public DateTime ScheduledTime { get; set; }
    }

    public enum LocationAccuracy
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public class RoutePreferences
    {
        public bool AvoidTolls { get; set; } = false;
        public bool AvoidHighways { get; set; } = false;
        public bool AvoidFerries { get; set; } = true;
        public bool PreferFastestRoute { get; set; } = true;
        public bool EmergencyVehicleMode { get; set; } = false;
        public double MaxDetourMinutes { get; set; } = 15.0;
    }

    public class TrafficAwareETAResponse
    {
        public string RouteId { get; set; } = string.Empty;
        public double EstimatedTravelTimeMinutes { get; set; }
        public double DistanceKm { get; set; }
        public TrafficCondition TrafficCondition { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<TrafficIncident> AffectingIncidents { get; set; } = new();
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public AlternativeRoute? FasterAlternative { get; set; }
    }

    public class AlternativeRoute
    {
        public double EstimatedTravelTimeMinutes { get; set; }
        public double DistanceKm { get; set; }
        public string Reason { get; set; } = string.Empty;
        public double TimeSavedMinutes { get; set; }
    }

    public class MapVisualization
    {
        public string MapId { get; set; } = string.Empty;
        public MapCenter Center { get; set; } = new();
        public int ZoomLevel { get; set; } = 10;
        public List<TechnicianMarker> TechnicianMarkers { get; set; } = new();
        public List<JobMarker> JobMarkers { get; set; } = new();
        public List<RoutePolyline> Routes { get; set; } = new();
        public List<IncidentMarker> TrafficIncidents { get; set; } = new();
        public bool ShowTrafficLayer { get; set; } = true;
    }

    public class MapCenter
    {
        public double Latitude { get; set; } = 40.7128;
        public double Longitude { get; set; } = -74.0060;
    }

    public class TechnicianMarker
    {
        public string TechnicianId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = string.Empty;
        public double Heading { get; set; }
        public string IconUrl { get; set; } = string.Empty;
        public TechnicianMarkerInfo Info { get; set; } = new();
    }

    public class TechnicianMarkerInfo
    {
        public string CurrentJobId { get; set; } = string.Empty;
        public double ETAMinutes { get; set; }
        public string Phone { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public double SlaSuccessRate { get; set; }
    }

    public class JobMarker
    {
        public string JobId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string AssignedTechnicianId { get; set; } = string.Empty;
    }

    public class RoutePolyline
    {
        public string RouteId { get; set; } = string.Empty;
        public string TechnicianId { get; set; } = string.Empty;
        public string EncodedPolyline { get; set; } = string.Empty;
        public string Color { get; set; } = "#4285F4";
        public int Weight { get; set; } = 3;
        public double Opacity { get; set; } = 0.8;
        public bool IsActive { get; set; } = true;
    }

    public class IncidentMarker
    {
        public string IncidentId { get; set; } = string.Empty;
        public IncidentType Type { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public IncidentSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}