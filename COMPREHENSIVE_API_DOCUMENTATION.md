# Field Service Intelligence System - Comprehensive API Documentation

## Overview

The Field Service Intelligence System is a comprehensive multi-tenant SaaS platform for intelligent technician job assignment, fault diagnosis, emergency response, and real-time tracking using ML.NET and Azure AI services.

### System Architecture

- **ML.Services**: Core machine learning and recommendation services
- **FieldServiceIntelligence.Core**: Packaged field service intelligence NuGet package
- **BlazorDemo**: Interactive demonstration application

---

## 🔧 Service Interfaces & Methods

### 1. Technician Recommendation Services

#### **ITenantRecommendationService**
*Primary service for multi-tenant technician recommendations*

```csharp
namespace ML.Services.Interfaces

// Get technician recommendations for a job
Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request)

// Score available technicians for a specific job
Task<List<TechnicianRecommendation>> ScoreTechniciansAsync(
    string tenantId,
    JobRequest job,
    List<Technician> technicians)

// Validate tenant access permissions
Task<bool> ValidateTenantAccessAsync(string tenantId)
```

**Usage Example:**
```csharp
var request = new RecommendationRequest
{
    TenantId = "tenant123",
    Job = new JobRequest
    {
        JobId = "job001",
        ServiceType = "HVAC Repair",
        Location = "Downtown Office",
        Latitude = 40.7128,
        Longitude = -74.0060,
        ScheduledDate = DateTime.UtcNow.AddHours(4),
        RequiredSkills = new Dictionary<string, string>
        {
            { "HVAC", "3" },
            { "Electrical", "2" }
        },
        Priority = "High",
        IsEmergency = false
    },
    AvailableTechnicians = technicians,
    MaxRecommendations = 5,
    IncludeLlmExplanation = true
};

var response = await recommendationService.GetRecommendationsAsync(request);
```

#### **IRecommendationScoring**
*Core scoring algorithms for technician recommendations*

```csharp
// Calculate skills match score (0.0 - 1.0)
float CalculateSkillsScore(
    Dictionary<string, string> requiredSkills,
    Dictionary<string, int> technicianSkills)

// Calculate distance-based score with decay
float CalculateDistanceScore(double jobLat, double jobLng, double techLat, double techLng)

// Calculate availability score based on workload and schedule
float CalculateAvailabilityScore(Technician technician, DateTime jobScheduledDate)

// Calculate SLA performance score
float CalculateSlaScore(double historicalSuccessRate)

// Calculate current workload impact
float CalculateWorkloadScore(int currentWorkload)

// Calculate geographic coverage match
float CalculateGeographicScore(JobRequest job, Technician technician)
GeographicMatch CalculateGeographicMatch(JobRequest job, Technician technician)

// Utility methods
double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
double EstimateTravelTime(double distanceKm)
```

### 2. Machine Learning Services

#### **IMLNetPredictionService**
*ML.NET prediction engine with tenant-specific models*

```csharp
// Predict recommendation score using ML model
Task<float> PredictScoreAsync(string tenantId, MLNetInput input)

// Load tenant-specific model
Task<bool> LoadTenantModelAsync(string tenantId)

// Get loaded model version
Task<string> GetModelVersionAsync(string tenantId)

// Check if model is loaded
Task<bool> IsModelLoadedAsync(string tenantId)
```

#### **ITenantModelManager**
*Manages ML models per tenant*

```csharp
// Get path to tenant model file
Task<string> GetModelPathAsync(string tenantId)

// Check if model file exists
Task<bool> ModelExistsAsync(string tenantId)

// Get model last update timestamp
Task<DateTime> GetModelLastUpdatedAsync(string tenantId)

// Get model version info
Task<string> GetModelVersionAsync(string tenantId)

// Update model with new data
Task UpdateModelAsync(string tenantId, byte[] modelData)
```

### 3. Emergency & Diversion Services

#### **IEmergencyDiversionService**
*Handles emergency job diversion and technician reallocation*

```csharp
// Find best diversion option for emergency
Task<EmergencyDiversionResponse> FindBestDiversionAsync(EmergencyDiversionRequest request)

// Get technicians that can be interrupted
Task<List<TechnicianRecommendation>> GetInterruptibleTechniciansAsync(
    JobRequest emergencyJob,
    List<Technician> technicians)

// Calculate cost of interrupting a technician
Task<double> CalculateInterruptionCostAsync(Technician technician, JobRequest emergencyJob)
```

**Emergency Request Example:**
```csharp
var emergencyRequest = new EmergencyDiversionRequest
{
    TenantId = "tenant123",
    EmergencyJob = new JobRequest
    {
        JobId = "EMRG-001",
        ServiceType = "Emergency HVAC",
        Priority = "Emergency",
        IsEmergency = true,
        Location = "Hospital Critical Care",
        Latitude = 40.7589,
        Longitude = -73.9851,
        ScheduledDate = DateTime.UtcNow.AddMinutes(30)
    },
    AvailableTechnicians = technicians,
    AllowInterruption = true,
    MaxDiversionDistance = 25.0,
    RequiredResponseTimeMinutes = 45
};

var diversionResponse = await emergencyService.FindBestDiversionAsync(emergencyRequest);
```

