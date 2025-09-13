using Microsoft.Extensions.Logging;
using ML.Services.Interfaces;
using ML.Services.Models;

namespace ML.Services.Implementation
{
    public class EmergencyDiversionService : IEmergencyDiversionService
    {
        private readonly ITenantRecommendationService _recommendationService;
        private readonly IRecommendationScoring _scoringService;
        private readonly ITechnicianTrackingService _trackingService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmergencyDiversionService> _logger;

        public EmergencyDiversionService(
            ITenantRecommendationService recommendationService,
            IRecommendationScoring scoringService,
            ITechnicianTrackingService trackingService,
            INotificationService notificationService,
            ILogger<EmergencyDiversionService> logger)
        {
            _recommendationService = recommendationService;
            _scoringService = scoringService;
            _trackingService = trackingService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<EmergencyDiversionResponse> FindBestDiversionAsync(EmergencyDiversionRequest request)
        {
            try
            {
                _logger.LogInformation("Processing emergency diversion for job {JobId}",
                    request.EmergencyJob.JobId);

                var availableTechs = request.AllAvailableTechnicians
                    .Where(t => t.IsAvailable && t.CurrentStatus.Status == "Available")
                    .ToList();

                var interruptibleTechs = await GetInterruptibleTechniciansAsync(
                    request.EmergencyJob, request.CurrentlyAssignedTechnicians);

                var allCandidates = availableTechs.Concat(interruptibleTechs.Select(r =>
                    request.CurrentlyAssignedTechnicians.First(t => t.TechnicianId == r.TechnicianId)))
                    .ToList();

                if (!allCandidates.Any())
                {
                    _logger.LogWarning("No available or interruptible technicians found for emergency job {JobId}",
                        request.EmergencyJob.JobId);
                    return new EmergencyDiversionResponse();
                }

                var recommendations = await _recommendationService.ScoreTechniciansAsync(
                    "demo-tenant", request.EmergencyJob, allCandidates);

                var bestRecommendation = recommendations
                    .OrderByDescending(r => r.Score)
                    .First();

                var selectedTechnician = allCandidates
                    .First(t => t.TechnicianId == bestRecommendation.TechnicianId);

                var diversionType = DetermineDiversionType(selectedTechnician);
                var notifications = await CreateNotificationsAsync(
                    request.EmergencyJob, selectedTechnician, diversionType);

                var response = new EmergencyDiversionResponse
                {
                    RecommendedTechnician = bestRecommendation,
                    DiversionType = diversionType,
                    PreviousJobId = selectedTechnician.CurrentJobId,
                    EstimatedDelayMinutes = await CalculateDelayAsync(selectedTechnician, request.EmergencyJob),
                    RequiredNotifications = notifications
                };

                _logger.LogInformation("Emergency diversion completed: {TechnicianId} assigned to {JobId} ({DiversionType})",
                    selectedTechnician.TechnicianId, request.EmergencyJob.JobId, diversionType);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing emergency diversion for job {JobId}",
                    request.EmergencyJob?.JobId);
                throw;
            }
        }

        public async Task<List<TechnicianRecommendation>> GetInterruptibleTechniciansAsync(
            JobRequest emergencyJob, List<Technician> technicians)
        {
            try
            {
                var interruptibleTechs = technicians
                    .Where(t => t.CanBeInterrupted &&
                               (t.CurrentStatus.Status == "EnRoute" || t.CurrentStatus.Status == "OnSite"))
                    .ToList();

                var recommendations = new List<TechnicianRecommendation>();

                foreach (var tech in interruptibleTechs)
                {
                    var interruptionCost = await CalculateInterruptionCostAsync(tech, emergencyJob);

                    if (interruptionCost < 0.8) // Only consider if interruption cost is reasonable
                    {
                        var baseScore = await CalculateBaseScoreAsync(tech, emergencyJob);
                        var adjustedScore = baseScore * (1 - interruptionCost * 0.3);

                        recommendations.Add(new TechnicianRecommendation
                        {
                            TechnicianId = tech.TechnicianId,
                            Name = tech.Name,
                            Score = adjustedScore,
                            Distance = _scoringService.CalculateDistance(
                                emergencyJob.Latitude, emergencyJob.Longitude,
                                tech.CurrentStatus.CurrentLatitude ?? tech.Latitude,
                                tech.CurrentStatus.CurrentLongitude ?? tech.Longitude),
                            EstimatedTravelTime = tech.CurrentStatus.EstimatedArrivalMinutes
                        });
                    }
                }

                return recommendations.OrderByDescending(r => r.Score).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interruptible technicians");
                return new List<TechnicianRecommendation>();
            }
        }

