# ML.NET Technician Recommendation Service

A multi-tenant SaaS system for intelligent technician job assignment using ML.NET and optional LLM explanations.

## Features

- **Multi-Tenant ML.NET Models**: Tenant-specific prediction models for personalized recommendations
- **LLM Explanations**: Optional Azure OpenAI integration for human-readable explanations
- **Comprehensive Scoring**: Skills, distance, availability, SLA history, and workload analysis
- **Injectable Services**: Easy dependency injection setup for ASP.NET Core applications
- **Configurable**: Flexible configuration for ML models and Azure OpenAI

## Quick Start

### 1. Add Services to DI Container

```csharp
// In Program.cs or Startup.cs
builder.Services.AddRecommendationServices(builder.Configuration);
```

### 2. Configure Settings

```json
{
  "MLModels": {
    "BasePath": "Models",
    "ModelCacheExpirationHours": 24,
    "EnableModelCaching": true
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4",
    "Enabled": true,
    "MaxTokens": 150,
    "Temperature": 0.3
  }
}
```

### 3. Use in Your Application

```csharp
public class JobController : ControllerBase
{
    private readonly ITenantRecommendationService _recommendationService;

    public JobController(ITenantRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpPost("recommendations")]
    public async Task<RecommendationResponse> GetRecommendations(
        [FromBody] RecommendationRequest request)
    {
        return await _recommendationService.GetRecommendationsAsync(request);
    }
}
```

## Architecture

### Core Services

1. **ITenantRecommendationService**: Main orchestration service
2. **IMLNetPredictionService**: ML.NET prediction engine with tenant models
3. **ILlmExplanationService**: Azure OpenAI explanation generation
4. **IRecommendationScoring**: Mathematical scoring algorithms
5. **ITenantModelManager**: Model file management per tenant

### Scoring Factors

- **Skills Match**: Technician skills vs job requirements
- **Distance**: Travel distance and estimated time
- **Availability**: Current schedule and workload
- **SLA History**: Historical success rates
- **Priority**: Job urgency weighting

### Multi-Tenant Model Management

Each tenant has isolated ML models stored in:
```
Models/
├── tenant1_model.zip
├── tenant2_model.zip
└── tenant3_model.zip
```

## Example Usage

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
        }
    },
    AvailableTechnicians = technicians,
    MaxRecommendations = 5,
    IncludeLlmExplanation = true
};

var response = await recommendationService.GetRecommendationsAsync(request);
```

## Dependencies

- Microsoft.ML (3.0.1)
- Azure.AI.OpenAI (1.0.0-beta.18)
- Microsoft.Extensions.* (8.0.0)

## Configuration Options

### MLModelsOptions
- `BasePath`: Directory for model files
- `ModelCacheExpirationHours`: Cache duration
- `EnableModelCaching`: Enable/disable caching

### AzureOpenAIOptions
- `Endpoint`: Azure OpenAI endpoint
- `ApiKey`: Authentication key
- `DeploymentName`: Model deployment name
- `Enabled`: Enable/disable LLM explanations
- `MaxTokens`: Response token limit
- `Temperature`: Response creativity (0.0-1.0)