### 4. Tracking & Location Services

#### **ITechnicianTrackingService**
*Real-time technician location and status tracking*

```csharp
// Update technician GPS location
Task UpdateTechnicianLocationAsync(TrackingUpdate update)

// Get current technician status
Task<TechnicianStatus> GetTechnicianStatusAsync(string technicianId)

// Calculate ETA to destination
Task<double> GetEstimatedArrivalTimeAsync(
    string technicianId,
    double destLat,
    double destLng)

// Start tracking for a job
Task StartTrackingAsync(string technicianId, string jobId)

// Stop tracking
Task StopTrackingAsync(string technicianId)
```

**Tracking Update Example:**
```csharp
var trackingUpdate = new TrackingUpdate
{
    TechnicianId = "TECH-001",
    Latitude = 40.7489,
    Longitude = -73.9780,
    Timestamp = DateTime.UtcNow,
    Speed = 25.0, // mph
    Heading = 180.0, // degrees
    Accuracy = 5.0, // meters
    Status = TechnicianStatus.EnRoute
};

await trackingService.UpdateTechnicianLocationAsync(trackingUpdate);
```

### 5. Traffic & Routing Services

#### **ITrafficAwareRoutingService**
*Advanced routing with real-time traffic data*

```csharp
// Calculate traffic-aware ETA
Task<TrafficAwareETAResponse> CalculateTrafficAwareETAAsync(RouteOptimizationRequest request)

// Get optimal route with traffic
Task<TrafficAwareRoute> GetOptimalRouteAsync(RouteOptimizationRequest request)

// Get traffic incidents in area
Task<List<TrafficIncident>> GetTrafficIncidentsAsync(
    double centerLat,
    double centerLng,
    double radiusKm)

// Check if route affected by traffic
Task<bool> IsRouteAffectedByTrafficAsync(string routeId)

// Find faster alternative route
Task<AlternativeRoute?> FindFasterAlternativeAsync(TrafficAwareRoute currentRoute)
```

#### **IGoogleMapsService**
*Google Maps API integration*

```csharp
// Get directions with traffic
Task<TrafficAwareRoute> GetDirectionsAsync(
    double originLat,
    double originLng,
    double destLat,
    double destLng,
    RoutePreferences? preferences = null)

// Convert address to coordinates
Task<List<RouteWaypoint>> GeocodeAddressAsync(string address)

// Convert coordinates to address
Task<string> ReverseGeocodeAsync(double latitude, double longitude)

// Get road closures
Task<List<TrafficIncident>> GetRoadClosuresAsync(
    double centerLat,
    double centerLng,
    double radiusKm)

// Generate map visualization
Task<MapVisualization> GenerateMapVisualizationAsync(
    List<TechnicianLocation> technicians,
    List<JobLocation> jobs,
    List<TrafficAwareRoute> routes)
```

### 6. Fault Diagnosis Services

#### **IFaultDiagnosisService**
*AI-powered equipment fault diagnosis*

```csharp
// Diagnose equipment fault
Task<FaultDiagnosisResponse> DiagnoseFaultAsync(FaultDiagnosisRequest request)

// Predict possible faults
Task<List<FaultPrediction>> PredictPossibleFaultsAsync(
    string tenantId,
    string equipmentId,
    string symptoms)

// Recommend replacement parts
Task<List<PartRecommendation>> RecommendPartsAsync(
    string tenantId,
    List<FaultPrediction> faultPredictions)

// Recommend technicians for fault type
Task<List<TechnicianRecommendation>> RecommendTechniciansForFaultAsync(
    string tenantId,
    List<FaultPrediction> faultPredictions)

// Calculate fault probability
Task<double> CalculateFaultProbabilityAsync(
    string tenantId,
    string equipmentId,
    string faultId,
    string symptoms)
```

**Fault Diagnosis Example:**
```csharp
var faultRequest = new FaultDiagnosisRequest
{
    TenantId = "tenant123",
    EquipmentId = "HVAC-001",
    ReportedSymptoms = "No cooling, strange noises, high energy consumption",
    ObservedBehaviors = new List<string>
    {
        "Compressor cycling frequently",
        "Unusual vibration",
        "Ice formation on coils"
    },
    FaultReportedDate = DateTime.UtcNow,
    Priority = "High",
    ReportedBy = "Building Manager",
    EnvironmentalConditions = new Dictionary<string, string>
    {
        { "AmbientTemp", "85F" },
        { "Humidity", "70%" },
        { "LastMaintenance", "6 months ago" }
    },
    IncludePartsRecommendations = true,
    IncludeTechnicianRecommendations = true
};

var diagnosis = await faultService.DiagnoseFaultAsync(faultRequest);
```

