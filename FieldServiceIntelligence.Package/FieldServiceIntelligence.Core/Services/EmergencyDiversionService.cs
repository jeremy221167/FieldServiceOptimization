using Microsoft.Extensions.Logging;
using FieldServiceIntelligence.Core.Interfaces;
using FieldServiceIntelligence.Core.Models;

namespace FieldServiceIntelligence.Core.Services
{
    public class EmergencyDiversionService : IEmergencyDiversionService
    {
        private readonly ILogger<EmergencyDiversionService> _logger;
        private readonly IRecommendationService _recommendationService;

        public EmergencyDiversionService(
            ILogger<EmergencyDiversionService> logger,
            IRecommendationService recommendationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
        }

        public async Task<EmergencyDiversionResponse> HandleEmergencyDiversionAsync(EmergencyDiversionRequest request)
        {
            try
            {
                if (request?.EmergencyJob == null)
                {
                    return new EmergencyDiversionResponse
                    {
                        DiversionType = "Failed",
                        EstimatedDelayMinutes = 0,
                        RequiredNotifications = new List<NotificationAction>()
                    };
                }

                var availableTechnicians = new List<Technician>();
                availableTechnicians.AddRange(request.AllAvailableTechnicians?.Where(t => t?.IsAvailable == true) ?? new List<Technician>());

                if (request.AllowInterruption && request.CurrentlyAssignedTechnicians?.Any() == true)
                {
                    var interruptibleTechnicians = request.CurrentlyAssignedTechnicians
                        .Where(t => t?.CanBeInterrupted == true)
                        .ToList();

                    availableTechnicians.AddRange(interruptibleTechnicians);
                }

                if (!availableTechnicians.Any())
                {
                    return new EmergencyDiversionResponse
                    {
                        DiversionType = "Failed",
                        EstimatedDelayMinutes = 0,
                        RequiredNotifications = new List<NotificationAction>
                        {
                            new NotificationAction
                            {
                                Type = "SMS",
                                Recipient = request.EmergencyJob.CustomerPhone,
                                Message = "Emergency service request received. No available technicians at this time.",
                                IsUrgent = true
                            }
                        }
                    };
                }

                var recommendationRequest = new RecommendationRequest
                {
                    Job = request.EmergencyJob,
                    AvailableTechnicians = availableTechnicians,
                    MaxRecommendations = 1,
                    IncludeLlmExplanation = false
                };

                var recommendations = await _recommendationService.GetTechnicianRecommendationsAsync(recommendationRequest);
                var bestTechnician = recommendations.Recommendations.FirstOrDefault();

                if (bestTechnician == null)
                {
                    return new EmergencyDiversionResponse
                    {
                        DiversionType = "Failed",
                        EstimatedDelayMinutes = 0,
                        RequiredNotifications = new List<NotificationAction>()
                    };
                }

                var originalTechnician = request.CurrentlyAssignedTechnicians?
                    .FirstOrDefault(t => t.TechnicianId == bestTechnician.TechnicianId);

                var diversionType = originalTechnician != null ? "Interrupted" : "Available";
                var estimatedDelay = diversionType == "Interrupted" ? 15.0 : 0.0;

                var notifications = await GenerateNotificationsAsync(new EmergencyDiversionResponse
                {
                    RecommendedTechnician = bestTechnician,
                    DiversionType = diversionType,
                    PreviousJobId = originalTechnician?.CurrentJobId ?? "",
                    EstimatedDelayMinutes = estimatedDelay
                });

                return new EmergencyDiversionResponse
                {
                    RecommendedTechnician = bestTechnician,
                    DiversionType = diversionType,
                    PreviousJobId = originalTechnician?.CurrentJobId ?? "",
                    EstimatedDelayMinutes = estimatedDelay,
                    RequiredNotifications = notifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling emergency diversion for job {JobId}", request?.EmergencyJob?.JobId);
                return new EmergencyDiversionResponse
                {
                    DiversionType = "Failed",
                    EstimatedDelayMinutes = 0,
                    RequiredNotifications = new List<NotificationAction>()
                };
            }
        }

        public async Task<List<NotificationAction>> GenerateNotificationsAsync(EmergencyDiversionResponse diversion)
        {
            try
            {
                var notifications = new List<NotificationAction>();

                if (diversion?.RecommendedTechnician == null)
                    return notifications;

                notifications.Add(new NotificationAction
                {
                    Type = "SMS",
                    Recipient = diversion.RecommendedTechnician.Name,
                    Message = $"EMERGENCY JOB ASSIGNED - Priority dispatch. ETA: {Math.Round(diversion.RecommendedTechnician.EstimatedTravelTime)} minutes.",
                    IsUrgent = true
                });

                if (diversion.DiversionType == "Interrupted" && !string.IsNullOrEmpty(diversion.PreviousJobId))
                {
                    notifications.Add(new NotificationAction
                    {
                        Type = "SMS",
                        Recipient = "Customer Service",
                        Message = $"Technician {diversion.RecommendedTechnician.Name} diverted from job {diversion.PreviousJobId} for emergency. Estimated delay: {diversion.EstimatedDelayMinutes} minutes.",
                        IsUrgent = true
                    });
                }

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating notifications for diversion");
                return new List<NotificationAction>();
            }
        }
    }
}