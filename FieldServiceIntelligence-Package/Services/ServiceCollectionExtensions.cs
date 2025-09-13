using ML.Services.Interfaces;
using ML.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ML.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRecommendationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<ITenantModelManager, TenantModelManager>();

            services.AddSingleton<IMLNetPredictionService, MLNetPredictionService>();

            services.AddScoped<ILlmExplanationService, LlmExplanationService>();

            services.AddScoped<IRecommendationScoring, RecommendationScoring>();

            services.AddScoped<ITenantRecommendationService, TenantRecommendationService>();

            // Emergency and tracking services
            services.AddScoped<IEmergencyDiversionService, EmergencyDiversionService>();
            services.AddSingleton<ITechnicianTrackingService, TechnicianTrackingService>();
            services.AddScoped<INotificationService, NotificationService>();

            // Google Maps and traffic services
            services.AddHttpClient();
            services.AddScoped<IGoogleMapsService, GoogleMapsService>();
            services.AddScoped<ITrafficAwareRoutingService, TrafficAwareRoutingService>();
            services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
            services.AddScoped<IMapVisualizationService, MapVisualizationService>();

            services.Configure<MLModelsOptions>(
                configuration.GetSection("MLModels"));

            services.Configure<AzureOpenAIOptions>(
                configuration.GetSection("AzureOpenAI"));

            return services;
        }

        public static IServiceCollection AddRecommendationServices(
            this IServiceCollection services,
            Action<MLModelsOptions> configureMLModels,
            Action<AzureOpenAIOptions> configureAzureOpenAI)
        {
            services.Configure(configureMLModels);
            services.Configure(configureAzureOpenAI);

            services.AddSingleton<ITenantModelManager, TenantModelManager>();
            services.AddSingleton<IMLNetPredictionService, MLNetPredictionService>();
            services.AddScoped<ILlmExplanationService, LlmExplanationService>();
            services.AddScoped<IRecommendationScoring, RecommendationScoring>();
            services.AddScoped<ITenantRecommendationService, TenantRecommendationService>();

            // Emergency and tracking services
            services.AddScoped<IEmergencyDiversionService, EmergencyDiversionService>();
            services.AddSingleton<ITechnicianTrackingService, TechnicianTrackingService>();
            services.AddScoped<INotificationService, NotificationService>();

            // Google Maps and traffic services
            services.AddHttpClient();
            services.AddScoped<IGoogleMapsService, GoogleMapsService>();
            services.AddScoped<ITrafficAwareRoutingService, TrafficAwareRoutingService>();
            services.AddScoped<IRouteOptimizationService, RouteOptimizationService>();
            services.AddScoped<IMapVisualizationService, MapVisualizationService>();

            return services;
        }
    }

    public class MLModelsOptions
    {
        public string BasePath { get; set; } = "Models";
        public int ModelCacheExpirationHours { get; set; } = 24;
        public bool EnableModelCaching { get; set; } = true;
    }

    public class AzureOpenAIOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = "gpt-4";
        public bool Enabled { get; set; } = false;
        public int MaxTokens { get; set; } = 150;
        public float Temperature { get; set; } = 0.3f;
    }
}