### 7. KPI & Analytics Services

#### **ITechnicianKPIService**
*Performance tracking and analytics*

```csharp
// Get technician KPIs for period
Task<TechnicianKPI> GetTechnicianKPIAsync(
    string tenantId,
    string technicianId,
    DateTime periodStart,
    DateTime periodEnd)

// Get all technicians' KPIs
Task<List<TechnicianKPI>> GetAllTechnicianKPIsAsync(
    string tenantId,
    DateTime periodStart,
    DateTime periodEnd)

// Get technician specialization data
Task<Dictionary<string, double>> GetTechnicianFaultSpecializationAsync(
    string tenantId,
    string technicianId)

// Rank technicians by fault type expertise
Task<List<TechnicianPerformanceMetrics>> RankTechniciansForFaultTypeAsync(
    string tenantId,
    string faultId)

// Calculate success rate for specific fault
Task<double> CalculateTechnicianSuccessRateAsync(
    string tenantId,
    string technicianId,
    string faultId)

// Update KPI after job completion
Task UpdateTechnicianKPIAsync(
    string tenantId,
    string technicianId,
    HistoricalFault completedJob)
```

### 8. Notification Services

#### **INotificationService**
*Multi-channel communication*

```csharp
// Send SMS notification
Task SendSmsAsync(string phoneNumber, string message)

// Send email notification
Task SendEmailAsync(string email, string subject, string message)

// Send ETA update to customer
Task<bool> SendETAUpdateAsync(
    string customerPhone,
    string technicianName,
    double etaMinutes)

// Send emergency diversion notification
Task<bool> SendEmergencyDiversionNotificationAsync(NotificationAction notification)
```

### 9. AI Explanation Services

#### **ILlmExplanationService**
*AI-powered recommendation explanations*

```csharp
// Generate explanation for single recommendation
Task<string> GenerateExplanationAsync(
    JobRequest job,
    Technician technician,
    TechnicianRecommendation recommendation)

// Generate batch explanations
Task<Dictionary<string, string>> GenerateBatchExplanationsAsync(
    JobRequest job,
    List<TechnicianRecommendation> recommendations)

// Check if LLM service is available
Task<bool> IsServiceAvailableAsync()
```

---

## 📊 Data Models & Structures

### Core Models

#### **JobRequest**
```csharp
public class JobRequest
{
    public string JobId { get; set; }                    // Unique job identifier
    public string ServiceType { get; set; }             // "HVAC", "Plumbing", "Electrical"
    public string Equipment { get; set; }               // Equipment description
    public string Location { get; set; }                // Human-readable location
    public double Latitude { get; set; }                // GPS latitude
    public double Longitude { get; set; }               // GPS longitude
    public DateTime ScheduledDate { get; set; }         // When job is scheduled
    public int SlaHours { get; set; }                   // SLA requirement in hours
    public string Priority { get; set; } = "Normal";    // "Low", "Normal", "High", "Emergency"
    public Dictionary<string, string> RequiredSkills { get; set; }  // Skill -> Level mapping
    public bool IsEmergency { get; set; }               // Emergency flag
    public string CustomerPhone { get; set; }           // Customer contact
    public string CustomerName { get; set; }            // Customer name
    public string Description { get; set; }             // Job description
    public decimal EstimatedDurationHours { get; set; }  // Expected duration
    public string[] RequiredCertifications { get; set; }  // Required certifications
}
```

#### **Technician**
```csharp
public class Technician
{
    public string TechnicianId { get; set; }            // Unique technician ID
    public string Name { get; set; }                    // Full name
    public double Latitude { get; set; }                // Current GPS latitude
    public double Longitude { get; set; }               // Current GPS longitude
    public Dictionary<string, int> Skills { get; set; }  // Skill -> Proficiency (1-5)
    public bool IsAvailable { get; set; }               // Current availability
    public int CurrentWorkload { get; set; }            // Number of assigned jobs
    public double HistoricalSlaSuccessRate { get; set; }  // SLA success rate (0.0-1.0)
    public DateTime AvailableFrom { get; set; }         // Available start time
    public DateTime AvailableUntil { get; set; }        // Available end time
    public GeographicCoverage CoverageArea { get; set; }  // Service area
    public TechnicianStatus CurrentStatus { get; set; }  // Current status
    public string CurrentJobId { get; set; }            // Currently assigned job
    public bool CanBeInterrupted { get; set; }          // Can be interrupted for emergency
    public string Phone { get; set; }                   // Contact number
    public string[] Certifications { get; set; }        // Professional certifications
    public decimal HourlyRate { get; set; }             // Hourly billing rate
    public string VehicleType { get; set; }             // Vehicle for transportation
}
```

