using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Services;

namespace FieldServiceIntelligence.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFieldServiceIntelligence(this IServiceCollection services)
        {
            return services.AddFieldServiceIntelligence(options => { });
        }

        public static IServiceCollection AddFieldServiceIntelligence(
            this IServiceCollection services,
            Action<FieldServiceOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            var options = new FieldServiceOptions();
            configureOptions(options);

            services.AddSingleton(options);

            services.AddScoped<IRecommendationScoring, RecommendationScoring>();
            services.AddScoped<IGeographicMatchingService, GeographicMatchingService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            if (options.IncludeEmergencyServices)
            {
                services.AddScoped<IEmergencyDiversionService, EmergencyDiversionService>();
            }

            if (options.IncludeTrafficServices)
            {
                services.AddScoped<ITrafficAwareRoutingService, TrafficAwareRoutingService>();
            }

            if (options.IncludeTrackingServices)
            {
                services.AddScoped<ITechnicianTrackingService, TechnicianTrackingService>();
            }

            if (options.IncludeMLNetScoring)
            {
                services.AddScoped<IMLNetScoringService, MLNetScoringService>();
            }

            return services;
        }

        public static IServiceCollection AddFieldServiceRecommendations(this IServiceCollection services)
        {
            services.AddScoped<IRecommendationScoring, RecommendationScoring>();
            services.AddScoped<IGeographicMatchingService, GeographicMatchingService>();
            services.AddScoped<IRecommendationService, RecommendationService>();

            return services;
        }

        public static IServiceCollection AddFieldServiceEmergencyServices(this IServiceCollection services)
        {
            services.AddScoped<IEmergencyDiversionService, EmergencyDiversionService>();
            return services;
        }

        public static IServiceCollection AddFieldServiceTrafficServices(this IServiceCollection services)
        {
            services.AddScoped<ITrafficAwareRoutingService, TrafficAwareRoutingService>();
            return services;
        }

        public static IServiceCollection AddFieldServiceTrackingServices(this IServiceCollection services)
        {
            services.AddScoped<ITechnicianTrackingService, TechnicianTrackingService>();
            return services;
        }

        public static IServiceCollection AddFieldServiceMLNetScoring(this IServiceCollection services)
        {
            services.AddScoped<IMLNetScoringService, MLNetScoringService>();
            return services;
        }
    }

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
    }
}