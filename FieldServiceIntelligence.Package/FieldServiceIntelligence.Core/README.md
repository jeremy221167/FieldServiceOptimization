# Field Service Intelligence Core

ML.NET-powered field service optimization package providing technician recommendation, route optimization, emergency diversion, and traffic-aware routing services for enterprise applications.

## Features

- **Technician Recommendation Engine**: AI-powered matching of technicians to jobs based on skills, location, availability, and historical performance
- **Emergency Diversion Services**: Intelligent rerouting for emergency jobs with automatic notifications
- **Traffic-Aware Routing**: Route optimization considering real-time traffic conditions and incidents
- **Geographic Matching**: Advanced location-based technician assignment with coverage area analysis
- **Real-Time Technician Tracking**: GPS-based location tracking and status management
- **ML.NET Scoring**: Optional machine learning models for enhanced prediction accuracy

## Quick Start

### Installation

```bash
dotnet add package FieldServiceIntelligence.Core
```

### Basic Usage

```csharp
using FieldServiceIntelligence.Core.Extensions;

// Register services
builder.Services.AddFieldServiceIntelligence();

// Or register specific services only
builder.Services.AddFieldServiceRecommendations();
```

### Get Technician Recommendations

```csharp
public class JobAssignmentService
{
    private readonly IRecommendationService _recommendationService;

    public JobAssignmentService(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<RecommendationResponse> GetRecommendationsAsync(
        JobRequest job,
        List<Technician> availableTechnicians)
    {
        var request = new RecommendationRequest
        {
            Job = job,
            AvailableTechnicians = availableTechnicians,
            MaxRecommendations = 5
        };

        return await _recommendationService.GetTechnicianRecommendationsAsync(request);
    }
}
```

## Configuration Options

```csharp
builder.Services.AddFieldServiceIntelligence(options =>
{
    options.IncludeEmergencyServices = true;
    options.IncludeTrafficServices = true;
    options.IncludeTrackingServices = true;
    options.DefaultServiceRadiusKm = 50.0;
    options.MaxRecommendations = 10;
});
```

## Key Models

- `JobRequest`: Represents a service job with location, skills required, priority, etc.
- `Technician`: Represents a field technician with skills, location, availability
- `TechnicianRecommendation`: Scored recommendation with detailed scoring breakdown
- `EmergencyDiversionRequest/Response`: Emergency job handling with automatic notifications
- `TrafficIncident`: Real-time traffic data for route optimization

## Scoring Algorithm

The recommendation engine uses a weighted scoring system considering:

- **Skills Match** (25-40%): How well technician skills match job requirements
- **Distance** (15-30%): Geographic proximity to job location
- **Availability** (15-25%): Current workload and schedule availability
- **SLA Performance** (10-25%): Historical on-time completion rate
- **Geographic Coverage** (5-25%): Service area preferences and coverage

Weights are automatically adjusted based on job priority (Emergency, High, Normal, Low).

## License

MIT License. See LICENSE file for details.

## Support

For issues and feature requests, please visit our GitHub repository.