#### **TechnicianRecommendation**
```csharp
public class TechnicianRecommendation
{
    public string TechnicianId { get; set; }            // Technician identifier
    public string Name { get; set; }                    // Technician name
    public double Score { get; set; }                   // Overall recommendation score (0.0-1.0)
    public double SkillsScore { get; set; }             // Skills match score
    public double DistanceScore { get; set; }           // Distance-based score
    public double AvailabilityScore { get; set; }       // Availability score
    public double SlaScore { get; set; }                // SLA performance score
    public double GeographicScore { get; set; }         // Geographic coverage score
    public string? Explanation { get; set; }            // AI-generated explanation
    public double EstimatedTravelTime { get; set; }     // Travel time in minutes
    public double Distance { get; set; }                // Distance in kilometers
    public GeographicMatch GeographicMatch { get; set; }  // Geographic analysis
    public decimal EstimatedCost { get; set; }          // Estimated job cost
    public DateTime EstimatedArrival { get; set; }      // Estimated arrival time
    public List<string> MatchedSkills { get; set; }     // Skills that match requirements
    public List<string> MissingSkills { get; set; }     // Skills not possessed
}
```

### Request/Response Models

#### **RecommendationRequest**
```csharp
public class RecommendationRequest
{
    public string TenantId { get; set; }                 // Multi-tenant identifier
    public JobRequest Job { get; set; }                  // Job details
    public List<Technician> AvailableTechnicians { get; set; }  // Available technicians
    public int MaxRecommendations { get; set; } = 5;    // Max recommendations to return
    public bool IncludeLlmExplanation { get; set; }     // Include AI explanations
    public RoutePreferences? RoutePreferences { get; set; }  // Routing preferences
    public bool ConsiderTraffic { get; set; } = true;   // Use traffic-aware routing
    public double MaxTravelDistance { get; set; } = 50.0;  // Max travel distance (km)
}
```

#### **RecommendationResponse**
```csharp
public class RecommendationResponse
{
    public string RequestId { get; set; }               // Unique request identifier
    public string TenantId { get; set; }                // Tenant identifier
    public List<TechnicianRecommendation> Recommendations { get; set; }  // Ranked recommendations
    public DateTime GeneratedAt { get; set; }           // Response generation time
    public TimeSpan ProcessingTime { get; set; }        // Processing duration
    public string ModelVersion { get; set; }            // ML model version used
    public Dictionary<string, object> Metadata { get; set; }  // Additional metadata
    public List<string> Warnings { get; set; }          // Any warnings or notices
    public bool UsedMLPrediction { get; set; }          // Whether ML model was used
}
```

### Fault Diagnosis Models

#### **FaultDiagnosisRequest**
```csharp
public class FaultDiagnosisRequest
{
    public string TenantId { get; set; }                 // Tenant identifier
    public string EquipmentId { get; set; }             // Equipment identifier
    public string ReportedSymptoms { get; set; }        // Described symptoms
    public List<string> ObservedBehaviors { get; set; }  // Observed behaviors
    public DateTime FaultReportedDate { get; set; }     // When fault was reported
    public string Priority { get; set; } = "Medium";    // Priority level
    public string ReportedBy { get; set; }              // Who reported the fault
    public Dictionary<string, string> EnvironmentalConditions { get; set; }  // Environmental data
    public bool IncludePartsRecommendations { get; set; } = true;  // Include parts suggestions
    public bool IncludeTechnicianRecommendations { get; set; } = true;  // Include tech suggestions
    public byte[]? DiagnosticData { get; set; }         // Raw diagnostic data
    public string[]? Images { get; set; }               // Diagnostic images (base64)
}
```

#### **FaultDiagnosisResponse**
```csharp
public class FaultDiagnosisResponse
{
    public string DiagnosisId { get; set; }             // Unique diagnosis ID
    public string TenantId { get; set; }                // Tenant identifier
    public string EquipmentId { get; set; }             // Equipment identifier
    public List<FaultPrediction> PossibleFaults { get; set; }  // Ranked fault predictions
    public List<PartRecommendation> RecommendedParts { get; set; }  // Recommended parts
    public List<TechnicianRecommendation> RecommendedTechnicians { get; set; }  // Best technicians
    public int EstimatedResolutionTimeMinutes { get; set; }  // Expected fix time
    public decimal EstimatedCost { get; set; }          // Estimated total cost
    public string RecommendedActions { get; set; }      // Step-by-step actions
    public DateTime GeneratedAt { get; set; }           // Response timestamp
    public string ModelVersion { get; set; }            // AI model version
    public double ConfidenceScore { get; set; }         // Overall confidence (0.0-1.0)
}
```

### Traffic & Routing Models