        public async Task<double> CalculateInterruptionCostAsync(Technician technician, JobRequest emergencyJob)
        {
            try
            {
                var baseCost = 0.0;

                // Factor in current job priority vs emergency priority
                var priorityWeight = GetPriorityWeight(emergencyJob.Priority);
                if (priorityWeight <= GetPriorityWeight(technician.CurrentStatus.CurrentJobLocation))
                {
                    baseCost += 0.4;
                }

                // Factor in progress on current job
                var progressCost = technician.CurrentStatus.Status switch
                {
                    "EnRoute" => 0.2,
                    "OnSite" => 0.5,
                    "Busy" => 0.8,
                    _ => 0.1
                };
                baseCost += progressCost;

                // Factor in customer impact
                if (technician.CurrentStatus.EstimatedArrivalMinutes < 30)
                {
                    baseCost += 0.3;
                }

                // Factor in SLA impact
                if (technician.HistoricalSlaSuccessRate > 0.9)
                {
                    baseCost += 0.1; // High-performing techs have higher interruption cost
                }

                return Math.Max(0.0, Math.Min(1.0, baseCost));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating interruption cost for technician {TechnicianId}",
                    technician.TechnicianId);
                return 1.0; // High cost if we can't calculate
            }
        }

        private async Task<double> CalculateBaseScoreAsync(Technician technician, JobRequest job)
        {
            var skillsScore = _scoringService.CalculateSkillsScore(job.RequiredSkills, technician.Skills);
            var distanceScore = _scoringService.CalculateDistanceScore(
                job.Latitude, job.Longitude,
                technician.CurrentStatus.CurrentLatitude ?? technician.Latitude,
                technician.CurrentStatus.CurrentLongitude ?? technician.Longitude);
            var geographicScore = _scoringService.CalculateGeographicScore(job, technician);

            return (skillsScore * 0.5 + distanceScore * 0.3 + geographicScore * 0.2);
        }

        private string DetermineDiversionType(Technician technician)
        {
            return technician.CurrentStatus.Status switch
            {
                "Available" => "Available",
                "EnRoute" => "Rerouted",
                "OnSite" or "Busy" => "Interrupted",
                _ => "Available"
            };
        }

        private async Task<List<NotificationAction>> CreateNotificationsAsync(
            JobRequest emergencyJob, Technician technician, string diversionType)
        {
            var notifications = new List<NotificationAction>();

            // Notify customer about emergency technician assignment
            if (!string.IsNullOrEmpty(emergencyJob.CustomerPhone))
            {
                var eta = await _trackingService.GetEstimatedArrivalTimeAsync(
                    technician.TechnicianId, emergencyJob.Latitude, emergencyJob.Longitude);

                notifications.Add(new NotificationAction
                {
                    Type = "SMS",
                    Recipient = emergencyJob.CustomerPhone,
                    Message = $"EMERGENCY: Technician {technician.Name} is being dispatched to your location. ETA: {eta:F0} minutes. We'll keep you updated.",
                    IsUrgent = true
                });
            }

            // Notify technician
            if (!string.IsNullOrEmpty(technician.Phone))
            {
                var message = diversionType switch
                {
                    "Interrupted" => $"EMERGENCY DIVERSION: Please stop current work and proceed immediately to {emergencyJob.Location}. Job ID: {emergencyJob.JobId}",
                    "Rerouted" => $"EMERGENCY REROUTE: New priority destination - {emergencyJob.Location}. Job ID: {emergencyJob.JobId}",
                    _ => $"EMERGENCY ASSIGNMENT: Immediate dispatch to {emergencyJob.Location}. Job ID: {emergencyJob.JobId}"
                };

                notifications.Add(new NotificationAction
                {
                    Type = "SMS",
                    Recipient = technician.Phone,
                    Message = message,
                    IsUrgent = true
                });
            }

            return notifications;
        }

        private async Task<double> CalculateDelayAsync(Technician technician, JobRequest emergencyJob)
        {
            if (technician.CurrentStatus.Status == "Available")
                return 0.0;

            var currentETA = technician.CurrentStatus.EstimatedArrivalMinutes;
            var newDistance = _scoringService.CalculateDistance(
                technician.CurrentStatus.CurrentLatitude ?? technician.Latitude,
                technician.CurrentStatus.CurrentLongitude ?? technician.Longitude,
                emergencyJob.Latitude, emergencyJob.Longitude);

            var newTravelTime = _scoringService.EstimateTravelTime(newDistance);

            return Math.Max(0.0, newTravelTime - currentETA);
        }

        private double GetPriorityWeight(string priority) => priority?.ToLower() switch
        {
            "urgent" => 1.0,
            "high" => 0.8,
            "normal" => 0.6,
            "low" => 0.4,
            _ => 0.6
        };
    }
}