#### **TrafficAwareRoute**
```csharp
public class TrafficAwareRoute
{
    public string RouteId { get; set; }                 // Unique route identifier
    public List<RouteWaypoint> Waypoints { get; set; }  // Route waypoints
    public double DistanceMeters { get; set; }          // Total distance
    public double DurationSeconds { get; set; }         // Duration without traffic
    public double DurationInTrafficSeconds { get; set; }  // Duration with current traffic
    public TrafficCondition TrafficCondition { get; set; }  // Overall traffic condition
    public List<TrafficIncident> TrafficIncidents { get; set; }  // Incidents along route
    public string PolylineEncoded { get; set; }         // Encoded polyline for mapping
    public DateTime LastUpdated { get; set; }           // When route was calculated
    public double FuelCostEstimate { get; set; }        // Estimated fuel cost
    public double TollCostEstimate { get; set; }        // Estimated toll costs
}
```

#### **TrafficIncident**
```csharp
public class TrafficIncident
{
    public string IncidentId { get; set; }              // Unique incident ID
    public IncidentType Type { get; set; }              // Type of incident
    public string Description { get; set; }             // Human-readable description
    public double Latitude { get; set; }                // Incident location latitude
    public double Longitude { get; set; }               // Incident location longitude
    public IncidentSeverity Severity { get; set; }      // Severity level
    public DateTime StartTime { get; set; }             // When incident started
    public DateTime? EstimatedEndTime { get; set; }     // Expected resolution time
    public double ImpactRadiusMeters { get; set; }      // Impact radius
    public string AlternateRoute { get; set; }          // Suggested alternate route
    public int DelayMinutes { get; set; }               // Estimated delay caused
}
```

### Equipment Models

#### **Equipment**
```csharp
public class Equipment
{
    public string EquipmentId { get; set; }             // Unique equipment identifier
    public string EquipmentType { get; set; }           // "HVAC", "Boiler", "Electrical Panel"
    public string Manufacturer { get; set; }            // Equipment manufacturer
    public string Model { get; set; }                   // Model number
    public string SerialNumber { get; set; }            // Serial number
    public DateTime InstallationDate { get; set; }      // Installation date
    public string Location { get; set; }                // Physical location
    public double Latitude { get; set; }                // GPS latitude
    public double Longitude { get; set; }               // GPS longitude
    public Dictionary<string, string> Specifications { get; set; }  // Technical specs
    public int MaintenanceCycleMonths { get; set; }     // Maintenance frequency
    public DateTime LastMaintenanceDate { get; set; }   // Last maintenance
    public string Status { get; set; } = "Active";      // Current status
    public decimal PurchasePrice { get; set; }          // Original purchase price
    public int WarrantyMonths { get; set; }             // Warranty period
}
```

### KPI & Analytics Models

#### **TechnicianKPI**
```csharp
public class TechnicianKPI
{
    public string TechnicianId { get; set; }            // Technician identifier
    public string Name { get; set; }                    // Technician name
    public DateTime PeriodStart { get; set; }           // KPI period start
    public DateTime PeriodEnd { get; set; }             // KPI period end
    public int JobsCompleted { get; set; }              // Total jobs completed
    public int JobsOnTime { get; set; }                 // Jobs completed on time
    public double SlaSuccessRate { get; set; }          // SLA success rate (0.0-1.0)
    public double AverageJobDuration { get; set; }      // Average job duration (hours)
    public double CustomerSatisfactionScore { get; set; }  // Average customer rating (1.0-5.0)
    public int EmergencyJobsHandled { get; set; }       // Emergency jobs completed
    public double TotalRevenue { get; set; }            // Revenue generated
    public int TrainingHours { get; set; }              // Training hours completed
    public double EfficiencyScore { get; set; }         // Overall efficiency score
    public Dictionary<string, int> FaultTypesResolved { get; set; }  // Fault expertise
}
```

### ML.NET Training Models

#### **MLNetInput**
```csharp
public class MLNetInput
{
    [LoadColumn(0)] public float SkillsMatch { get; set; }      // Skills compatibility (0.0-1.0)
    [LoadColumn(1)] public float Distance { get; set; }         // Distance to job (km)
    [LoadColumn(2)] public float Workload { get; set; }         // Current workload (0-10)
    [LoadColumn(3)] public float SlaHistory { get; set; }       // Historical SLA rate (0.0-1.0)
    [LoadColumn(4)] public float TravelTime { get; set; }       // Travel time (minutes)
    [LoadColumn(5)] public float Priority { get; set; }         // Job priority (1-4)
    [LoadColumn(6)] public float AvailabilityMatch { get; set; }  // Schedule availability (0.0-1.0)
    [LoadColumn(7)] public float GeographicScore { get; set; }   // Geographic coverage (0.0-1.0)
}
```

#### **FaultDiagnosisMLInput**
```csharp
public class FaultDiagnosisMLInput
{
    [LoadColumn(0)] public float EquipmentAge { get; set; }           // Age in years
    [LoadColumn(1)] public float TimeSinceLastMaintenance { get; set; }  // Days since maintenance
    [LoadColumn(2)] public float SymptomSeverity { get; set; }        // Severity score (1-10)
    [LoadColumn(3)] public float EnvironmentalFactors { get; set; }    // Environmental impact (0.0-1.0)
    [LoadColumn(4)] public float UsageIntensity { get; set; }          // Usage intensity (0.0-1.0)
    [LoadColumn(5)] public float EquipmentType { get; set; }           // Encoded equipment type
    [LoadColumn(6)] public float FaultFrequency { get; set; }          // Historical fault frequency
    [LoadColumn(7)] public float MaintenanceQuality { get; set; }      // Last maintenance quality (0.0-1.0)
}
```

---

## ⚙️ Configuration Options

### 1. ML Models Configuration

#### **appsettings.json**
```json
{
  "MLModels": {
    "BasePath": "Models",                    // Directory for model files
    "ModelCacheExpirationHours": 24,         // Cache duration
    "EnableModelCaching": true,              // Enable model caching
    "RetrainIntervalDays": 30,               // Model retraining frequency
    "MinimumTrainingData": 100,              // Minimum data points for training
    "ModelValidationThreshold": 0.75         // Minimum accuracy threshold
  }
}
```

#### **MLModelsOptions Class**
```csharp
public class MLModelsOptions
{
    public string BasePath { get; set; } = "Models";
    public int ModelCacheExpirationHours { get; set; } = 24;
    public bool EnableModelCaching { get; set; } = true;
    public int RetrainIntervalDays { get; set; } = 30;
    public int MinimumTrainingData { get; set; } = 100;
    public double ModelValidationThreshold { get; set; } = 0.75;
}
```

### 2. Azure OpenAI Configuration

#### **appsettings.json**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",  // Azure endpoint
    "ApiKey": "your-api-key-here",           // API key
    "DeploymentName": "gpt-4",               // Model deployment
    "Enabled": true,                         // Enable LLM explanations
    "MaxTokens": 150,                        // Response token limit
    "Temperature": 0.3,                      // Response creativity (0.0-1.0)
    "FrequencyPenalty": 0.0,                 // Frequency penalty
    "PresencePenalty": 0.0,                  // Presence penalty
    "TimeoutSeconds": 30,                    // Request timeout
    "MaxRetries": 3                          // Retry attempts
  }
}
```

#### **AzureOpenAIOptions Class**
```csharp
public class AzureOpenAIOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public bool Enabled { get; set; } = false;
    public int MaxTokens { get; set; } = 150;
    public float Temperature { get; set; } = 0.3f;
    public float FrequencyPenalty { get; set; } = 0.0f;
    public float PresencePenalty { get; set; } = 0.0f;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}
```

### 3. Field Service Intelligence Options

#### **FieldServiceOptions Class**
```csharp
public class FieldServiceOptions
{
    public bool IncludeEmergencyServices { get; set; } = true;
    public bool IncludeTrafficServices { get; set; } = true;
    public bool IncludeTrackingServices { get; set; } = true;
    public bool IncludeMLNetScoring { get; set; } = false;
    public string? MLNetModelPath { get; set; }
    public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRecommendations { get; set; } = 10;
    public double DefaultServiceRadiusKm { get; set; } = 50.0;
    public double DefaultMaxTravelDistanceKm { get; set; } = 200.0;
    public bool EnableRealTimeUpdates { get; set; } = true;
    public int TrackingUpdateIntervalSeconds { get; set; } = 30;
    public bool EnableNotifications { get; set; } = true;
    public string NotificationProvider { get; set; } = "Twilio"; // "Twilio", "SendGrid", etc.
}
```

### 4. Google Maps Configuration

#### **GoogleMapsOptions Class**
```csharp
public class GoogleMapsOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public bool EnableDirections { get; set; } = true;
    public bool EnableGeocoding { get; set; } = true;
    public bool EnableTrafficData { get; set; } = true;
    public string DefaultUnits { get; set; } = "metric"; // "metric" or "imperial"
    public string DefaultRegion { get; set; } = "US";
    public int RequestTimeoutSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
}
```

---

## 🔗 Service Registration Patterns

### 1. ML.Services Registration

#### **Full Service Registration**
```csharp
// Program.cs - Configuration-based registration
builder.Services.AddRecommendationServices(builder.Configuration);

// Program.cs - Action-based registration
builder.Services.AddRecommendationServices(
    configureMLModels: options => {
        options.BasePath = "Models";
        options.ModelCacheExpirationHours = 24;
        options.EnableModelCaching = true;
    },
    configureAzureOpenAI: options => {
        options.Enabled = true;
        options.DeploymentName = "gpt-4";
        options.MaxTokens = 200;
        options.Temperature = 0.3f;
    }
);
```

#### **Individual Service Registration**
```csharp
// Register specific services only
builder.Services.AddScoped<ITenantRecommendationService, TenantRecommendationService>();
builder.Services.AddScoped<IRecommendationScoring, RecommendationScoring>();
builder.Services.AddSingleton<ITenantModelManager, TenantModelManager>();
builder.Services.AddSingleton<IMLNetPredictionService, MLNetPredictionService>();
builder.Services.AddScoped<ILlmExplanationService, LlmExplanationService>();

// Configure options
builder.Services.Configure<MLModelsOptions>(builder.Configuration.GetSection("MLModels"));
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
```

**Services Registered:**
- `ITenantModelManager` → `TenantModelManager` (Singleton)
- `IMLNetPredictionService` → `MLNetPredictionService` (Singleton)
- `ILlmExplanationService` → `LlmExplanationService` (Scoped)
- `IRecommendationScoring` → `RecommendationScoring` (Scoped)
- `ITenantRecommendationService` → `TenantRecommendationService` (Scoped)
- `IEmergencyDiversionService` → `EmergencyDiversionService` (Scoped)
- `ITechnicianTrackingService` → `TechnicianTrackingService` (Singleton)
- `IGoogleMapsService` → `GoogleMapsService` (Scoped)
- `INotificationService` → `NotificationService` (Scoped)
- `IFaultDiagnosisService` → `FaultDiagnosisService` (Scoped)
- `ITechnicianKPIService` → `TechnicianKPIService` (Scoped)

### 2. FieldServiceIntelligence.Core Registration

#### **Full Package Registration**
```csharp
// Register all services with options
builder.Services.AddFieldServiceIntelligence(options => {
    options.IncludeEmergencyServices = true;
    options.IncludeTrafficServices = true;
    options.IncludeTrackingServices = true;
    options.MaxRecommendations = 10;
    options.DefaultServiceRadiusKm = 50.0;
    options.EnableRealTimeUpdates = true;
});

// Register with configuration section
builder.Services.AddFieldServiceIntelligence(
    builder.Configuration.GetSection("FieldService")
);
```

#### **Selective Service Registration**
```csharp
// Register individual service groups
builder.Services.AddFieldServiceRecommendations();
builder.Services.AddFieldServiceEmergencyServices();
builder.Services.AddFieldServiceTrafficServices();
builder.Services.AddFieldServiceTrackingServices();
builder.Services.AddFieldServiceMLNetScoring(modelPath: "path/to/model.zip");
```

### 3. Demo Data Service Registration

#### **Demo Application Setup**
```csharp
// BlazorDemo/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add recommendation services
builder.Services.AddRecommendationServices(builder.Configuration);

// Add demo data service
builder.Services.AddScoped<IDemoDataService, DemoDataService>();

var app = builder.Build();

// Configure middleware pipeline
app.UseRouting();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

---

## 📋 Complete Usage Examples

### 1. Basic Technician Recommendation

```csharp
public class RecommendationController : ControllerBase
{
    private readonly ITenantRecommendationService _recommendationService;

    public RecommendationController(ITenantRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpPost("api/recommendations")]
    public async Task<ActionResult<RecommendationResponse>> GetRecommendations(
        [FromBody] RecommendationRequest request)
    {
        try
        {
            // Validate tenant access
            var hasAccess = await _recommendationService.ValidateTenantAccessAsync(request.TenantId);
            if (!hasAccess)
                return Forbid("Invalid tenant access");

            // Get recommendations
            var response = await _recommendationService.GetRecommendationsAsync(request);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### 2. Emergency Job Diversion

```csharp
public class EmergencyController : ControllerBase
{
    private readonly IEmergencyDiversionService _emergencyService;
    private readonly INotificationService _notificationService;

    public EmergencyController(
        IEmergencyDiversionService emergencyService,
        INotificationService notificationService)
    {
        _emergencyService = emergencyService;
        _notificationService = notificationService;
    }

    [HttpPost("api/emergency/divert")]
    public async Task<ActionResult<EmergencyDiversionResponse>> HandleEmergency(
        [FromBody] EmergencyDiversionRequest request)
    {
        try
        {
            // Find best diversion option
            var diversionResponse = await _emergencyService.FindBestDiversionAsync(request);

            // Send notifications to affected parties
            if (diversionResponse.RecommendedTechnician != null)
            {
                await _notificationService.SendEmergencyDiversionNotificationAsync(
                    new NotificationAction
                    {
                        TechnicianId = diversionResponse.RecommendedTechnician.TechnicianId,
                        JobId = request.EmergencyJob.JobId,
                        Message = $"Emergency job assignment: {request.EmergencyJob.ServiceType}",
                        Priority = NotificationPriority.Emergency
                    });

                // Notify customer
                if (!string.IsNullOrEmpty(request.EmergencyJob.CustomerPhone))
                {
                    await _notificationService.SendETAUpdateAsync(
                        request.EmergencyJob.CustomerPhone,
                        diversionResponse.RecommendedTechnician.Name,
                        diversionResponse.EstimatedArrivalMinutes);
                }
            }

            return Ok(diversionResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### 3. Real-Time Technician Tracking

```csharp
public class TrackingController : ControllerBase
{
    private readonly ITechnicianTrackingService _trackingService;

    [HttpPost("api/tracking/update")]
    public async Task<ActionResult> UpdateLocation([FromBody] TrackingUpdate update)
    {
        try
        {
            await _trackingService.UpdateTechnicianLocationAsync(update);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/tracking/status/{technicianId}")]
    public async Task<ActionResult<TechnicianStatus>> GetStatus(string technicianId)
    {
        try
        {
            var status = await _trackingService.GetTechnicianStatusAsync(technicianId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/tracking/eta/{technicianId}")]
    public async Task<ActionResult<double>> GetETA(
        string technicianId,
        [FromQuery] double destLat,
        [FromQuery] double destLng)
    {
        try
        {
            var eta = await _trackingService.GetEstimatedArrivalTimeAsync(
                technicianId, destLat, destLng);
            return Ok(eta);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### 4. Fault Diagnosis

```csharp
public class DiagnosisController : ControllerBase
{
    private readonly IFaultDiagnosisService _diagnosisService;

    [HttpPost("api/diagnosis/fault")]
    public async Task<ActionResult<FaultDiagnosisResponse>> DiagnoseFault(
        [FromBody] FaultDiagnosisRequest request)
    {
        try
        {
            var diagnosis = await _diagnosisService.DiagnoseFaultAsync(request);
            return Ok(diagnosis);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/diagnosis/equipment/{equipmentId}/history")]
    public async Task<ActionResult<List<HistoricalFault>>> GetEquipmentHistory(
        string equipmentId,
        [FromQuery] string tenantId,
        [FromQuery] int daysBack = 365)
    {
        try
        {
            var history = await _diagnosisService.GetEquipmentFaultHistoryAsync(
                tenantId, equipmentId, daysBack);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

### 5. Traffic-Aware Routing

```csharp
public class RoutingController : ControllerBase
{
    private readonly ITrafficAwareRoutingService _routingService;
    private readonly IGoogleMapsService _mapsService;

    [HttpPost("api/routing/optimal")]
    public async Task<ActionResult<TrafficAwareRoute>> GetOptimalRoute(
        [FromBody] RouteOptimizationRequest request)
    {
        try
        {
            var route = await _routingService.GetOptimalRouteAsync(request);
            return Ok(route);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("api/routing/traffic")]
    public async Task<ActionResult<List<TrafficIncident>>> GetTrafficIncidents(
        [FromQuery] double centerLat,
        [FromQuery] double centerLng,
        [FromQuery] double radiusKm = 10.0)
    {
        try
        {
            var incidents = await _routingService.GetTrafficIncidentsAsync(
                centerLat, centerLng, radiusKm);
            return Ok(incidents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

---

## 🎯 Key Features Summary

### 1. **Multi-Tenant Architecture**
- Complete tenant isolation for data and models
- Tenant-specific ML.NET models
- Per-tenant configuration and settings

### 2. **Machine Learning Integration**
- ML.NET models for scoring and predictions
- Automatic model retraining capabilities
- Confidence scoring for all predictions

### 3. **Real-Time Intelligence**
- GPS-based technician tracking
- Traffic-aware routing and ETA calculations
- Live status updates and notifications

### 4. **Emergency Response System**
- Automated emergency job diversion
- Technician interruption capabilities
- Priority-based job scheduling

### 5. **AI-Powered Explanations**
- Azure OpenAI integration
- Human-readable recommendation explanations
- Batch explanation generation

### 6. **Comprehensive Analytics**
- Technician performance KPIs
- Equipment failure predictions
- Historical trend analysis

### 7. **Geographic Intelligence**
- Advanced location-based matching
- Service area coverage analysis
- Distance and travel time optimization

### 8. **Fault Diagnosis**
- Equipment fault prediction
- Parts recommendation engine
- Historical fault pattern analysis

### 9. **Notification System**
- Multi-channel communication (SMS, Email)
- Automated customer updates
- Emergency alert capabilities

### 10. **Flexible Configuration**
- Comprehensive configuration options
- Environment-specific settings
- Feature flag support

---

## 📚 Quick Reference

### Essential Dependencies
```xml
<PackageReference Include="Microsoft.ML" Version="3.0.1" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.0.0-beta.1" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### Minimum Configuration
```json
{
  "MLModels": {
    "BasePath": "Models",
    "EnableModelCaching": true
  },
  "AzureOpenAI": {
    "Enabled": false
  }
}
```

### Basic Service Registration
```csharp
builder.Services.AddRecommendationServices(builder.Configuration);
// OR
builder.Services.AddFieldServiceIntelligence();
```

This comprehensive documentation provides complete coverage of all endpoints, data models, configuration options, and usage patterns for the Field Service Intelligence System.